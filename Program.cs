using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LMS_Backend.Data;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Infrastructure.HealthChecks;
using LMS_Backend.Infrastructure.Seed;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using LMS_Backend.Services.Reporting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

LoadEnvFile();

var testConnectionRequested = args.Any(IsTestConnectionArg);
var filteredArgs = testConnectionRequested
    ? args.Where(arg => !IsTestConnectionArg(arg)).ToArray()
    : args;

var builder = WebApplication.CreateBuilder(filteredArgs);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPublicService, PublicService>();
builder.Services.AddScoped<IQuizService, QuizService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://lmsfrontend-umber.vercel.app",
                "https://lms-gold-tau.vercel.app",
                "https://lms-deepananirmal.vercel.app",
                "http://localhost:3000",
                "https://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

var azureStorageConnectionString = FirstNonEmpty(
    builder.Configuration[$"{AzureStorageOptions.SectionName}:ConnectionString"],
    builder.Configuration["AZURE_CONN"]);
var azureStorageContainerName = FirstNonEmpty(
    builder.Configuration[$"{AzureStorageOptions.SectionName}:ContainerName"],
    AzureStorageOptions.DefaultContainerName);
var profileImagesContainerName = FirstNonEmpty(
    builder.Configuration[$"{AzureStorageOptions.SectionName}:ProfileImagesContainerName"],
    AzureStorageOptions.DefaultProfileImagesContainerName);

builder.Services.Configure<AzureStorageOptions>(options =>
{
    options.ConnectionString = azureStorageConnectionString;
    options.ContainerName = string.IsNullOrWhiteSpace(azureStorageContainerName)
        ? AzureStorageOptions.DefaultContainerName
        : azureStorageContainerName;
    options.ProfileImagesContainerName = string.IsNullOrWhiteSpace(profileImagesContainerName)
        ? AzureStorageOptions.DefaultProfileImagesContainerName
        : profileImagesContainerName;
});
builder.Services.AddScoped<AzureStorageService>();
builder.Services.Configure<AzureCommunicationOptions>(
    builder.Configuration.GetSection(AzureCommunicationOptions.SectionName));

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("The API process is running."),
        tags: ["live", "full"])
    .AddCheck<ApplicationDbContextHealthCheck>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "full", "database"],
        timeout: TimeSpan.FromSeconds(10))
    .AddCheck<AzureBlobStorageHealthCheck>(
        "azure_blob_storage",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "full", "storage"],
        timeout: TimeSpan.FromSeconds(10))
    .AddCheck<AzureCommunicationServicesHealthCheck>(
        "azure_communication_services",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "full", "communication"],
        timeout: TimeSpan.FromSeconds(10));

// Email
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Identity
builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDBContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.TeacherOnly, policy => policy.RequireRole(AppRoles.Teacher));
    options.AddPolicy(AppPolicies.StudentOnly, policy => policy.RequireRole(AppRoles.Student));
});

// Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IProfileImageService, ProfileImageService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<AdminDiagnosticsService>();
builder.Services.AddScoped<TeacherDashboardService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<ILiveSessionService, LiveSessionService>();
builder.Services.AddScoped<IAzureCommunicationIdentityService, AzureCommunicationIdentityService>();
builder.Services.AddScoped<IAzureCommunicationLiveSessionService, AzureCommunicationLiveSessionService>();
builder.Services.AddScoped<ILiveSessionJoinService, LiveSessionJoinService>();
builder.Services.AddScoped<StudentDashboardService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IdentitySeeder>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LMS API",
        Version = "v1",
        Description = "Learning Management System backend endpoints"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT as: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

if (testConnectionRequested)
{
    await RunConnectionTestAsync(app);
    return;
}

// Apply pending migrations on startup. Identity seeding remains manual.
try
{
    await ApplyPendingMigrationsAsync(app);
    // await SeedIdentityAsync(app);
}
catch (Exception ex)
{
    Console.WriteLine($"Startup DB error: {ex}");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();

app.UseRouting();

app.UseCors("AllowFrontend");

// Temporarily disable if Azure proxy keeps breaking OPTIONS preflight.
// Re-enable later if needed after confirming forwarded headers work.
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    AllowCachingResponses = false,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    AllowCachingResponses = false,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/health/full", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("full"),
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckResponseWriter.WriteFullResponseAsync,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapControllers();

app.Run();


// Helpers

static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await seeder.SeedAsync();
}

static async Task ApplyPendingMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

    var pending = await dbContext.Database.GetPendingMigrationsAsync();
    if (!pending.Any())
        return;

    Console.WriteLine($"Applying {pending.Count()} pending migration(s)...");
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("Database migrations applied successfully.");
}

static bool IsTestConnectionArg(string arg) =>
    string.Equals(arg, "--testconnection", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "-testconnection", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "/testconnection", StringComparison.OrdinalIgnoreCase);

static async Task RunConnectionTestAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        Console.WriteLine(canConnect
            ? "Database connection succeeded."
            : "Database connection failed.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Database connection failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
}

static string? FirstNonEmpty(params string?[] values)
{
    return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

static void LoadEnvFile()
{
    var contentRoot = Directory.GetCurrentDirectory();
    var environmentName =
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? "Production";

    var candidateNames = new List<string> { ".env", $".env.{environmentName}" };

    foreach (var file in candidateNames)
    {
        var fullPath = Path.Combine(contentRoot, file);
        if (!File.Exists(fullPath)) continue;

        foreach (var line in File.ReadAllLines(fullPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0) continue;

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}