using System.Security.Claims;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/courses")]
[Authorize(Roles = "Teacher")]
public class TeacherCoursesController : ControllerBase
{
    private readonly CourseService _courseService;

    public TeacherCoursesController(CourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseListItemDto>>> GetMyCourses(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var courses = await _courseService.GetCoursesForTeacherAsync(
            teacherId,
            search,
            cancellationToken);

        return Ok(courses);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(
        [FromBody] CreateCourseRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var course = await _courseService.CreateCourseAsync(teacherId, dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetMyCourses),
            new { id = course.Id },
            new { course.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCourse(
        Guid id,
        [FromBody] CreateCourseRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var updated = await _courseService.UpdateCourseAsync(id, teacherId, dto, cancellationToken);
        if (!updated) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCourse(
        Guid id,
        CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var deleted = await _courseService.DeleteCourseAsync(id, teacherId, cancellationToken);
        if (!deleted) return NotFound();

        return NoContent();
    }
}

