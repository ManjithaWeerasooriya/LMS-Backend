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
    public string StudentId { get; set; } = string.Empty;

    [ForeignKey(nameof(StudentId))]
    public User Student { get; set; } = default!;

    public int AttemptNumber { get; set; }

    public QuizAttemptStatus Status { get; set; } = QuizAttemptStatus.InProgress;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime DeadlineUtc { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Score { get; set; }

    public ICollection<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
}
