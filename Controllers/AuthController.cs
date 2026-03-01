using LMS_Backend.Models.DTOs.Auth;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using LMS_Backend.Models.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TokenService _tokenService;
    private readonly BootstrapAdminOptions _bootstrapAdminOptions;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<User> signInManager,
        TokenService tokenService,
        IOptions<BootstrapAdminOptions> bootstrapAdminOptions,
        IPasswordHasher<User> passwordHasher)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _bootstrapAdminOptions = bootstrapAdminOptions.Value ?? new BootstrapAdminOptions();
        _passwordHasher = passwordHasher;
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
        if (TryHandleBootstrapAdminLogin(req, out var bootstrapResult))
        {
            return bootstrapResult!;
        }

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

        var userAgent = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var refreshToken = await _tokenService.CreateOrReplaceRefreshTokenAsync(user, req.DeviceId, userAgent, ip);

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
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var user = await _tokenService.ValidateRefreshTokenAsync(req.RefreshToken, req.DeviceId);
        if (user == null || user.Status != UserStatus.Active)
            return Unauthorized(new { message = "Invalid refresh token." });

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Student";

        var (accessToken, expiresIn) = await _tokenService.CreateAccessTokenAsync(user);

        var userAgent = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        var newRefreshToken = await _tokenService.CreateOrReplaceRefreshTokenAsync(user, req.DeviceId, userAgent, ip);

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
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req)
    {
        await _tokenService.RevokeRefreshTokenAsync(req.RefreshToken, req.DeviceId);
        return Ok(new { message = "Logged out." });
    }

    private bool TryHandleBootstrapAdminLogin(LoginRequest req, out IActionResult? result)
    {
        result = null;
        if (!_bootstrapAdminOptions.Enabled ||
            string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Email) ||
            string.IsNullOrWhiteSpace(_bootstrapAdminOptions.PasswordHash))
        {
            return false;
        }

        if (!string.Equals(req.Email, _bootstrapAdminOptions.Email, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var pseudoUser = new User
        {
            Id = string.IsNullOrWhiteSpace(_bootstrapAdminOptions.UserId)
                ? "bootstrap-admin"
                : _bootstrapAdminOptions.UserId,
            Email = _bootstrapAdminOptions.Email,
            UserName = _bootstrapAdminOptions.Email,
            Status = UserStatus.Active
        };

        var verification = _passwordHasher.VerifyHashedPassword(pseudoUser, _bootstrapAdminOptions.PasswordHash, req.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            result = Unauthorized(new { message = "Invalid credentials." });
            return true;
        }

        var (accessToken, expiresIn) = _tokenService.CreateBootstrapAdminToken(
            pseudoUser.Id,
            pseudoUser.Email ?? string.Empty,
            pseudoUser.UserName ?? pseudoUser.Email ?? string.Empty,
            pseudoUser.Status);

        result = Ok(new
        {
            accessToken,
            refreshToken = (string?)null,
            expiresIn,
            tokenType = "Bearer",
            user = new
            {
                id = pseudoUser.Id,
                email = pseudoUser.Email,
                username = pseudoUser.UserName,
                role = "Admin"
            }
        });

        return true;
    }
}
