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
[Route("api/v1/student/courses/{courseId:guid}/discussions")]
[Authorize(Roles = "Student")]
public class StudentCourseDiscussionsController : ControllerBase
{
    private readonly CourseDiscussionService _discussionService;

    public StudentCourseDiscussionsController(CourseDiscussionService discussionService)
    {
        _discussionService = discussionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseDiscussionMessageDto>>> GetDiscussion(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        var isEnrolled = await _discussionService.IsStudentEnrolledInCourseAsync(
            courseId,
            studentId,
            cancellationToken);

        if (!isEnrolled)
        {
            return Forbid();
        }

        var messages = await _discussionService.GetDiscussionForCourseAsync(courseId, cancellationToken);
        return Ok(messages);
    }

    [HttpPost]
    public async Task<ActionResult<CourseDiscussionMessageDto>> PostMessage(
        Guid courseId,
        [FromBody] CreateCourseDiscussionMessageDto request,
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Message content is required." });
        }

        var isEnrolled = await _discussionService.IsStudentEnrolledInCourseAsync(
            courseId,
            studentId,
            cancellationToken);

        if (!isEnrolled)
        {
            return Forbid();
        }

        var message = await _discussionService.CreateMessageAsync(
            courseId,
            studentId,
            request.Content,
            request.ParentMessageId,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetDiscussion),
            new { courseId },
            message);
    }
}

