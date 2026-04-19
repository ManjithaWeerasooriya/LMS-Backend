using System.ComponentModel.DataAnnotations;
using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.LiveSessions;

public class CreateLiveSessionRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Range(1, 1440)]
    public int DurationMinutes { get; set; }

    public bool RecordingEnabled { get; set; }

    public bool PlaybackEnabled { get; set; }

    public MeetingType MeetingType { get; set; }

    [MaxLength(200)]
    public string? RoomId { get; set; }

    [MaxLength(100)]
    public string? GroupId { get; set; }

    [MaxLength(1000)]
    public string? MeetingLink { get; set; }

    [MaxLength(200)]
    public string? MeetingId { get; set; }

    [MaxLength(200)]
    public string? Passcode { get; set; }

    [MaxLength(200)]
    public string? ChatThreadId { get; set; }
}
