using System.Security.Claims;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/admin/courses")]
[Authorize(Roles = "Admin")]
public class AdminCoursesController : ControllerBase
{
    private readonly CourseService _courseService;
    private readonly ILogger<AdminCoursesController> _logger;

    public AdminCoursesController(CourseService courseService, ILogger<AdminCoursesController> logger)
    {
        _courseService = courseService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CourseListItemDto>>> GetCourses(
        [FromQuery] CourseQueryParametersDto query)
    {
        var options = query?.ToOptions() ?? new CourseQueryOptions();
        var result = await _courseService.GetCoursesForAdminAsync(options);
        return Ok(result);
    }

    [HttpPut("{id:guid}/disable")]
    public async Task<IActionResult> DisableCourse(Guid id)
    {
        var disabled = await _courseService.DisableCourseAdminAsync(id);
        if (!disabled)
        {
            return NotFound();
        }

        LogAdminAction("archived", id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        var deleted = await _courseService.DeleteCourseAdminAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        LogAdminAction("deleted", id);
        return NoContent();
    }

    private void LogAdminAction(string action, Guid courseId)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        _logger.LogInformation("Admin {AdminId} {Action} course {CourseId}.", adminId, action, courseId);
    }
}
