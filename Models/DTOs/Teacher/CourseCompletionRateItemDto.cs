using System;

namespace LMS_Backend.Models.DTOs.Teacher;

public class CourseCompletionRateItemDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public double CompletionRate { get; set; }
}

