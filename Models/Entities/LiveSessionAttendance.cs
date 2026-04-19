using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class LiveSessionAttendance
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SessionId { get; set; }

    [ForeignKey(nameof(SessionId))]
    public LiveSession Session { get; set; } = default!;

    [Required]
    public string StudentId { get; set; } = default!;

    [ForeignKey(nameof(StudentId))]
    public User Student { get; set; } = default!;

    public DateTime? JoinTime { get; set; }

    public DateTime? LeaveTime { get; set; }

    public int DurationSeconds { get; set; }

    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Pending;

    public DateTime? LastSeenAt { get; set; }
}
