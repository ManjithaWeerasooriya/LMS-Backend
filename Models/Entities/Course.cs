using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public enum CourseStatus
{
    Draft = 1,
    Active = 2,
    Archived = 3
}

public class Course
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = default!;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>
    /// Estimated duration in hours (for example, 40).
    /// </summary>
    public double DurationHours { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    /// <summary>
    /// Maximum number of students that can enroll in the course.
    /// </summary>
    public int MaxStudents { get; set; }

    /// <summary>
    /// Difficulty level label, e.g., "Beginner", "Intermediate", "Advanced".
    /// </summary>
    [MaxLength(50)]
    public string? DifficultyLevel { get; set; }

    [MaxLength(1000)]
    public string? Prerequisites { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.Draft;

    /// <summary>
    /// Teacher who owns this course (Identity User Id).
    /// </summary>
    [Required]
    public string TeacherId { get; set; } = default!;

    [ForeignKey(nameof(TeacherId))]
    public User Teacher { get; set; } = default!;

    public double? AverageRating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<LiveSession> LiveSessions { get; set; } = new List<LiveSession>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
