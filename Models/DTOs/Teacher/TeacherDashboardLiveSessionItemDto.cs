using System;
using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.Teacher;

public class TeacherDashboardLiveSessionItemDto
{
    public Guid LiveSessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string? CourseTitle { get; set; }
    public int StudentsEnrolled { get; set; }
    public LiveSessionStatus Status { get; set; }
}
