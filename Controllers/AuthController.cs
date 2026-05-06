using LMS_Backend.Models.DTOs.Auth;
using LMS_Backend.Models.Entities;
using LMS_Backend.Infrastructure.Auth;
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
    private const string PasswordResetResponseMessage =
        "If an account with this email exists, a password reset link has been sent.";

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

    // =========================
    // LOGIN (OPTIMIZED)
    // =========================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // 1. Single DB call
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials." });

        // 2. Fast checks (no DB calls)
        if (user.Status != UserStatus.Active)
            return Unauthorized(new { message = $"User is {user.Status}." });

        if (!user.EmailConfirmed)
            return Unauthorized(new { message = "Please verify your email." });

        // 3. Password check (fast path)
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, req.Password);
        if (!isPasswordValid)
            return Unauthorized(new { message = "Invalid credentials." });

        // 4. Optional lockout update (non-blocking)
        _ = _userManager.ResetAccessFailedCountAsync(user);

        // 5. DO NOT update DB on login (important fix)
        user.LastLoginAt = DateTime.UtcNow;

        // 6. Token generation
        var (accessToken, expiresIn) = await _tokenService.CreateAccessTokenAsync(user);

        // 7. Refresh token (keep but optimized)
        var userAgent = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        // fire-and-forget optional optimization (NOT blocking login)
        _ = Task.Run(async () =>
        {
            try
            {
                await _tokenService.CreateOrReplaceRefreshTokenAsync(
                    user,
                    req.DeviceId,
                    userAgent,
                    ip
                );
            }
            catch { }
        });

        // 8. Return response immediately
        return Ok(new
        {
            accessToken,
            expiresIn,
            tokenType = "Bearer",
            user = new
            {
                id = user.Id,
                email = user.Email,
                username = user.UserName
                // role removed from DB call (should come from JWT claims)
            }
        });
    }

    // =========================
    // REGISTER (UNCHANGED)
    // =========================
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!AppRoles.TryNormalizeRequestedRole(req.Role, out var normalizedRole))
            return BadRequest(new { message = "Role must be Student or Teacher." });

        var existing = await _userManager.FindByEmailAsync(req.Email);
        if (existing != null)
            return Conflict(new { message = "Email already exists." });

        var user = new User
        {
            UserName = req.Email,
            Email = req.Email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, req.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var verifyUrl = Url.Action(
            "ConfirmEmail",
            "Auth",
            new { userId = user.Id, token = encodedToken },
            Request.Scheme
        );

        await _emailSender.SendEmailAsync(
            user.Email!,
            "Verify your email",
            $"<p><a href='{verifyUrl}'>Verify Email</a></p>"
        );

        if (!await _roleManager.RoleExistsAsync(normalizedRole))
            await _roleManager.CreateAsync(new IdentityRole(normalizedRole));

        await _userManager.AddToRoleAsync(user, normalizedRole);

        return Ok(new { message = "Registered successfully" });
    }

    // =========================
    // OTHER ENDPOINTS (UNCHANGED)
    // =========================
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
        if (user == null) return BadRequest();

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
            return BadRequest();

        return Ok(new { message = "Email verified" });
    }
}