namespace LMS_Backend.Models.DTOs.Student;

public class StudentQuizScoreItemDto
{
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal TotalMarks { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}
