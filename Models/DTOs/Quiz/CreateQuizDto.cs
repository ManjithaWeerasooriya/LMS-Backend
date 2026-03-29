using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.Quiz;

public class CreateQuizDto : IValidatableObject
{
    [Required]
    public Guid CourseId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(1, 500)]
    public int DurationMinutes { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    [Range(typeof(decimal), "0.01", "999999")]
    public decimal TotalMarks { get; set; }

    public bool RandomizeQuestions { get; set; }

    public bool AllowMultipleAttempts { get; set; }

    public bool IsPublished { get; set; }

    public bool AreResultsPublished { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTimeUtc <= StartTimeUtc)
        {
            yield return new ValidationResult(
                "EndTimeUtc must be later than StartTimeUtc.",
                new[] { nameof(EndTimeUtc), nameof(StartTimeUtc) });
        }
    }
}
