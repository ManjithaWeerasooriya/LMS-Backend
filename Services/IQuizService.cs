using LMS_Backend.Models.DTOs.Quiz;
namespace LMS_Backend.Services;

public interface IQuizService
{
    Task<QuizResponseDto> CreateQuizAsync(CreateQuizDto dto);

    Task<List<QuizResponseDto>> GetQuizzesByCourseAsync(Guid courseId);

    Task<QuizResponseDto?> GetQuizByIdAsync(Guid quizId);

    Task<QuizResponseDto?> UpdateQuizAsync(Guid quizId, UpdateQuizDto dto);

    Task<bool> DeleteQuizAsync(Guid quizId);
}