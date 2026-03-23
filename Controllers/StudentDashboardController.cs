using System.Security.Claims;
using LMS_Backend.Models.DTOs.Student;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/student/dashboard")]
[Authorize(Roles = "Student")]
public class StudentDashboardController : ControllerBase
{
    private readonly StudentDashboardService _dashboardService;

    public StudentDashboardController(StudentDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<StudentDashboardResponseDto>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        var dashboard = await _dashboardService.GetDashboardAsync(studentId, cancellationToken);
        return Ok(dashboard);
    }
}

