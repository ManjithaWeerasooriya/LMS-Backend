using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.LiveSessions;

public class LiveSessionAttendanceSummaryDto
{
    public Guid SessionId { get; set; }
    public Guid CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public LiveSessionStatus Status { get; set; }
    public int TotalEnrolledStudents { get; set; }
    public int TotalJoinedStudents { get; set; }
    public int TotalCompletedAttendances { get; set; }
    public List<LiveSessionAttendanceStudentDto> Students { get; set; } = new();
}
