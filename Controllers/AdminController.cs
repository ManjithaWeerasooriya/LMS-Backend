using LMS_Backend.Models.DTOs.Admin;
using LMS_Backend.Models.Exceptions;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Returns a paginated list of users visible to administrators.
    /// </summary>
    /// <param name="query">Pagination and filtering options.</param>
    /// <returns>Paginated user collection.</returns>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] UserQueryParametersDto query)
    {
        var result = await _adminService.GetUsersAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Suspends a user account (Admin only).
    /// </summary>
    [HttpPatch("users/{id}/suspend")]
    public async Task<IActionResult> SuspendUser(string id, [FromBody] SuspendUserDto request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest(new { message = "UserId is required." });
        }

        if (!string.Equals(id, request.UserId, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Route id and payload user id must match." });
        }

        try
        {
            await _adminService.SuspendUserAsync(id, request.Reason);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reactivates a suspended user (Admin only).
    /// </summary>
    [HttpPatch("users/{id}/reactivate")]
    public async Task<IActionResult> ReactivateUser(string id)
    {
        try
        {
            await _adminService.ReactivateUserAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
