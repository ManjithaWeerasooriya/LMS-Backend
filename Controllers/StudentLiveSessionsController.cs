using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/student")]
[Authorize(Policy = AppPolicies.StudentOnly)]
public class StudentLiveSessionsController : ApiControllerBase
{
    private readonly ILiveSessionService _liveSessionService;

    public StudentLiveSessionsController(ILiveSessionService liveSessionService)
    {
        _liveSessionService = liveSessionService;
    }

    [HttpGet("courses/{courseId:guid}/live-sessions")]
    public async Task<IActionResult> GetLiveSessionsByCourse(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var sessions = await _liveSessionService.GetStudentLiveSessionsByCourseAsync(
                studentId,
                courseId,
                cancellationToken);

            return Success(sessions, "Live sessions retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("live-sessions/{sessionId:guid}")]
    public async Task<IActionResult> GetLiveSessionById(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var session = await _liveSessionService.GetStudentLiveSessionByIdAsync(
                studentId,
                sessionId,
                cancellationToken);

            return Success(session, "Live session retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
