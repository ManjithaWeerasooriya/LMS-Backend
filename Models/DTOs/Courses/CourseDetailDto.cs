using System;

namespace LMS_Backend.Models.DTOs.Courses;

public class CourseDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public double DurationHours { get; set; }
    public decimal Price { get; set; }
    public int MaxStudents { get; set; }
    public string? DifficultyLevel { get; set; }
    public string? Prerequisites { get; set; }
    public string Status { get; set; } = string.Empty;
}

