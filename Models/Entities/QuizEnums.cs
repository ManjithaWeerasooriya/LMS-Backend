namespace LMS_Backend.Models.Entities;

public enum QuestionType
{
    SingleMcq = 1,
    MultipleMcq = 2,
    TrueFalse = 3,
    ShortAnswer = 4,
    Essay = 5,
    FileUpload = 6
}

public enum QuizAttemptStatus
{
    InProgress = 1,
    Submitted = 2,
    PendingReview = 3,
    Graded = 4,
    Expired = 5
}

public enum StudentAnswerReviewStatus
{
    NotRequired = 1,
    PendingReview = 2,
    Reviewed = 3
}
