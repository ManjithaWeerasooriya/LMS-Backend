using System.Security.Claims;
using System.Text;
using LMS_Backend.Models.DTOs.User;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Requires JWT auth
public class UsersController : ControllerBase
{
    private const string AccountDeletionTokenPurpose = "AccountDeletion";

    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        TokenService tokenService,
        IEmailSender emailSender,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileRequest>> GetMe(CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null) return Unauthorized();

        return Ok(ToProfileDto(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileRequest>> UpdateMe(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null) return Unauthorized();

        // Only update allowed fields
        if (request.FirstName is not null) user.FirstName = request.FirstName.Trim();
        if (request.LastName is not null) user.LastName = request.LastName.Trim();
        if (request.Phone is not null) user.Phone = request.Phone.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Profile update failed.",
                errors = result.Errors.Select(e => new { e.Code, e.Description })
            });
        }

        // Reload to ensure we return latest values (optional but nice)
        var updated = await _userManager.FindByIdAsync(user.Id);
        return Ok(ToProfileDto(updated!));
    }

    [HttpPost("me/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        // require Active users only
        if (user.Status != UserStatus.Active)
            return Forbid();

        // Secure: checks current password + applies password policy + rehash if needed
        var result = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Password change failed.",
                errors = result.Errors.Select(e => new { e.Code, e.Description })
            });
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await _tokenService.RevokeAllRefreshTokensForUserAsync(user.Id);
        await _signInManager.RefreshSignInAsync(user);

        return NoContent();
    }

    [HttpPost("me/delete-request")]
    public async Task<IActionResult> RequestAccountDeletion(CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest(new { message = "User does not have an email address on file." });
        }

        var token = await _userManager.GenerateUserTokenAsync(
            user,
            TokenOptions.DefaultProvider,
            AccountDeletionTokenPurpose);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var callbackUrl = BuildDeletionCallbackUrl(user.Id, encodedToken);

        var htmlBody = $"""
            <p>Hi {user.FirstName ?? "there"},</p>
            <p>You recently requested to permanently delete your LMS account. This action cannot be undone.</p>
            <p>Please confirm by clicking the link below:</p>
            <p><a href="{callbackUrl}">Delete my account</a></p>
            <p>If you did not make this request, simply ignore this email and your account will remain active.</p>
            <p>Confirmation Token (for support reference): <strong>{encodedToken}</strong></p>
            """;

        await _emailSender.SendEmailAsync(
            user.Email,
            "Confirm your LMS account deletion",
            htmlBody);

        _logger.LogInformation("Sent account deletion email to {UserId}", user.Id);

        return Ok(new
        {
            message = "A confirmation email has been sent. Follow the link in your inbox to finish deleting your account."
        });
    }

    [AllowAnonymous]
    [HttpGet("confirm-delete")]
    public async Task<IActionResult> ConfirmAccountDeletion([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { message = "Missing userId or token." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { message = "User not found or already deleted." });
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid token." });
        }

        var valid = await _userManager.VerifyUserTokenAsync(
            user,
            TokenOptions.DefaultProvider,
            AccountDeletionTokenPurpose,
            decodedToken);

        if (!valid)
        {
            return BadRequest(new { message = "Invalid or expired token." });
        }

        await _tokenService.RevokeAllRefreshTokensForUserAsync(user.Id);
        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            return StatusCode(500, new
            {
                message = "Failed to delete the account.",
                errors = deleteResult.Errors.Select(e => new { e.Code, e.Description })
            });
        }

        _logger.LogInformation("Deleted user {UserId} after email confirmation.", userId);

        return Ok(new { message = "Account deleted successfully." });
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return null;

        // UserManager methods don’t take CancellationToken; this is fine.
        return await _userManager.FindByIdAsync(userId);
    }

    private string BuildDeletionCallbackUrl(string userId, string encodedToken)
    {
        var values = new { userId, token = encodedToken };
        var link = Url.ActionLink(
            action: nameof(ConfirmAccountDeletion),
            controller: "Users",
            values: values,
            protocol: Request.Scheme,
            host: Request.Host.HasValue ? Request.Host.Value : null);

        if (!string.IsNullOrWhiteSpace(link))
        {
            return link;
        }

        var host = Request.Host.HasValue ? Request.Host.Value : "localhost";
        var scheme = string.IsNullOrEmpty(Request.Scheme) ? "https" : Request.Scheme;
        return $"{scheme}://{host}/api/v1/users/confirm-delete?userId={Uri.EscapeDataString(userId)}&token={encodedToken}";
    }

    private static UserProfileRequest ToProfileDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? "",
        FirstName = user.FirstName,
        LastName = user.LastName,
        Phone = user.Phone,
        Status = user.Status,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}
