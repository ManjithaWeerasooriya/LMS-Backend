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
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public int DurationMinutes { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalMarks { get; set; }

    public bool RandomizeQuestions { get; set; }

    public bool AllowMultipleAttempts { get; set; }

    /// <summary>
    /// Controls whether the quiz is visible and available to enrolled students.
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Controls whether students can see scores and grading details for their attempts.
    /// </summary>
    public bool AreResultsPublished { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();

    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
