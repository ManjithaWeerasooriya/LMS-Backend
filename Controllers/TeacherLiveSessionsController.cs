using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.LiveSessions;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher")]
[Authorize(Policy = AppPolicies.TeacherOnly)]
public class TeacherLiveSessionsController : ApiControllerBase
{
    private readonly ILiveSessionService _liveSessionService;

    public TeacherLiveSessionsController(ILiveSessionService liveSessionService)
    {
        _liveSessionService = liveSessionService;
    }

    [HttpPost("courses/{courseId:guid}/live-sessions")]
    public async Task<IActionResult> CreateLiveSession(
        Guid courseId,
        [FromBody] CreateLiveSessionRequestDto dto,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var session = await _liveSessionService.CreateLiveSessionAsync(
                teacherId,
                courseId,
                dto,
                cancellationToken);

            return CreatedResponse(
                nameof(GetLiveSessionsByCourse),
                new { courseId },
                session,
                "Live session created successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("live-sessions/{sessionId:guid}")]
    public async Task<IActionResult> UpdateLiveSession(
        Guid sessionId,
        [FromBody] UpdateLiveSessionRequestDto dto,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var session = await _liveSessionService.UpdateLiveSessionAsync(
                teacherId,
                sessionId,
                dto,
                cancellationToken);

            return Success(session, "Live session updated successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("live-sessions/{sessionId:guid}")]
    public async Task<IActionResult> CancelLiveSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            await _liveSessionService.CancelLiveSessionAsync(
                teacherId,
                sessionId,
                cancellationToken);

            return SuccessMessage("Live session cancelled successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("courses/{courseId:guid}/live-sessions")]
    public async Task<IActionResult> GetLiveSessionsByCourse(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var sessions = await _liveSessionService.GetTeacherLiveSessionsByCourseAsync(
                teacherId,
                courseId,
                cancellationToken);

            return Success(sessions, "Live sessions retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
