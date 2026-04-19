using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.LiveSessions;

public class LiveSessionRecordingDto
{
    public Guid SessionId { get; set; }
    public Guid CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public bool PlaybackEnabled { get; set; }
    public string? AcsRecordingId { get; set; }
    public LiveSessionRecordingStatus RecordingStatus { get; set; }
    public string? RecordingUrl { get; set; }
    public DateTime? RecordingStartedAt { get; set; }
    public DateTime? RecordingStoppedAt { get; set; }
}
