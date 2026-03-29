using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class QuizService : IQuizService
{
    private readonly ApplicationDBContext _context;

    public QuizService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<QuizResponseDto> CreateQuizAsync(CreateQuizDto dto)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
            throw new Exception("Course not found.");

        if (dto.PassingMarks > dto.TotalMarks)
            throw new Exception("Passing marks cannot be greater than total marks.");

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            CourseId = dto.CourseId,
            Title = dto.Title,
            DurationMinutes = dto.DurationMinutes,
            TotalMarks = dto.TotalMarks,
            PassingMarks = dto.PassingMarks,
            IsPublished = dto.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return MapToDto(quiz);
    }

    public async Task<List<QuizResponseDto>> GetQuizzesByCourseAsync(Guid courseId)
    {
        return await _context.Quizzes
            .Where(q => q.CourseId == courseId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuizResponseDto
            {
                Id = q.Id,
                CourseId = q.CourseId,
                Title = q.Title,
                DurationMinutes = q.DurationMinutes,
                TotalMarks = q.TotalMarks,
                PassingMarks = q.PassingMarks,
                IsPublished = q.IsPublished,
                CreatedAt = q.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<QuizResponseDto?> GetQuizByIdAsync(Guid quizId)
    {
        var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId);
        return quiz == null ? null : MapToDto(quiz);
    }

    public async Task<QuizResponseDto?> UpdateQuizAsync(Guid quizId, UpdateQuizDto dto)
    {
        var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz == null) return null;

        if (dto.PassingMarks > dto.TotalMarks)
            throw new Exception("Passing marks cannot be greater than total marks.");

        quiz.Title = dto.Title;
        quiz.DurationMinutes = dto.DurationMinutes;
        quiz.TotalMarks = dto.TotalMarks;
        quiz.PassingMarks = dto.PassingMarks;
        quiz.IsPublished = dto.IsPublished;

        await _context.SaveChangesAsync();

        return MapToDto(quiz);
    }

    public async Task<bool> DeleteQuizAsync(Guid quizId)
    {
        var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz == null) return false;

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();

        return true;
    }

    private static QuizResponseDto MapToDto(Quiz quiz)
    {
        return new QuizResponseDto
        {
            Id = quiz.Id,
            CourseId = quiz.CourseId,
            Title = quiz.Title,
            DurationMinutes = quiz.DurationMinutes,
            TotalMarks = quiz.TotalMarks,
            PassingMarks = quiz.PassingMarks,
            IsPublished = quiz.IsPublished,
            CreatedAt = quiz.CreatedAt
        };
    }
}