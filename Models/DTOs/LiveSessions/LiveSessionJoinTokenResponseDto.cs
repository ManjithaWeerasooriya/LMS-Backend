using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.LiveSessions;

public class LiveSessionJoinTokenResponseDto
{
    public string AcsUserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AcsEndpoint { get; set; }
    public MeetingType MeetingType { get; set; }
    public string? RoomId { get; set; }
    public string? ChatThreadId { get; set; }
    public LiveSessionJoinMetadataDto Session { get; set; } = new();
}
