using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.Courses;

public class CreateCourseRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(0, 1000)]
    public double DurationHours { get; set; }

    [Range(0, 999999)]
    public decimal Price { get; set; }

    [Range(1, 100000)]
    public int MaxStudents { get; set; } = 100;

    [MaxLength(50)]
    public string? DifficultyLevel { get; set; }

    [MaxLength(1000)]
    public string? Prerequisites { get; set; }
}

