namespace LMS_Backend.Models.DTOs.Quiz;

public class QuizResponseDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public decimal TotalMarks { get; set; }
    public bool RandomizeQuestions { get; set; }
    public bool AllowMultipleAttempts { get; set; }
    public bool IsPublished { get; set; }
    public bool AreResultsPublished { get; set; }
    public int QuestionCount { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
