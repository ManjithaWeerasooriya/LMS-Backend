using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace LMS_Backend.Infrastructure.Seed;

/// <summary>
/// Seeds core identity data (roles and admin user) during development.
/// </summary>
public class IdentitySeeder
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    private const string AdminEmail = "admin@lms.local";
    private const string AdminPassword = "Admin123!";
    private const string AdminRole = "Admin";

    public IdentitySeeder(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
    }

    /// <summary>
    /// Ensures the Admin role and default admin user exist.
    /// </summary>
    public async Task SeedAsync()
    {
        var userManager = _userManager;
        var roleManager = _roleManager;

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AdminRole));
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create '{AdminRole}' role: {errors}");
            }
        }

        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FirstName = "System",
                LastName = "Admin",
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, AdminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create admin user: {errors}");
            }
        }
        else if (adminUser.Status != UserStatus.Active)
        {
            adminUser.Status = UserStatus.Active;
            await userManager.UpdateAsync(adminUser);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AdminRole))
        {
            var assignResult = await userManager.AddToRoleAsync(adminUser, AdminRole);
            if (!assignResult.Succeeded)
            {
                var errors = string.Join(", ", assignResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign '{AdminRole}' role: {errors}");
            }
        }
    }
}
