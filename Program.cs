using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LMS_Backend.Data;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Infrastructure.Seed;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using LMS_Backend.Services.Reporting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;

LoadEnvFile();

var testConnectionRequested = args.Any(IsTestConnectionArg);
var filteredArgs = testConnectionRequested
    ? args.Where(arg => !IsTestConnectionArg(arg)).ToArray()
    : args;

var builder = WebApplication.CreateBuilder(filteredArgs);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            return new BadRequestObjectResult(
                LMS_Backend.Models.DTOs.Common.ApiResponse<object?>.ErrorResponse(
                    "Validation failed.",
                    errors));
        };
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPublicService, PublicService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<AzureStorageService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase) ||
                origin.StartsWith("https://localhost:", StringComparison.OrdinalIgnoreCase) ||
                origin.Contains(".vercel.app", StringComparison.OrdinalIgnoreCase))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlServer(connectionString));

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
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<TeacherDashboardService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<LiveClassService>();
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

    options.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
    options.OperationFilter<FileUploadOperationFilter>();

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

// Apply migrations + seed
await ApplyPendingMigrationsAsync(app);
await SeedIdentityAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();


// ======================
// Helpers
// ======================

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