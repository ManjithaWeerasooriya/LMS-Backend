using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class LiveClass
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = default!;

    public Guid? CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [Required]
    public string TeacherId { get; set; } = default!;

    [ForeignKey(nameof(TeacherId))]
    public User Teacher { get; set; } = default!;

    /// <summary>
    /// Scheduled start date and time in UTC.
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    [MaxLength(500)]
    public string? MeetingLink { get; set; }

    public bool EnableRecording { get; set; }

    public int? DurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

