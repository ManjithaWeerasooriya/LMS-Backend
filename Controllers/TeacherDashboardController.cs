using System.Security.Claims;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Teacher;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/dashboard")]
[Authorize(Policy = AppPolicies.TeacherOnly)]
public class TeacherDashboardController : ControllerBase
{
    private readonly TeacherDashboardService _dashboardService;

    public TeacherDashboardController(TeacherDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<TeacherDashboardResponseDto>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var dashboard = await _dashboardService.GetDashboardAsync(teacherId, cancellationToken);
        return Ok(dashboard);
    }
}
