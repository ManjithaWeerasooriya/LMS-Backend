using System.ComponentModel.DataAnnotations;

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

    [MaxLength(200)]
    public string? ChatThreadId { get; set; }
}
