using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class LiveSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = default!;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public DateTime StartTime { get; set; }

    [Range(1, 1440)]
    public int DurationMinutes { get; set; }

    public LiveSessionStatus Status { get; set; } = LiveSessionStatus.Scheduled;

    public bool RecordingEnabled { get; set; }

    public bool PlaybackEnabled { get; set; }

    public MeetingType MeetingType { get; set; }

    [MaxLength(200)]
    public string? RoomId { get; set; }

    [MaxLength(200)]
    public string? ChatThreadId { get; set; }

    [MaxLength(300)]
    public string? AcsRecordingId { get; set; }

    public LiveSessionRecordingStatus RecordingStatus { get; set; } = LiveSessionRecordingStatus.NotStarted;

    [MaxLength(1000)]
    public string? RecordingUrl { get; set; }

    public DateTime? RecordingStartedAt { get; set; }

    public DateTime? RecordingStoppedAt { get; set; }

    [Required]
    public string CreatedByTeacherId { get; set; } = default!;

    [ForeignKey(nameof(CreatedByTeacherId))]
    public User CreatedByTeacher { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<LiveSessionAttendance> Attendances { get; set; } = new List<LiveSessionAttendance>();
}
