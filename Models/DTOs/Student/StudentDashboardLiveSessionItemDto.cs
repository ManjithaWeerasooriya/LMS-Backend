using System;
using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.Student;

public class StudentDashboardLiveSessionItemDto
{
    public Guid LiveSessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CourseTitle { get; set; }
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public LiveSessionStatus Status { get; set; }
}
