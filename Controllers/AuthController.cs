using LMS_Backend.Models.DTOs.Auth;
using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LMS_Backend.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly IEmailSender _emailSender;

    public AuthController(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<User> signInManager,
        TokenService tokenService,
        IConfiguration config,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _config = config;
        _emailSender = emailSender;
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

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var verifyUrl =
            Url.Action(
                action: "ConfirmEmail",
                controller: "Auth",
                values: new { userId = user.Id, token = encodedToken },
                protocol: Request.Scheme
            );

        await _emailSender.SendEmailAsync(
            user.Email!,
            "Verify your email",
            $"""
            <p>Hi {user.FirstName ?? "there"},</p>
            <p>Please verify your email by clicking the link below:</p>
            <p><a href="{verifyUrl}">Verify Email</a></p>
            <p>If you didn’t create this account, ignore this email.</p>
            """
        );
        // IMPORTANT: user must NOT be able to login until EmailConfirmed is true.

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

        if (!user.EmailConfirmed)
            return Unauthorized(new { message = "Please verify your email before logging in." });

        // Existing rule still applies:
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

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return BadRequest(new { message = "Invalid user." });

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
            return BadRequest(new { message = "Email verification failed.", errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Email verified successfully. You can now login (teachers still require admin approval)." });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return Ok(new { message = "If the account exists, a verification email was sent." });
        if (user.EmailConfirmed) return Ok(new { message = "Email is already verified." });

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var verifyUrl =
            Url.Action(
                action: "ConfirmEmail",
                controller: "Auth",
                values: new { userId = user.Id, token = encodedToken },
                protocol: Request.Scheme
            );

        await _emailSender.SendEmailAsync(user.Email!, "Verify your email", $"<p><a href=\"{verifyUrl}\">Verify Email</a></p>");

        return Ok(new { message = "Verification email sent." });
    }
}
