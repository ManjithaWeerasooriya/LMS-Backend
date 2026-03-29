using System.ComponentModel.DataAnnotations;
using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.Quiz;

public class StudentQuizListItemDto
{
    public Guid QuizId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public decimal TotalMarks { get; set; }
    public bool AllowMultipleAttempts { get; set; }
    public bool ResultsPublished { get; set; }
    public int AttemptCount { get; set; }
}

public class StudentQuizDetailDto
{
    public Guid QuizId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public decimal TotalMarks { get; set; }
    public bool RandomizeQuestions { get; set; }
    public bool AllowMultipleAttempts { get; set; }
    public int AttemptCount { get; set; }
    public List<StudentQuestionDto> Questions { get; set; } = new();
}

public class StudentQuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public decimal Marks { get; set; }
    public int OrderIndex { get; set; }
    public List<StudentQuestionOptionDto> Options { get; set; } = new();
}

public class StudentQuestionOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class StartQuizAttemptResponseDto
{
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime DeadlineUtc { get; set; }
    public StudentQuizDetailDto Quiz { get; set; } = new();
}

public class SubmitQuizAttemptDto : IValidatableObject
{
    [Required]
    [MinLength(1)]
    public List<SubmitStudentAnswerDto> Answers { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var duplicateQuestionIds = Answers
            .GroupBy(a => a.QuestionId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateQuestionIds.Count > 0)
        {
            yield return new ValidationResult(
                "Each question can only be answered once per submission.",
                new[] { nameof(Answers) });
        }
    }
}

public class SubmitStudentAnswerDto : IValidatableObject
{
    [Required]
    public Guid QuestionId { get; set; }

    public List<Guid> SelectedOptionIds { get; set; } = new();

    [MaxLength(8000)]
    public string? AnswerText { get; set; }

    [MaxLength(1000)]
    public string? FileReference { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SelectedOptionIds.Count > 0 && (!string.IsNullOrWhiteSpace(AnswerText) || !string.IsNullOrWhiteSpace(FileReference)))
        {
            yield return new ValidationResult(
                "Objective and text/file answer payloads cannot be mixed for the same question.",
                new[] { nameof(SelectedOptionIds), nameof(AnswerText), nameof(FileReference) });
        }
    }
}

public class QuizAttemptListItemDto
{
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public QuizAttemptStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime DeadlineUtc { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal Score { get; set; }
}

public class QuizAttemptDetailDto
{
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public QuizAttemptStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime DeadlineUtc { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    public bool ResultsPublished { get; set; }
    public List<QuizAttemptAnswerDto> Answers { get; set; } = new();
}

public class QuizAttemptAnswerDto
{
    public Guid AnswerId { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public decimal MaxMarks { get; set; }
    public List<Guid> SelectedOptionIds { get; set; } = new();
    public List<string> SelectedOptionTexts { get; set; } = new();
    public string? AnswerText { get; set; }
    public string? FileReference { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal? AwardedMarks { get; set; }
    public StudentAnswerReviewStatus ReviewStatus { get; set; }
    public string? TeacherFeedback { get; set; }
    public List<QuestionOptionResponseDto> Options { get; set; } = new();
}

public class ManualGradeAnswerDto
{
    [Range(typeof(decimal), "0", "999999")]
    public decimal AwardedMarks { get; set; }

    [MaxLength(2000)]
    public string? TeacherFeedback { get; set; }
}
