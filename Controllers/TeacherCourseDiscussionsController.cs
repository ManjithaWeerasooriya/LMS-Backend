using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/courses/{courseId:guid}/discussions")]
[Authorize(Roles = "Teacher")]
public class TeacherCourseDiscussionsController : ControllerBase
{
    private readonly CourseDiscussionService _discussionService;

    public TeacherCourseDiscussionsController(CourseDiscussionService discussionService)
    {
        _discussionService = discussionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseDiscussionMessageDto>>> GetDiscussionForTeacher(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var ownsCourse = await _discussionService.IsTeacherOwnerOfCourseAsync(
            courseId,
            teacherId,
            cancellationToken);

        if (!ownsCourse)
        {
            return Forbid();
        }

        var messages = await _discussionService.GetDiscussionForCourseAsync(courseId, cancellationToken);
        return Ok(messages);
    }

    [HttpPost]
    public async Task<ActionResult<CourseDiscussionMessageDto>> PostMessageAsTeacher(
        Guid courseId,
        [FromBody] CreateCourseDiscussionMessageDto request,
        CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Message content is required." });
        }

        var ownsCourse = await _discussionService.IsTeacherOwnerOfCourseAsync(
            courseId,
            teacherId,
            cancellationToken);

        if (!ownsCourse)
        {
            return Forbid();
        }

        var message = await _discussionService.CreateMessageAsync(
            courseId,
            teacherId,
            request.Content,
            request.ParentMessageId,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetDiscussionForTeacher),
            new { courseId },
            message);
    }
}
