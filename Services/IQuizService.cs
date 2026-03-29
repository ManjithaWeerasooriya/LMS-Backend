using LMS_Backend.Models.DTOs.Quiz;

namespace LMS_Backend.Services;

public interface IQuizService
{
    Task<IReadOnlyList<QuizResponseDto>> GetTeacherQuizzesByCourseAsync(string teacherId, Guid courseId, CancellationToken cancellationToken);
    Task<QuizResponseDto> GetTeacherQuizByIdAsync(string teacherId, Guid quizId, CancellationToken cancellationToken);
    Task<QuizResponseDto> CreateQuizAsync(string teacherId, CreateQuizDto dto, CancellationToken cancellationToken);
    Task<QuizResponseDto> UpdateQuizAsync(string teacherId, Guid quizId, UpdateQuizDto dto, CancellationToken cancellationToken);
    Task DeleteQuizAsync(string teacherId, Guid quizId, CancellationToken cancellationToken);
    Task<QuizResponseDto> SetResultsPublicationAsync(string teacherId, Guid quizId, bool publishResults, CancellationToken cancellationToken);

    Task<IReadOnlyList<QuestionResponseDto>> GetQuestionsAsync(string teacherId, Guid quizId, CancellationToken cancellationToken);
    Task<QuestionResponseDto> GetQuestionByIdAsync(string teacherId, Guid quizId, Guid questionId, CancellationToken cancellationToken);
    Task<QuestionResponseDto> CreateQuestionAsync(string teacherId, Guid quizId, CreateQuestionDto dto, CancellationToken cancellationToken);
    Task<QuestionResponseDto> UpdateQuestionAsync(string teacherId, Guid quizId, Guid questionId, UpdateQuestionDto dto, CancellationToken cancellationToken);
    Task DeleteQuestionAsync(string teacherId, Guid quizId, Guid questionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<QuizAttemptListItemDto>> GetQuizAttemptsAsync(string teacherId, Guid quizId, CancellationToken cancellationToken);
    Task<QuizAttemptDetailDto> GetQuizAttemptByIdAsync(string teacherId, Guid quizId, Guid attemptId, CancellationToken cancellationToken);
    Task<QuizAttemptDetailDto> GradeAnswerAsync(string teacherId, Guid quizId, Guid attemptId, Guid answerId, ManualGradeAnswerDto dto, CancellationToken cancellationToken);

    Task<IReadOnlyList<StudentQuizListItemDto>> GetStudentQuizzesAsync(string studentId, CancellationToken cancellationToken);
    Task<StudentQuizDetailDto> GetStudentQuizByIdAsync(string studentId, Guid quizId, CancellationToken cancellationToken);
    Task<StartQuizAttemptResponseDto> StartQuizAttemptAsync(string studentId, Guid quizId, CancellationToken cancellationToken);
    Task<QuizAttemptDetailDto> GetStudentAttemptByIdAsync(string studentId, Guid attemptId, CancellationToken cancellationToken);
    Task<QuizAttemptDetailDto> SubmitQuizAttemptAsync(string studentId, Guid attemptId, SubmitQuizAttemptDto dto, CancellationToken cancellationToken);
}
