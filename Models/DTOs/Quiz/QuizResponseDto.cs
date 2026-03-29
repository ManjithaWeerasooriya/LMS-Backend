namespace LMS_Backend.Models.DTOs.Quiz;

public class QuizResponseDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = default!;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int PassingMarks { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}