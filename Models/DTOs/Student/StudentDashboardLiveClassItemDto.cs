using System;

namespace LMS_Backend.Models.DTOs.Student;

public class StudentDashboardLiveClassItemDto
{
    public Guid LiveClassId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? CourseTitle { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int? DurationMinutes { get; set; }
}

