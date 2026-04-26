using System.Security.Claims;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/student/courses")]
[Authorize(Policy = AppPolicies.StudentOnly)]
public class StudentCoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public StudentCoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentCourseListItemDto>>> GetAvailableCourses(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        var courses = await _courseService.GetCoursesForStudentAsync(
            studentId,
            search,
            cancellationToken);

        return Ok(courses);
    }

    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<StudentCourseListItemDto>>> GetMyCourses(
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        var courses = await _courseService.GetEnrolledCoursesForStudentAsync(
            studentId,
            cancellationToken);

        return Ok(courses);
    }

    [HttpPost("{courseId:guid}/enroll")]
    public async Task<IActionResult> EnrollInCourse(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        var result = await _courseService.EnrollStudentInCourseAsync(
            courseId,
            studentId,
            cancellationToken);

        if (result.Success)
        {
            // Idempotent success (already enrolled or newly enrolled).
            return NoContent();
        }

        return result.ErrorCode switch
        {
            "CourseNotFound" => NotFound(new { message = result.ErrorMessage }),
            "AlreadyEnrolled" => Conflict(new { message = result.ErrorMessage }),
            "CourseNotActive" => BadRequest(new { message = result.ErrorMessage }),
            "CourseFull" => Conflict(new { message = result.ErrorMessage }),
            _ => StatusCode(500, new { message = "Unable to enroll in course." })
        };
    }
}
