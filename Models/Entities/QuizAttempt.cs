using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class QuizAttempt
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid QuizId { get; set; }

    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; } = default!;

    [Required]
    public string StudentId { get; set; } = default!;

    [ForeignKey(nameof(StudentId))]
    public User Student { get; set; } = default!;

    /// <summary>
    /// Marks obtained by the student for this attempt.
    /// </summary>
    public int Score { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

