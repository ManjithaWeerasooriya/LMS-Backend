using System;

namespace LMS_Backend.Models.DTOs.Courses;

public class StudentCourseListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public int StudentsEnrolled { get; set; }
    public decimal Price { get; set; }
    public double? Rating { get; set; }
    public bool IsEnrolled { get; set; }
}

