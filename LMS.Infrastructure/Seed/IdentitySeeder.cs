using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMS_Backend.Infrastructure.Seed;

/// <summary>
/// Seeds the bootstrap admin user and role in an idempotent, environment-aware manner.
/// </summary>
public class IdentitySeeder
{
    private const string AdminRole = "Admin";
    private const string ConfigSection = "AdminBootstrap";

    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<IdentitySeeder> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        var adminEmail = _configuration[$"{ConfigSection}:Email"]?.Trim();
        var adminPassword = _configuration[$"{ConfigSection}:Password"]?.Trim();

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            _logger.LogWarning("Admin bootstrap email is not configured; skipping identity seeding.");
            return;
        }

        await EnsureRoleExistsAsync();

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                var level = _environment.IsProduction() ? LogLevel.Warning : LogLevel.Information;
                _logger.Log(level,
                    "Admin bootstrap password is not configured; cannot create admin user in {EnvironmentName}.",
                    _environment.EnvironmentName);
                return;
            }

            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create admin user {Email}: {Errors}", adminEmail, errors);
                throw new InvalidOperationException($"Failed to create admin user: {errors}");
            }

            _logger.LogInformation("Created bootstrap admin user {Email} in {EnvironmentName}.", adminEmail, _environment.EnvironmentName);
        }
        else
        {
            var updated = await EnsureAdminUserStateAsync(adminUser);
            if (updated)
            {
                _logger.LogInformation("Updated bootstrap admin user {Email} to ensure it stays active and confirmed.", adminEmail);
            }
            else
            {
                _logger.LogInformation("Bootstrap admin user {Email} already exists.", adminEmail);
            }
        }

        await EnsureUserInRoleAsync(adminUser);
    }

    private async Task EnsureRoleExistsAsync()
    {
        if (await _roleManager.RoleExistsAsync(AdminRole))
        {
            _logger.LogInformation("Role '{Role}' already exists.", AdminRole);
            return;
        }

        var roleResult = await _roleManager.CreateAsync(new IdentityRole(AdminRole));
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create role '{Role}': {Errors}", AdminRole, errors);
            throw new InvalidOperationException($"Failed to create '{AdminRole}' role: {errors}");
        }

        _logger.LogInformation("Created role '{Role}'.", AdminRole);
    }

    private async Task<bool> EnsureAdminUserStateAsync(User adminUser)
    {
        var changed = false;

        if (adminUser.Status != UserStatus.Active)
        {
            adminUser.Status = UserStatus.Active;
            changed = true;
        }

        if (!adminUser.EmailConfirmed)
        {
            adminUser.EmailConfirmed = true;
            changed = true;
        }

        if (!changed)
        {
            return false;
        }

        var updateResult = await _userManager.UpdateAsync(adminUser);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update admin user {Email}: {Errors}", adminUser.Email, errors);
            throw new InvalidOperationException($"Failed to update admin user: {errors}");
        }

        return true;
    }

    private async Task EnsureUserInRoleAsync(User adminUser)
    {
        if (await _userManager.IsInRoleAsync(adminUser, AdminRole))
        {
            _logger.LogInformation("Admin user {Email} already in role '{Role}'.", adminUser.Email, AdminRole);
            return;
        }

        var assignResult = await _userManager.AddToRoleAsync(adminUser, AdminRole);
        if (!assignResult.Succeeded)
        {
            var errors = string.Join(", ", assignResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to assign role '{Role}' to {Email}: {Errors}", AdminRole, adminUser.Email, errors);
            throw new InvalidOperationException($"Failed to assign '{AdminRole}' role: {errors}");
        }

        _logger.LogInformation("Ensured admin user {Email} has role '{Role}'.", adminUser.Email, AdminRole);
    }
}
