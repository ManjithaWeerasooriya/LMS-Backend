using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.LiveSessions;

public class LiveSessionAttendanceDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public DateTime? JoinTime { get; set; }
    public DateTime? LeaveTime { get; set; }
    public int DurationSeconds { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
