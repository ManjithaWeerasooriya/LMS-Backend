using System.ComponentModel.DataAnnotations;
using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.LiveSessions;

public class CreateLiveSessionRequestDto
{
    [Required]
    public Guid CourseId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Range(1, 1440)]
    public int DurationMinutes { get; set; }

    public LiveSessionStatus Status { get; set; } = LiveSessionStatus.Scheduled;

    public bool RecordingEnabled { get; set; }

    public bool PlaybackEnabled { get; set; }

    [MaxLength(200)]
    public string? AcsRoomId { get; set; }

    [MaxLength(500)]
    public string? AcsCallLocator { get; set; }

    [MaxLength(200)]
    public string? ChatThreadId { get; set; }
}
