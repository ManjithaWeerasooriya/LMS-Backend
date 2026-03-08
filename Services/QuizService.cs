using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Quizzes;
using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class QuizService
{
    private readonly ApplicationDBContext _dbContext;

    public QuizService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Quiz?> CreateQuizAsync(
        string teacherId,
        CreateQuizRequestDto dto,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(
                c => c.Id == dto.CourseId && c.TeacherId == teacherId,
                cancellationToken);

        if (course == null)
        {
            return null;
        }

        var quiz = new Quiz
        {
            CourseId = course.Id,
            Title = dto.Title.Trim(),
            DurationMinutes = dto.DurationMinutes,
            TotalMarks = dto.TotalMarks,
            PassingMarks = dto.PassingMarks,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Quizzes.Add(quiz);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return quiz;
    }

    public async Task<List<QuizListItemDto>> GetQuizzesForTeacherAsync(
        string teacherId,
        CancellationToken cancellationToken)
    {
        var quizzes = await _dbContext.Quizzes
            .Include(q => q.Course)
            .Include(q => q.Attempts)
            .Where(q => q.Course.TeacherId == teacherId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuizListItemDto
            {
                Id = q.Id,
                Title = q.Title,
                CourseTitle = q.Course.Title,
                QuestionCount = 0, // Question entity not modeled yet
                DurationMinutes = q.DurationMinutes,
                Attempts = q.Attempts.Count,
                AverageScorePercent = q.Attempts.Any()
    ? (q.Attempts.Average(a => (double)a.Score) / q.TotalMarks) * 100.0
    : 0
            })
            .ToListAsync(cancellationToken);

        return quizzes;
    }
}

