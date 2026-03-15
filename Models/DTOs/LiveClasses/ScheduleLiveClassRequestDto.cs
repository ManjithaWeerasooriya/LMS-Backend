using System;
using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.LiveClasses;

public class ScheduleLiveClassRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    public Guid? CourseId { get; set; }

    /// <summary>
    /// Scheduled start time in UTC.
    /// </summary>
    [Required]
    public DateTime ScheduledAt { get; set; }

    [MaxLength(500)]
    public string? MeetingLink { get; set; }

    public bool EnableRecording { get; set; }

    [Range(1, 600)]
    public int? DurationMinutes { get; set; }
}

