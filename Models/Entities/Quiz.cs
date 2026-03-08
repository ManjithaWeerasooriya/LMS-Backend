using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class Quiz
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = default!;

    /// <summary>
    /// Estimated time to complete the quiz (in minutes), e.g., 30.
    /// </summary>
    public int DurationMinutes { get; set; }

    public int TotalMarks { get; set; }
    public int PassingMarks { get; set; }

    /// <summary>
    /// Indicates whether the quiz is visible to students.
    /// </summary>
    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}

