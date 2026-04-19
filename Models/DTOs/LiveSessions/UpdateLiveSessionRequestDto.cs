using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.LiveSessions;

public class UpdateLiveSessionRequestDto
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

    [MaxLength(200)]
    public string? AcsRoomId { get; set; }

    [MaxLength(500)]
    public string? AcsCallLocator { get; set; }

    [MaxLength(200)]
    public string? ChatThreadId { get; set; }
}
