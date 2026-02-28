using System.Security.Claims;
using LMS_Backend.Models.DTOs.User;
using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Requires JWT auth
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public UsersController(UserManager<User> userManager)
    {
        _userManager = userManager;
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

    private async Task<User?> GetCurrentUserAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return null;

        // UserManager methods don’t take CancellationToken; this is fine.
        return await _userManager.FindByIdAsync(userId);
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