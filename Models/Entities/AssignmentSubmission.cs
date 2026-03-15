using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public enum SubmissionStatus
{
    Pending = 1,
    Graded = 2
}

public class AssignmentSubmission
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AssignmentId { get; set; }

    [ForeignKey(nameof(AssignmentId))]
    public Assignment Assignment { get; set; } = default!;

    [Required]
    public string StudentId { get; set; } = default!;

    [ForeignKey(nameof(StudentId))]
    public User Student { get; set; } = default!;

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public int? Score { get; set; }

    [MaxLength(4000)]
    public string? Feedback { get; set; }
}

