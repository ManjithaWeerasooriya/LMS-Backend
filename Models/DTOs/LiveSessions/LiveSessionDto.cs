using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.LiveSessions;

public class LiveSessionDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public LiveSessionStatus Status { get; set; }
    public bool RecordingEnabled { get; set; }
    public bool PlaybackEnabled { get; set; }
    public MeetingType MeetingType { get; set; }
    public string? RoomId { get; set; }
    public string? ChatThreadId { get; set; }
    public string? AcsRecordingId { get; set; }
    public LiveSessionRecordingStatus RecordingStatus { get; set; }
    public string? RecordingUrl { get; set; }
    public DateTime? RecordingStartedAt { get; set; }
    public DateTime? RecordingStoppedAt { get; set; }
    public string CreatedByTeacherId { get; set; } = string.Empty;
    public string? TeacherName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
