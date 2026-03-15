using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class CourseEnrollment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = default!;

    [Required]
    public string StudentId { get; set; } = default!;

    [ForeignKey(nameof(StudentId))]
    public User Student { get; set; } = default!;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Overall course progress for the student as a percentage (0-100).
    /// </summary>
    public double ProgressPercent { get; set; }

    public DateTime? CompletedAt { get; set; }
}

