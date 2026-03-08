using System;

namespace LMS_Backend.Models.DTOs.Teacher;

public class TeacherDashboardCourseItemDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Students { get; set; }
    public double AverageProgressPercent { get; set; }
    public string Status { get; set; } = string.Empty;
}

