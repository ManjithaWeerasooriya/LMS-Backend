using LMS_Backend.Models.DTOs.Admin;
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
}
