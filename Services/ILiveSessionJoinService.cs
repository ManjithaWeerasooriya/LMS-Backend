using LMS_Backend.Models.DTOs.LiveSessions;

namespace LMS_Backend.Services;

public interface ILiveSessionJoinService
{
    Task<LiveSessionJoinTokenResponseDto> CreateJoinTokenAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken);
}
