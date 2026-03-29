using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMS_Backend.Infrastructure.Seed;

/// <summary>
/// Seeds the bootstrap teacher user and role in an idempotent, environment-aware manner.
/// </summary>
public class IdentitySeeder
{
    private const string ConfigSection = "TeacherBootstrap";
    private const string LegacyConfigSection = "AdminBootstrap";

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

    /// <summary>
    /// Ensures the Teacher role and bootstrap teacher user exist.
    /// </summary>
    public async Task SeedAsync()
    {
        await EnsureTeacherRoleExistsAsync();
        await NormalizeLegacyAdminUsersAsync();
        await NormalizeLegacyPendingUsersAsync();

        var bootstrapEmail = GetBootstrapSetting("Email");
        var bootstrapPassword = GetBootstrapSetting("Password");

        if (string.IsNullOrWhiteSpace(bootstrapEmail))
        {
            _logger.LogWarning("Teacher bootstrap email is not configured; skipping bootstrap identity seeding.");
            return;
        }

        var bootstrapUser = await _userManager.FindByEmailAsync(bootstrapEmail);
        if (bootstrapUser == null)
        {
            if (string.IsNullOrWhiteSpace(bootstrapPassword))
            {
                var level = _environment.IsProduction() ? LogLevel.Warning : LogLevel.Information;
                _logger.Log(level,
                    "Teacher bootstrap password is not configured; cannot create bootstrap teacher in {EnvironmentName}.",
                    _environment.EnvironmentName);
                return;
            }

            bootstrapUser = new User
            {
                UserName = bootstrapEmail,
                Email = bootstrapEmail,
                FirstName = "System",
                LastName = "Teacher",
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(bootstrapUser, bootstrapPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create bootstrap teacher {Email}: {Errors}", bootstrapEmail, errors);
                throw new InvalidOperationException($"Failed to create bootstrap teacher: {errors}");
            }

            _logger.LogInformation("Created bootstrap teacher {Email} in {EnvironmentName}.", bootstrapEmail, _environment.EnvironmentName);
        }
        else
        {
            var updated = await EnsureBootstrapTeacherStateAsync(bootstrapUser);
            if (updated)
            {
                _logger.LogInformation("Updated bootstrap teacher {Email} to ensure it stays active and confirmed.", bootstrapEmail);
            }
            else
            {
                _logger.LogInformation("Bootstrap teacher {Email} already exists.", bootstrapEmail);
            }
        }

        await EnsureUserInTeacherRoleAsync(bootstrapUser);
    }

    private async Task EnsureTeacherRoleExistsAsync()
    {
        if (await _roleManager.RoleExistsAsync(AppRoles.Teacher))
        {
            _logger.LogInformation("Role '{Role}' already exists.", AppRoles.Teacher);
            return;
        }

        var roleResult = await _roleManager.CreateAsync(new IdentityRole(AppRoles.Teacher));
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create role '{Role}': {Errors}", AppRoles.Teacher, errors);
            throw new InvalidOperationException($"Failed to create '{AppRoles.Teacher}' role: {errors}");
        }

        _logger.LogInformation("Created role '{Role}'.", AppRoles.Teacher);
    }

    private async Task<bool> EnsureBootstrapTeacherStateAsync(User bootstrapUser)
    {
        var changed = false;

        if (bootstrapUser.Status != UserStatus.Active)
        {
            bootstrapUser.Status = UserStatus.Active;
            changed = true;
        }

        if (!bootstrapUser.EmailConfirmed)
        {
            bootstrapUser.EmailConfirmed = true;
            changed = true;
        }

        if (!changed)
        {
            return false;
        }

        var updateResult = await _userManager.UpdateAsync(bootstrapUser);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update bootstrap teacher {Email}: {Errors}", bootstrapUser.Email, errors);
            throw new InvalidOperationException($"Failed to update bootstrap teacher: {errors}");
        }

        return true;
    }

    private async Task EnsureUserInTeacherRoleAsync(User bootstrapUser)
    {
        if (await _userManager.IsInRoleAsync(bootstrapUser, AppRoles.Teacher))
        {
            await RemoveLegacyAdminRoleAsync(bootstrapUser);
            _logger.LogInformation("Bootstrap teacher {Email} already in role '{Role}'.", bootstrapUser.Email, AppRoles.Teacher);
        }
        else
        {
            var assignResult = await _userManager.AddToRoleAsync(bootstrapUser, AppRoles.Teacher);
            if (!assignResult.Succeeded)
            {
                var errors = string.Join(", ", assignResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role '{Role}' to {Email}: {Errors}", AppRoles.Teacher, bootstrapUser.Email, errors);
                throw new InvalidOperationException($"Failed to assign '{AppRoles.Teacher}' role: {errors}");
            }

            _logger.LogInformation("Ensured bootstrap teacher {Email} has role '{Role}'.", bootstrapUser.Email, AppRoles.Teacher);
        }

        await RemoveLegacyAdminRoleAsync(bootstrapUser);
    }

    private async Task NormalizeLegacyAdminUsersAsync()
    {
        if (!await _roleManager.RoleExistsAsync(AppRoles.LegacyAdmin))
        {
            return;
        }

        var legacyAdmins = await _userManager.GetUsersInRoleAsync(AppRoles.LegacyAdmin);
        foreach (var legacyAdmin in legacyAdmins)
        {
            if (!await _userManager.IsInRoleAsync(legacyAdmin, AppRoles.Teacher))
            {
                var addTeacherResult = await _userManager.AddToRoleAsync(legacyAdmin, AppRoles.Teacher);
                if (!addTeacherResult.Succeeded)
                {
                    var errors = string.Join(", ", addTeacherResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to migrate legacy admin '{legacyAdmin.Email}' to Teacher role: {errors}");
                }
            }

            await RemoveLegacyAdminRoleAsync(legacyAdmin);
        }

        var adminRole = await _roleManager.FindByNameAsync(AppRoles.LegacyAdmin);
        if (adminRole != null)
        {
            var deleteRoleResult = await _roleManager.DeleteAsync(adminRole);
            if (!deleteRoleResult.Succeeded)
            {
                var errors = string.Join(", ", deleteRoleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to delete legacy '{AppRoles.LegacyAdmin}' role: {errors}");
            }
        }
    }

    private async Task NormalizeLegacyPendingUsersAsync()
    {
        var legacyPendingStatus = (UserStatus)2;
        var pendingUsers = _userManager.Users
            .Where(user => user.Status == legacyPendingStatus)
            .ToList();

        foreach (var pendingUser in pendingUsers)
        {
            pendingUser.Status = UserStatus.Active;

            var updateResult = await _userManager.UpdateAsync(pendingUser);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to activate legacy pending user '{pendingUser.Email}': {errors}");
            }
        }
    }

    private string? GetBootstrapSetting(string name)
    {
        var preferred = _configuration[$"{ConfigSection}:{name}"]?.Trim();
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        return _configuration[$"{LegacyConfigSection}:{name}"]?.Trim();
    }

    private async Task RemoveLegacyAdminRoleAsync(User user)
    {
        if (!await _userManager.IsInRoleAsync(user, AppRoles.LegacyAdmin))
        {
            return;
        }

        var removeResult = await _userManager.RemoveFromRoleAsync(user, AppRoles.LegacyAdmin);
        if (!removeResult.Succeeded)
        {
            var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to remove legacy '{AppRoles.LegacyAdmin}' role from '{user.Email}': {errors}");
        }
    }
}
