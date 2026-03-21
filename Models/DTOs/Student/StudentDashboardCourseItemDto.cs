using System;

namespace LMS_Backend.Models.DTOs.Student;

public class StudentDashboardCourseItemDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public double ProgressPercent { get; set; }
}

