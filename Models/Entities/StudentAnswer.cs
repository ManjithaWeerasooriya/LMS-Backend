using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class StudentAnswer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid QuizAttemptId { get; set; }

    [ForeignKey(nameof(QuizAttemptId))]
    public QuizAttempt QuizAttempt { get; set; } = default!;

    [Required]
    public Guid QuestionId { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = default!;

    [MaxLength(8000)]
    public string? AnswerText { get; set; }

    /// <summary>
    /// Stores a file reference produced by an upload pipeline outside this module.
    /// </summary>
    [MaxLength(1000)]
    public string? FileReference { get; set; }

    public bool? IsCorrect { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal AwardedMarks { get; set; }

    public StudentAnswerReviewStatus ReviewStatus { get; set; }

    [MaxLength(2000)]
    public string? TeacherFeedback { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public ICollection<StudentAnswerOption> SelectedOptions { get; set; } = new List<StudentAnswerOption>();
}
