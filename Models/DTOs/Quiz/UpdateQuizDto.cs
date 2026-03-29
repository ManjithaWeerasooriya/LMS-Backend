using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.Quiz;

public class UpdateQuizDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = default!;

    [Range(1, 500)]
    public int DurationMinutes { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalMarks { get; set; }

    [Range(0, int.MaxValue)]
    public int PassingMarks { get; set; }

    public bool IsPublished { get; set; }
}