using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/live-sessions")]
[Authorize(Roles = AppRoles.Teacher + "," + AppRoles.Student)]
public class LiveSessionsController : ApiControllerBase
{
    private readonly ILiveSessionService _liveSessionService;
    private readonly ILiveSessionJoinService _liveSessionJoinService;

    public LiveSessionsController(
        ILiveSessionService liveSessionService,
        ILiveSessionJoinService liveSessionJoinService)
    {
        _liveSessionService = liveSessionService;
        _liveSessionJoinService = liveSessionJoinService;
    }

    [HttpPost("{sessionId:guid}/join-token")]
    public async Task<IActionResult> CreateJoinToken(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var response = await _liveSessionJoinService.CreateJoinTokenAsync(
                userId,
                sessionId,
                cancellationToken);

            return Success(response, "Live session join token generated successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{sessionId:guid}/attendance/join")]
    [Authorize(Policy = AppPolicies.StudentOnly)]
    public async Task<IActionResult> JoinAttendance(
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
            var attendance = await _liveSessionService.JoinAttendanceAsync(
                studentId,
                sessionId,
                cancellationToken);

            return Success(attendance, "Live session attendance joined successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{sessionId:guid}/attendance/leave")]
    [Authorize(Policy = AppPolicies.StudentOnly)]
    public async Task<IActionResult> LeaveAttendance(
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
            var attendance = await _liveSessionService.LeaveAttendanceAsync(
                studentId,
                sessionId,
                cancellationToken);

            return Success(attendance, "Live session attendance left successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
