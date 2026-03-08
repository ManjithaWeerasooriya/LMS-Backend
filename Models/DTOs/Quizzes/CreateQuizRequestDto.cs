using System;
using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.Quizzes;

public class CreateQuizRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public Guid CourseId { get; set; }

    [Range(1, 600)]
    public int DurationMinutes { get; set; }

    [Range(1, 1000)]
    public int TotalMarks { get; set; }

    [Range(0, 1000)]
    public int PassingMarks { get; set; }
}

