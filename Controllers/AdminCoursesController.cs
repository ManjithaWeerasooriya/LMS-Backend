using System.Security.Claims;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/admin/courses")]
[Authorize(Policy = AppPolicies.TeacherOnly)]
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
        var result = await _courseService.GetCoursesForManagementAsync(options);
        return Ok(result);
    }

    [HttpPut("{id:guid}/disable")]
    public async Task<IActionResult> DisableCourse(Guid id)
    {
        var disabled = await _courseService.ArchiveCourseAsync(id);
        if (!disabled)
        {
            return NotFound();
        }

        LogTeacherAction("archived", id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        var deleted = await _courseService.DeleteCourseForManagementAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        LogTeacherAction("deleted", id);
        return NoContent();
    }

    private void LogTeacherAction(string action, Guid courseId)
    {
        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        _logger.LogInformation("Teacher {TeacherId} {Action} course {CourseId}.", teacherId, action, courseId);
    }
}
