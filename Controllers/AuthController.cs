using LMS_Backend.Models.DTOs.Auth;
using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LMS_Backend.Services;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<User> signInManager,
        TokenService tokenService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var requestedRole = req.Role?.Trim();
        if (string.IsNullOrWhiteSpace(requestedRole))
            return BadRequest(new { message = "Role must be Student or Teacher." });

        var isStudent = string.Equals(requestedRole, "Student", StringComparison.OrdinalIgnoreCase);
        var isTeacher = string.Equals(requestedRole, "Teacher", StringComparison.OrdinalIgnoreCase);
        if (!isStudent && !isTeacher)
            return BadRequest(new { message = "Role must be Student or Teacher." });

        var normalizedRole = isTeacher ? "Teacher" : "Student";

        // Check email uniqueness
        var existing = await _userManager.FindByEmailAsync(req.Email);
        if (existing != null)
            return Conflict(new { message = "Email already exists." });

        var user = new User
        {
            UserName = req.Email,
            Email = req.Email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Status = isTeacher ? UserStatus.Pending : UserStatus.Active,
            Phone = null,
            CreatedAt = DateTime.UtcNow
        };

        // Identity will hash the password into PasswordHash
        var createResult = await _userManager.CreateAsync(user, req.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed.",
                errors = createResult.Errors.Select(e => e.Description)
            });
        }

        // Ensure the target role exists
        if (!await _roleManager.RoleExistsAsync(normalizedRole))
            await _roleManager.CreateAsync(new IdentityRole(normalizedRole));

        if (isStudent)
        {
            await _userManager.AddToRoleAsync(user, normalizedRole);
        }
        // Teachers keep Status = Pending and will receive the Teacher role during admin approval.

        return Ok(new
        {
            message = isTeacher
                ? "Registered. Waiting for admin approval."
                : "Registered successfully.",
            userId = user.Id,
            status = user.Status.ToString(),
            role = normalizedRole
        });
    }

[HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials." });

        if (user.Status != UserStatus.Active)
            return Unauthorized(new { message = $"User is {user.Status}." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Student";

        var (accessToken, expiresIn) = await _tokenService.CreateAccessTokenAsync(user);
        var refreshToken = await _tokenService.CreateAndStoreRefreshTokenAsync(user);

        return Ok(new
        {
            accessToken,
            refreshToken,
            expiresIn,
            tokenType = "Bearer",
            user = new
            {
                id = user.Id,                // string (Identity default)
                email = user.Email,
                username = user.UserName,
                role
            }
        });
    }

    // Refresh endpoint (client sends refreshToken)
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var user = await _tokenService.ValidateRefreshTokenAsync(refreshToken);
        if (user == null || user.Status != UserStatus.Active)
            return Unauthorized(new { message = "Invalid refresh token." });

        // rotate refresh token (recommended)
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Student";

        var (accessToken, expiresIn) = await _tokenService.CreateAccessTokenAsync(user);
        var newRefreshToken = await _tokenService.CreateAndStoreRefreshTokenAsync(user);

        return Ok(new
        {
            accessToken,
            refreshToken = newRefreshToken,
            expiresIn,
            tokenType = "Bearer",
            user = new
            {
                id = user.Id,
                email = user.Email,
                username = user.UserName,
                role
            }
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);
        return Ok(new { message = "Logged out." });
    }
}
