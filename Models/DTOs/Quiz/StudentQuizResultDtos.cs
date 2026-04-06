using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.Quiz;

public class StudentQuizResultDto
{
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public Guid AttemptId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public QuizAttemptStatus Status { get; set; }
    public decimal TotalMarks { get; set; }
    public decimal? AwardedMarks { get; set; }
    public decimal? Percentage { get; set; }
    public bool AreResultsPublished { get; set; }
    public List<StudentQuizQuestionResultDto> QuestionResults { get; set; } = new();
}

public class StudentQuizQuestionResultDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public StudentAnswerReviewStatus ReviewStatus { get; set; }
    public decimal? AwardedMarks { get; set; }
    public decimal MaxMarks { get; set; }
    public string? Feedback { get; set; }
}
