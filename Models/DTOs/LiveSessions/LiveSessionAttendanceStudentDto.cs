using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.LiveSessions;

public class LiveSessionAttendanceStudentDto
{
    public string StudentId { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public string? StudentEmail { get; set; }
    public DateTime? JoinTime { get; set; }
    public DateTime? LeaveTime { get; set; }
    public int DurationSeconds { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
