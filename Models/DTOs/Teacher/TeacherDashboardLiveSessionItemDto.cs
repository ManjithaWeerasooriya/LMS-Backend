using System;

namespace LMS_Backend.Models.DTOs.Teacher;

public class TeacherDashboardLiveSessionItemDto
{
    public Guid LiveClassId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string? CourseTitle { get; set; }
    public int StudentsEnrolled { get; set; }
    public string? MeetingLink { get; set; }
}

