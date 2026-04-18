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
    private readonly ILiveSessionJoinService _liveSessionJoinService;

    public LiveSessionsController(ILiveSessionJoinService liveSessionJoinService)
    {
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
}
