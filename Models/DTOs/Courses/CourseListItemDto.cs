using System;

namespace LMS_Backend.Models.DTOs.Courses;

public class CourseListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public int Students { get; set; }
    public decimal Price { get; set; }
    public double? Rating { get; set; }
    public string Status { get; set; } = string.Empty;
}

