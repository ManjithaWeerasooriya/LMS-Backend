using System.ComponentModel.DataAnnotations;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.DTOs.Student;
using LMS_Backend.Models.DTOs.Teacher;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class QuizService : IQuizService
{
    private readonly ApplicationDBContext _context;

    public QuizService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<QuizResponseDto>> GetTeacherQuizzesByCourseAsync(
        string teacherId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsCourseAsync(teacherId, courseId, cancellationToken);

        return await _context.Quizzes
            .AsNoTracking()
            .Where(q => q.CourseId == courseId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuizResponseDto
            {
                Id = q.Id,
                CourseId = q.CourseId,
                Title = q.Title,
                Description = q.Description,
                DurationMinutes = q.DurationMinutes,
                StartTimeUtc = q.StartTimeUtc,
                EndTimeUtc = q.EndTimeUtc,
                TotalMarks = q.TotalMarks,
                RandomizeQuestions = q.RandomizeQuestions,
                AllowMultipleAttempts = q.AllowMultipleAttempts,
                IsPublished = q.IsPublished,
                AreResultsPublished = q.AreResultsPublished,
                QuestionCount = q.Questions.Count,
                AttemptCount = q.Attempts.Count,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<QuizResponseDto> GetTeacherQuizByIdAsync(
        string teacherId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var quiz = await GetTeacherManagedQuizQuery(teacherId)
            .AsNoTracking()
            .Include(q => q.Questions)
            .Include(q => q.Attempts)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            await ThrowTeacherQuizAccessExceptionAsync(teacherId, quizId, cancellationToken);
        }

        return ToQuizResponseDto(quiz!);
    }

    public async Task<QuizResponseDto> CreateQuizAsync(
        string teacherId,
        CreateQuizDto dto,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsCourseAsync(teacherId, dto.CourseId, cancellationToken);

        var quiz = new Quiz
        {
            CourseId = dto.CourseId,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            DurationMinutes = dto.DurationMinutes,
            StartTimeUtc = EnsureUtc(dto.StartTimeUtc),
            EndTimeUtc = EnsureUtc(dto.EndTimeUtc),
            TotalMarks = dto.TotalMarks,
            RandomizeQuestions = dto.RandomizeQuestions,
            AllowMultipleAttempts = dto.AllowMultipleAttempts,
            IsPublished = dto.IsPublished,
            AreResultsPublished = dto.AreResultsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetTeacherQuizByIdAsync(teacherId, quiz.Id, cancellationToken);
    }

    public async Task<QuizResponseDto> UpdateQuizAsync(
        string teacherId,
        Guid quizId,
        UpdateQuizDto dto,
        CancellationToken cancellationToken)
    {
        var quiz = await GetTeacherManagedQuizQuery(teacherId)
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            await ThrowTeacherQuizAccessExceptionAsync(teacherId, quizId, cancellationToken);
        }

        var activeQuestionMarks = quiz!.Questions.Sum(q => q.Marks);
        if (dto.TotalMarks < activeQuestionMarks)
        {
            throw new InvalidOperationException("Quiz total marks cannot be less than the sum of existing question marks.");
        }

        quiz.Title = dto.Title.Trim();
        quiz.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        quiz.DurationMinutes = dto.DurationMinutes;
        quiz.StartTimeUtc = EnsureUtc(dto.StartTimeUtc);
        quiz.EndTimeUtc = EnsureUtc(dto.EndTimeUtc);
        quiz.TotalMarks = dto.TotalMarks;
        quiz.RandomizeQuestions = dto.RandomizeQuestions;
        quiz.AllowMultipleAttempts = dto.AllowMultipleAttempts;
        quiz.IsPublished = dto.IsPublished;
        quiz.AreResultsPublished = dto.AreResultsPublished;
        quiz.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetTeacherQuizByIdAsync(teacherId, quizId, cancellationToken);
    }

    public async Task DeleteQuizAsync(
        string teacherId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var quiz = await GetTeacherManagedQuizQuery(teacherId)
            .Include(q => q.Questions)
            .ThenInclude(question => question.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            await ThrowTeacherQuizAccessExceptionAsync(teacherId, quizId, cancellationToken);
        }

        quiz!.IsDeleted = true;
        quiz.DeletedAt = DateTime.UtcNow;
        quiz.UpdatedAt = DateTime.UtcNow;

        foreach (var question in quiz.Questions)
        {
            question.IsDeleted = true;
            question.DeletedAt = DateTime.UtcNow;
            question.UpdatedAt = DateTime.UtcNow;

            foreach (var option in question.Options)
            {
                option.IsDeleted = true;
                option.DeletedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuizResponseDto> SetResultsPublicationAsync(
        string teacherId,
        Guid quizId,
        bool publishResults,
        CancellationToken cancellationToken)
    {
        var quiz = await GetTeacherManagedQuizQuery(teacherId)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            await ThrowTeacherQuizAccessExceptionAsync(teacherId, quizId, cancellationToken);
        }

        quiz!.AreResultsPublished = publishResults;
        quiz.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetTeacherQuizByIdAsync(teacherId, quizId, cancellationToken);
    }

    public async Task<TeacherQuizAnalyticsDto> GetTeacherQuizAnalyticsAsync(
        string teacherId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var analyticsData = await GetTeacherManagedQuizQuery(teacherId)
            .AsNoTracking()
            .Where(q => q.Id == quizId)
            .Select(q => new
            {
                q.Id,
                QuizTitle = q.Title,
                q.CourseId,
                CourseTitle = q.Course.Title,
                q.TotalMarks,
                TotalEnrolledStudents = q.Course.Enrollments.Count(),
                BestAttempts = q.Attempts
                    .Where(a =>
                        a.Status == QuizAttemptStatus.Submitted ||
                        a.Status == QuizAttemptStatus.PendingReview ||
                        a.Status == QuizAttemptStatus.Graded)
                    .GroupBy(a => a.StudentId)
                    .Select(g => g
                        .OrderByDescending(a => a.Score)
                        .ThenByDescending(a => a.SubmittedAt)
                        .ThenByDescending(a => a.AttemptNumber)
                        .Select(a => new
                        {
                            a.StudentId,
                            a.Score
                        })
                        .First())
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (analyticsData == null)
        {
            await ThrowTeacherQuizAccessExceptionAsync(teacherId, quizId, cancellationToken);
        }

        var bestAttempts = analyticsData!.BestAttempts.ToList();
        var studentsParticipated = bestAttempts.Count;
        var totalEnrolledStudents = analyticsData.TotalEnrolledStudents;
        var passMark = analyticsData.TotalMarks * 0.5m;

        var averageScore = studentsParticipated == 0
            ? 0m
            : Math.Round(bestAttempts.Average(a => a.Score), 2);

        var highestScore = studentsParticipated == 0
            ? 0m
            : bestAttempts.Max(a => a.Score);

        var lowestScore = studentsParticipated == 0
            ? 0m
            : bestAttempts.Min(a => a.Score);

        var passCount = studentsParticipated == 0
            ? 0
            : bestAttempts.Count(a => a.Score >= passMark);

        var failCount = studentsParticipated - passCount;

        var passPercentage = studentsParticipated == 0
            ? 0
            : Math.Round((double)passCount * 100d / studentsParticipated, 2);

        var failPercentage = studentsParticipated == 0
            ? 0
            : Math.Round((double)failCount * 100d / studentsParticipated, 2);

        var participationRate = totalEnrolledStudents == 0
            ? 0
            : Math.Round((double)studentsParticipated * 100d / totalEnrolledStudents, 2);

        return new TeacherQuizAnalyticsDto
        {
            QuizId = analyticsData.Id,
            QuizTitle = analyticsData.QuizTitle,
            CourseId = analyticsData.CourseId,
            CourseTitle = analyticsData.CourseTitle,
            TotalMarks = analyticsData.TotalMarks,
            AverageScore = averageScore,
            HighestScore = highestScore,
            LowestScore = lowestScore,
            PassPercentage = passPercentage,
            FailPercentage = failPercentage,
            ParticipationRate = participationRate,
            TotalEnrolledStudents = totalEnrolledStudents,
            StudentsParticipated = studentsParticipated
        };
    }

    public async Task<IReadOnlyList<QuestionResponseDto>> GetQuestionsAsync(
        string teacherId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsQuizAsync(teacherId, quizId, cancellationToken);

        var questions = await _context.Questions
            .AsNoTracking()
            .Include(q => q.Options)
            .Where(q => q.QuizId == quizId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(cancellationToken);

        return questions.Select(ToQuestionResponseDto).ToList();
    }

    public async Task<QuestionResponseDto> GetQuestionByIdAsync(
        string teacherId,
        Guid quizId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsQuizAsync(teacherId, quizId, cancellationToken);

        var question = await _context.Questions
            .AsNoTracking()
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.QuizId == quizId && q.Id == questionId, cancellationToken);

        if (question == null)
        {
            throw new NotFoundException("Question not found.");
        }

        return ToQuestionResponseDto(question);
    }

    public async Task<QuestionResponseDto> CreateQuestionAsync(
        string teacherId,
        Guid quizId,
        CreateQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var quiz = await GetTeacherManagedQuizQuery(teacherId)
            .Include(q => q.Attempts)
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            await ThrowTeacherQuizAccessExceptionAsync(teacherId, quizId, cancellationToken);
            throw new InvalidOperationException("Quiz lookup failed unexpectedly.");
        }

        EnsureQuizStructureCanChange(quiz);
        var normalizedOptions = NormalizeQuestionOptions(dto.Options);
        ValidateQuestionRequest(dto, normalizedOptions);
        EnsureQuestionOrderIsUnique(quiz.Questions, dto.OrderIndex, null);
        EnsureMarksBudget(quiz.TotalMarks, quiz.Questions.Sum(q => q.Marks) + dto.Marks);

        var question = new Question
        {
            QuizId = quizId,
            Text = dto.Text.Trim(),
            Type = dto.Type,
            Marks = dto.Marks,
            OrderIndex = dto.OrderIndex,
            CreatedAt = DateTime.UtcNow,
            Options = BuildQuestionOptions(normalizedOptions)
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetQuestionByIdAsync(teacherId, quizId, question.Id, cancellationToken);
    }

    public async Task<QuestionResponseDto> UpdateQuestionAsync(
        string teacherId,
        Guid quizId,
        Guid questionId,
        UpdateQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var quiz = await GetTeacherManagedQuizQuery(teacherId)
            .Include(q => q.Attempts)
            .Include(q => q.Questions)
            .ThenInclude(question => question.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            await ThrowTeacherQuizAccessExceptionAsync(teacherId, quizId, cancellationToken);
            throw new InvalidOperationException("Quiz lookup failed unexpectedly.");
        }

        EnsureQuizStructureCanChange(quiz);

        var question = quiz.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
        {
            throw new NotFoundException("Question not found.");
        }

        var normalizedOptions = NormalizeQuestionOptions(dto.Options);
        ValidateQuestionRequest(dto, normalizedOptions);
        EnsureQuestionOrderIsUnique(quiz.Questions, dto.OrderIndex, questionId);

        var remainingMarks = quiz.Questions
            .Where(q => q.Id != questionId)
            .Sum(q => q.Marks);

        EnsureMarksBudget(quiz.TotalMarks, remainingMarks + dto.Marks);

        question.Text = dto.Text.Trim();
        question.Type = dto.Type;
        question.Marks = dto.Marks;
        question.OrderIndex = dto.OrderIndex;
        question.UpdatedAt = DateTime.UtcNow;

        foreach (var option in question.Options)
        {
            option.IsDeleted = true;
            option.DeletedAt = DateTime.UtcNow;
        }

        question.Options = BuildQuestionOptions(normalizedOptions, question.Id);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetQuestionByIdAsync(teacherId, quizId, questionId, cancellationToken);
    }

    public async Task DeleteQuestionAsync(
        string teacherId,
        Guid quizId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var quiz = await GetTeacherManagedQuizQuery(teacherId)
            .Include(q => q.Attempts)
            .Include(q => q.Questions)
            .ThenInclude(question => question.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            await ThrowTeacherQuizAccessExceptionAsync(teacherId, quizId, cancellationToken);
            throw new InvalidOperationException("Quiz lookup failed unexpectedly.");
        }

        EnsureQuizStructureCanChange(quiz);

        var question = quiz.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
        {
            throw new NotFoundException("Question not found.");
        }

        question.IsDeleted = true;
        question.DeletedAt = DateTime.UtcNow;
        question.UpdatedAt = DateTime.UtcNow;

        foreach (var option in question.Options)
        {
            option.IsDeleted = true;
            option.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuizAttemptListItemDto>> GetQuizAttemptsAsync(
        string teacherId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsQuizAsync(teacherId, quizId, cancellationToken);

        return await _context.QuizAttempts
            .AsNoTracking()
            .Include(a => a.Student)
            .Where(a => a.QuizId == quizId)
            .OrderByDescending(a => a.AttemptNumber)
            .ThenByDescending(a => a.StartedAt)
            .Select(a => new QuizAttemptListItemDto
            {
                AttemptId = a.Id,
                QuizId = a.QuizId,
                StudentId = a.StudentId,
                StudentName = BuildFullName(a.Student.FirstName, a.Student.LastName, a.Student.UserName),
                AttemptNumber = a.AttemptNumber,
                Status = a.Status,
                StartedAt = a.StartedAt,
                DeadlineUtc = a.DeadlineUtc,
                SubmittedAt = a.SubmittedAt,
                Score = a.Score
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<QuizAttemptDetailDto> GetQuizAttemptByIdAsync(
        string teacherId,
        Guid quizId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsQuizAsync(teacherId, quizId, cancellationToken);

        var attempt = await LoadAttemptAsync(attemptId, cancellationToken);
        if (attempt == null || attempt.QuizId != quizId)
        {
            throw new NotFoundException("Quiz attempt not found.");
        }

        return ToTeacherAttemptDetailDto(attempt);
    }

    public async Task<QuizAttemptDetailDto> GradeAnswerAsync(
        string teacherId,
        Guid quizId,
        Guid attemptId,
        Guid answerId,
        ManualGradeAnswerDto dto,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsQuizAsync(teacherId, quizId, cancellationToken);

        var attempt = await LoadAttemptAsync(attemptId, cancellationToken);
        if (attempt == null || attempt.QuizId != quizId)
        {
            throw new NotFoundException("Quiz attempt not found.");
        }

        var answer = attempt.Answers.FirstOrDefault(a => a.Id == answerId);
        if (answer == null)
        {
            throw new NotFoundException("Answer not found.");
        }

        if (QuestionValidation.IsObjective(answer.Question.Type))
        {
            throw new InvalidOperationException("Objective answers are auto-graded and cannot be manually graded.");
        }

        if (dto.AwardedMarks > answer.Question.Marks)
        {
            throw new InvalidOperationException("Awarded marks cannot be greater than the question marks.");
        }

        answer.AwardedMarks = dto.AwardedMarks;
        answer.TeacherFeedback = string.IsNullOrWhiteSpace(dto.TeacherFeedback) ? null : dto.TeacherFeedback.Trim();
        answer.ReviewStatus = StudentAnswerReviewStatus.Reviewed;
        answer.ReviewedAt = DateTime.UtcNow;

        RecalculateAttemptOutcome(attempt, DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        return ToTeacherAttemptDetailDto(attempt);
    }

    public async Task<IReadOnlyList<StudentQuizListItemDto>> GetStudentQuizzesAsync(
        string studentId,
        CancellationToken cancellationToken)
    {
        return await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Course)
            .ThenInclude(c => c.Enrollments)
            .Include(q => q.Attempts)
            .Where(q =>
                q.IsPublished &&
                q.Course.Status == CourseStatus.Active &&
                q.Course.Enrollments.Any(e => e.StudentId == studentId))
            .OrderBy(q => q.StartTimeUtc)
            .Select(q => new StudentQuizListItemDto
            {
                QuizId = q.Id,
                CourseId = q.CourseId,
                CourseTitle = q.Course.Title,
                Title = q.Title,
                Description = q.Description,
                DurationMinutes = q.DurationMinutes,
                StartTimeUtc = q.StartTimeUtc,
                EndTimeUtc = q.EndTimeUtc,
                TotalMarks = q.TotalMarks,
                AllowMultipleAttempts = q.AllowMultipleAttempts,
                ResultsPublished = q.AreResultsPublished,
                AttemptCount = q.Attempts.Count(a => a.StudentId == studentId)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentCourseQuizResultsDto>> GetStudentQuizScoresByCourseAsync(
        string studentId,
        CancellationToken cancellationToken)
    {
        var enrolledCourses = await _context.CourseEnrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Course.Status == CourseStatus.Active)
            .Select(e => new
            {
                e.CourseId,
                CourseTitle = e.Course.Title
            })
            .ToListAsync(cancellationToken);

        var courseIds = enrolledCourses.Select(c => c.CourseId).ToList();

        var publishedQuizzes = await _context.Quizzes
            .AsNoTracking()
            .Where(q =>
                courseIds.Contains(q.CourseId) &&
                q.IsPublished &&
                !q.IsDeleted)
            .Select(q => new
            {
                q.Id,
                q.CourseId,
                q.Title,
                q.TotalMarks,
                q.AreResultsPublished
            })
            .ToListAsync(cancellationToken);

        var bestAttempts = await _context.QuizAttempts
            .AsNoTracking()
            .Where(a =>
                a.StudentId == studentId &&
                (a.Status == QuizAttemptStatus.Submitted ||
                 a.Status == QuizAttemptStatus.PendingReview ||
                 a.Status == QuizAttemptStatus.Graded))
            .GroupBy(a => a.QuizId)
            .Select(g => g
                .OrderByDescending(a => a.Score)
                .ThenByDescending(a => a.SubmittedAt)
                .ThenByDescending(a => a.AttemptNumber)
                .First())
            .Select(a => new
            {
                a.QuizId,
                a.Score,
                a.SubmittedAt,
                a.AttemptNumber,
                a.Status
            })
            .ToListAsync(cancellationToken);

        var attemptsByQuizId = bestAttempts.ToDictionary(a => a.QuizId, a => a);

        var result = enrolledCourses
            .Select(course =>
            {
                var courseQuizzes = publishedQuizzes
                    .Where(q => q.CourseId == course.CourseId)
                    .ToList();

                var visibleQuizResults = courseQuizzes
                    .Where(q => q.AreResultsPublished && attemptsByQuizId.ContainsKey(q.Id))
                    .Select(q =>
                    {
                        var attempt = attemptsByQuizId[q.Id];

                        return new StudentQuizScoreItemDto
                        {
                            QuizId = q.Id,
                            QuizTitle = q.Title,
                            Score = attempt.Score,
                            TotalMarks = q.TotalMarks,
                            SubmittedAt = attempt.SubmittedAt,
                            AttemptNumber = attempt.AttemptNumber,
                            Status = attempt.Status.ToString()
                        };
                    })
                    .OrderByDescending(x => x.SubmittedAt)
                    .ToList();

                var attemptedQuizIds = courseQuizzes
                    .Where(q => attemptsByQuizId.ContainsKey(q.Id))
                    .Select(q => q.Id)
                    .Distinct()
                    .Count();

                var average = visibleQuizResults.Count == 0
                    ? 0
                    : Math.Round(visibleQuizResults.Average(x => x.Score), 2);

                var totalQuizzes = courseQuizzes.Count;
                var progress = totalQuizzes == 0
                    ? 0
                    : Math.Round((double)attemptedQuizIds * 100d / totalQuizzes, 2);

                return new StudentCourseQuizResultsDto
                {
                    CourseId = course.CourseId,
                    CourseTitle = course.CourseTitle,
                    AttemptedQuizzes = attemptedQuizIds,
                    TotalQuizzes = totalQuizzes,
                    ProgressPercentage = progress,
                    CourseAverageScore = average,
                    Quizzes = visibleQuizResults
                };
            })
            .OrderBy(x => x.CourseTitle)
            .ToList();

        return result;
    }

    public async Task<StudentAverageScoreDto> GetStudentAverageScoreAsync(
        string studentId,
        CancellationToken cancellationToken)
    {
        var bestVisibleAttempts = await _context.QuizAttempts
            .AsNoTracking()
            .Where(a =>
                a.StudentId == studentId &&
                a.Quiz.AreResultsPublished &&
                a.Quiz.IsPublished &&
                !a.Quiz.IsDeleted &&
                a.Quiz.Course.Status == CourseStatus.Active &&
                (a.Status == QuizAttemptStatus.Submitted ||
                 a.Status == QuizAttemptStatus.PendingReview ||
                 a.Status == QuizAttemptStatus.Graded))
            .GroupBy(a => a.QuizId)
            .Select(g => g
                .OrderByDescending(a => a.Score)
                .ThenByDescending(a => a.SubmittedAt)
                .ThenByDescending(a => a.AttemptNumber)
                .Select(a => a.Score)
                .First())
            .ToListAsync(cancellationToken);

        var average = bestVisibleAttempts.Count == 0
            ? 0
            : Math.Round(bestVisibleAttempts.Average(), 2);

        return new StudentAverageScoreDto
        {
            AverageScore = average
        };
    }

    public async Task<StudentCompletionDto> GetStudentCompletionAsync(
        string studentId,
        CancellationToken cancellationToken)
    {
        var enrolledCourseIds = await _context.CourseEnrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Course.Status == CourseStatus.Active)
            .Select(e => e.CourseId)
            .ToListAsync(cancellationToken);

        var totalQuizzes = await _context.Quizzes
            .AsNoTracking()
            .Where(q =>
                enrolledCourseIds.Contains(q.CourseId) &&
                q.IsPublished &&
                !q.IsDeleted)
            .CountAsync(cancellationToken);

        var attemptedQuizIds = await _context.QuizAttempts
            .AsNoTracking()
            .Where(a =>
                a.StudentId == studentId &&
                enrolledCourseIds.Contains(a.Quiz.CourseId) &&
                a.Quiz.IsPublished &&
                !a.Quiz.IsDeleted &&
                (a.Status == QuizAttemptStatus.Submitted ||
                 a.Status == QuizAttemptStatus.PendingReview ||
                 a.Status == QuizAttemptStatus.Graded))
            .Select(a => a.QuizId)
            .Distinct()
            .CountAsync(cancellationToken);

        var completion = totalQuizzes == 0
            ? 0
            : Math.Round((double)attemptedQuizIds * 100d / totalQuizzes, 2);

        return new StudentCompletionDto
        {
            AttemptedQuizzes = attemptedQuizIds,
            TotalQuizzes = totalQuizzes,
            CompletionPercentage = completion
        };
    }

    public async Task<StudentQuizDetailDto> GetStudentQuizByIdAsync(
        string studentId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var quiz = await GetStudentVisibleQuizQuery(studentId)
            .AsNoTracking()
            .Include(q => q.Course)
            .Include(q => q.Questions)
            .ThenInclude(question => question.Options)
            .Include(q => q.Attempts)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            await ThrowStudentQuizAccessExceptionAsync(studentId, quizId, cancellationToken);
        }

        return ToStudentQuizDetailDto(quiz!, studentId, randomizeQuestions: quiz!.RandomizeQuestions);
    }

    public async Task<StartQuizAttemptResponseDto> StartQuizAttemptAsync(
        string studentId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var quiz = await GetStudentVisibleQuizQuery(studentId)
            .Include(q => q.Course)
            .ThenInclude(c => c.Enrollments)
            .Include(q => q.Questions)
            .ThenInclude(question => question.Options)
            .Include(q => q.Attempts)
            .ThenInclude(attempt => attempt.Answers)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            await ThrowStudentQuizAccessExceptionAsync(studentId, quizId, cancellationToken);
            throw new InvalidOperationException("Quiz lookup failed unexpectedly.");
        }

        var nowUtc = DateTime.UtcNow;
        EnsureQuizCanBeStarted(quiz, nowUtc, studentId);

        var studentAttempts = quiz.Attempts
            .Where(a => string.Equals(a.StudentId, studentId, StringComparison.Ordinal))
            .ToList();

        var attemptStatesNormalized = NormalizeAttemptStatesForStart(studentAttempts, nowUtc);
        if (attemptStatesNormalized)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var existingInProgressAttempt = studentAttempts
            .FirstOrDefault(a => IsActiveUnfinishedAttempt(a, nowUtc));

        if (existingInProgressAttempt != null)
        {
            throw new ConflictException("You already have an in-progress attempt for this quiz.");
        }

        var nextAttemptNumber = quiz.Attempts
            .Where(a => a.StudentId == studentId)
            .Select(a => a.AttemptNumber)
            .DefaultIfEmpty(0)
            .Max() + 1;

        if (!quiz.AllowMultipleAttempts && studentAttempts.Any(IsCompletedAttempt))
        {
            throw new ConflictException("Multiple attempts are not allowed for this quiz.");
        }

        var deadlineUtc = CalculateAttemptDeadlineUtc(nowUtc, quiz);

        var attempt = new QuizAttempt
        {
            QuizId = quiz.Id,
            StudentId = studentId,
            AttemptNumber = nextAttemptNumber,
            Status = QuizAttemptStatus.InProgress,
            StartedAt = nowUtc,
            DeadlineUtc = deadlineUtc
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        return new StartQuizAttemptResponseDto
        {
            AttemptId = attempt.Id,
            QuizId = quiz.Id,
            AttemptNumber = attempt.AttemptNumber,
            StartedAt = attempt.StartedAt,
            DeadlineUtc = attempt.DeadlineUtc,
            Quiz = ToStudentQuizDetailDto(quiz, studentId, randomizeQuestions: quiz.RandomizeQuestions)
        };
    }

    public async Task<QuizAttemptDetailDto> GetStudentAttemptByIdAsync(
        string studentId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await LoadAttemptAsync(attemptId, cancellationToken);
        if (attempt == null || !string.Equals(attempt.StudentId, studentId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Quiz attempt not found.");
        }

        return ToStudentAttemptDetailDto(attempt);
    }

    public async Task<QuizAttemptDetailDto> SubmitQuizAttemptAsync(
        string studentId,
        Guid attemptId,
        SubmitQuizAttemptDto dto,
        CancellationToken cancellationToken)
    {
        var attempt = await LoadAttemptAsync(attemptId, cancellationToken);
        if (attempt == null || !string.Equals(attempt.StudentId, studentId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Quiz attempt not found.");
        }

        if (attempt.Status != QuizAttemptStatus.InProgress || attempt.SubmittedAt.HasValue)
        {
            throw new ConflictException("Only in-progress attempts can be submitted.");
        }

        var nowUtc = DateTime.UtcNow;
        if (nowUtc > attempt.DeadlineUtc)
        {
            attempt.Status = QuizAttemptStatus.Expired;
            await _context.SaveChangesAsync(cancellationToken);
            throw new ConflictException("Quiz attempt has expired.");
        }

        ValidateSubmitAttemptRequest(dto);

        var questionMap = attempt.Quiz.Questions.ToDictionary(q => q.Id);
        var submittedAnswers = dto.Answers ?? [];
        var payloadMap = submittedAnswers.ToDictionary(a => a.QuestionId);

        foreach (var submittedQuestionId in payloadMap.Keys)
        {
            if (!questionMap.ContainsKey(submittedQuestionId))
            {
                throw new InvalidOperationException("Submission contains an answer for a question that does not belong to this quiz.");
            }
        }

        if (attempt.Answers.Count > 0)
        {
            throw new ConflictException("This quiz attempt already contains submitted answers.");
        }

        var generatedAnswers = new List<StudentAnswer>();

        foreach (var question in attempt.Quiz.Questions.OrderBy(q => q.OrderIndex))
        {
            payloadMap.TryGetValue(question.Id, out var submittedAnswer);
            var answer = CreateStudentAnswer(question, attempt.Id, submittedAnswer);
            generatedAnswers.Add(answer);
        }

        _context.StudentAnswers.AddRange(generatedAnswers);
        attempt.Answers = generatedAnswers;
        attempt.SubmittedAt = nowUtc;
        attempt.Status = QuizAttemptStatus.Submitted;

        RecalculateAttemptOutcome(attempt, nowUtc);
        await _context.SaveChangesAsync(cancellationToken);

        return ToStudentAttemptDetailDto(attempt);
    }

    private IQueryable<Quiz> GetTeacherManagedQuizQuery(string teacherId) =>
        _context.Quizzes.Where(q => q.Course.TeacherId == teacherId);

    private IQueryable<Quiz> GetStudentVisibleQuizQuery(string studentId) =>
        _context.Quizzes.Where(q =>
            q.IsPublished &&
            q.Course.Status == CourseStatus.Active &&
            q.Course.Enrollments.Any(e => e.StudentId == studentId));

    private async Task EnsureTeacherOwnsCourseAsync(
        string teacherId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Id, c.TeacherId })
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            throw new NotFoundException("Course not found.");
        }

        if (!string.Equals(course.TeacherId, teacherId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You do not have access to manage quizzes for this course.");
        }
    }

    private async Task EnsureTeacherOwnsQuizAsync(
        string teacherId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var access = await _context.Quizzes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(q => q.Id == quizId)
            .Select(q => new { q.Id, q.IsDeleted, q.Course.TeacherId })
            .FirstOrDefaultAsync(cancellationToken);

        if (access == null || access.IsDeleted)
        {
            throw new NotFoundException("Quiz not found.");
        }

        if (!string.Equals(access.TeacherId, teacherId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You do not have access to manage this quiz.");
        }
    }

    private async Task ThrowTeacherQuizAccessExceptionAsync(
        string teacherId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var access = await _context.Quizzes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(q => q.Id == quizId)
            .Select(q => new { q.Id, q.IsDeleted, q.Course.TeacherId })
            .FirstOrDefaultAsync(cancellationToken);

        if (access == null || access.IsDeleted)
        {
            throw new NotFoundException("Quiz not found.");
        }

        throw new ForbiddenException("You do not have access to manage this quiz.");
    }

    private async Task ThrowStudentQuizAccessExceptionAsync(
        string studentId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var access = await _context.Quizzes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(q => q.Id == quizId)
            .Select(q => new
            {
                q.Id,
                q.IsDeleted,
                q.IsPublished,
                q.Course.Status,
                IsEnrolled = q.Course.Enrollments.Any(e => e.StudentId == studentId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (access == null || access.IsDeleted)
        {
            throw new NotFoundException("Quiz not found.");
        }

        if (!access.IsEnrolled)
        {
            throw new ForbiddenException("You must be enrolled in the course to access this quiz.");
        }

        throw new ForbiddenException("This quiz is not available to you.");
    }

    private static void EnsureQuizCanBeStarted(Quiz quiz, DateTime nowUtc, string studentId)
    {
        if (!quiz.Course.Enrollments.Any(e => e.StudentId == studentId))
        {
            throw new ForbiddenException("You must be enrolled in the course to attempt this quiz.");
        }

        if (!quiz.IsPublished)
        {
            throw new ForbiddenException("This quiz is not published.");
        }

        if (quiz.Course.Status != CourseStatus.Active)
        {
            throw new InvalidOperationException("Quiz attempts are only allowed for active courses.");
        }

        if (nowUtc < quiz.StartTimeUtc)
        {
            throw new ConflictException("This quiz is not yet available.");
        }

        if (nowUtc > quiz.EndTimeUtc)
        {
            throw new ConflictException("This quiz is no longer available.");
        }

        if (!quiz.Questions.Any())
        {
            throw new InvalidOperationException("This quiz cannot be attempted because it has no questions.");
        }
    }

    private static DateTime CalculateAttemptDeadlineUtc(DateTime startedAtUtc, Quiz quiz)
    {
        var durationDeadline = startedAtUtc.AddMinutes(quiz.DurationMinutes);
        return durationDeadline <= quiz.EndTimeUtc ? durationDeadline : quiz.EndTimeUtc;
    }

    private static void EnsureQuizStructureCanChange(Quiz quiz)
    {
        if (quiz.Attempts.Any())
        {
            throw new ConflictException("Quiz questions cannot be modified after students have started attempting the quiz.");
        }
    }

    private static void EnsureQuestionOrderIsUnique(IEnumerable<Question> questions, int orderIndex, Guid? currentQuestionId)
    {
        var duplicate = questions.Any(q => q.Id != currentQuestionId && q.OrderIndex == orderIndex);
        if (duplicate)
        {
            throw new InvalidOperationException("Question order indexes must be unique within a quiz.");
        }
    }

    private static void EnsureMarksBudget(decimal quizTotalMarks, decimal allocatedMarks)
    {
        if (allocatedMarks > quizTotalMarks)
        {
            throw new InvalidOperationException("The sum of question marks cannot exceed the quiz total marks.");
        }
    }

    private async Task<QuizAttempt?> LoadAttemptAsync(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _context.QuizAttempts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

        if (attempt == null)
        {
            return null;
        }

        await _context.Entry(attempt)
            .Reference(a => a.Student)
            .LoadAsync(cancellationToken);

        await _context.Entry(attempt)
            .Reference(a => a.Quiz)
            .LoadAsync(cancellationToken);

        await _context.Entry(attempt.Quiz)
            .Collection(q => q.Questions)
            .Query()
            .Include(question => question.Options)
            .LoadAsync(cancellationToken);

        await _context.Entry(attempt)
            .Collection(a => a.Answers)
            .Query()
            .Include(answer => answer.Question)
            .ThenInclude(question => question.Options)
            .Include(answer => answer.SelectedOptions)
            .ThenInclude(selectedOption => selectedOption.QuestionOption)
            .LoadAsync(cancellationToken);

        return attempt;
    }

    private static StudentAnswer CreateStudentAnswer(
        Question question,
        Guid attemptId,
        SubmitStudentAnswerDto? submittedAnswer)
    {
        var answer = new StudentAnswer
        {
            QuizAttemptId = attemptId,
            QuestionId = question.Id,
            Question = question
        };

        if (QuestionValidation.IsObjective(question.Type))
        {
            var selectedOptionIds = NormalizeSelectedOptionIds(submittedAnswer?.SelectedOptionIds);
            ValidateObjectiveSubmission(question, submittedAnswer, selectedOptionIds);

            answer.SelectedOptions = question.Options
                .Where(o => selectedOptionIds.Contains(o.Id))
                .Select(o => new StudentAnswerOption
                {
                    QuestionOptionId = o.Id,
                    QuestionOption = o
                })
                .ToList();

            var correctOptionIds = question.Options
                .Where(o => o.IsCorrect)
                .Select(o => o.Id)
                .OrderBy(id => id)
                .ToList();

            var normalizedSelectedIds = selectedOptionIds
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            var isCorrect = correctOptionIds.SequenceEqual(normalizedSelectedIds);

            answer.IsCorrect = isCorrect;
            answer.AwardedMarks = isCorrect ? question.Marks : 0;
            answer.ReviewStatus = StudentAnswerReviewStatus.NotRequired;
            return answer;
        }

        ValidateSubjectiveSubmission(question, submittedAnswer);

        answer.AnswerText = string.IsNullOrWhiteSpace(submittedAnswer?.AnswerText) ? null : submittedAnswer.AnswerText.Trim();
        answer.FileReference = string.IsNullOrWhiteSpace(submittedAnswer?.FileReference) ? null : submittedAnswer.FileReference.Trim();
        answer.IsCorrect = null;
        answer.AwardedMarks = 0;

        var hasContent = !string.IsNullOrWhiteSpace(answer.AnswerText) || !string.IsNullOrWhiteSpace(answer.FileReference);
        answer.ReviewStatus = hasContent
            ? StudentAnswerReviewStatus.PendingReview
            : StudentAnswerReviewStatus.Reviewed;

        return answer;
    }

    private static void ValidateObjectiveSubmission(
        Question question,
        SubmitStudentAnswerDto? submittedAnswer,
        IReadOnlyCollection<Guid> selectedOptionIds)
    {
        if (!string.IsNullOrWhiteSpace(submittedAnswer?.AnswerText) || !string.IsNullOrWhiteSpace(submittedAnswer?.FileReference))
        {
            throw new InvalidOperationException("Objective questions can only be answered by selecting options.");
        }

        if (selectedOptionIds.Count == 0)
        {
            throw new InvalidOperationException(GetObjectiveSelectionRequiredMessage(question.Type));
        }

        var validOptionIds = question.Options.Select(o => o.Id).ToHashSet();
        if (selectedOptionIds.Any(id => !validOptionIds.Contains(id)))
        {
            throw new InvalidOperationException("Submission contains invalid question options.");
        }

        if ((question.Type == QuestionType.SingleMcq || question.Type == QuestionType.TrueFalse) &&
            selectedOptionIds.Count != 1)
        {
            throw new InvalidOperationException(GetSingleSelectionMessage(question.Type));
        }
    }

    private static void ValidateSubjectiveSubmission(Question question, SubmitStudentAnswerDto? submittedAnswer)
    {
        var hasSelectedOptions = NormalizeSelectedOptionIds(submittedAnswer?.SelectedOptionIds).Count > 0;
        if (hasSelectedOptions)
        {
            throw new InvalidOperationException("Subjective questions cannot be answered using options.");
        }

        if (question.Type == QuestionType.FileUpload &&
            submittedAnswer != null &&
            !string.IsNullOrWhiteSpace(submittedAnswer.AnswerText))
        {
            throw new InvalidOperationException("File upload questions only accept a file reference.");
        }

        if (question.Type == QuestionType.FileUpload &&
            string.IsNullOrWhiteSpace(submittedAnswer?.FileReference))
        {
            throw new InvalidOperationException("File upload questions require a file reference.");
        }

        if ((question.Type == QuestionType.ShortAnswer || question.Type == QuestionType.Essay) &&
            submittedAnswer != null &&
            !string.IsNullOrWhiteSpace(submittedAnswer.FileReference))
        {
            throw new InvalidOperationException("Text-based questions do not accept a file reference.");
        }

        if ((question.Type == QuestionType.ShortAnswer || question.Type == QuestionType.Essay) &&
            string.IsNullOrWhiteSpace(submittedAnswer?.AnswerText))
        {
            throw new InvalidOperationException(GetTextAnswerRequiredMessage(question.Type));
        }
    }

    private static bool NormalizeAttemptStatesForStart(
        IEnumerable<QuizAttempt> attempts,
        DateTime nowUtc)
    {
        var changed = false;

        foreach (var attempt in attempts)
        {
            if (attempt.Status == QuizAttemptStatus.InProgress && nowUtc > attempt.DeadlineUtc)
            {
                attempt.Status = QuizAttemptStatus.Expired;
                attempt.ReviewedAt = null;
                changed = true;
                continue;
            }

            if (attempt.SubmittedAt.HasValue &&
                (attempt.Status == QuizAttemptStatus.InProgress || attempt.Status == QuizAttemptStatus.Submitted))
            {
                var previousStatus = attempt.Status;
                var previousScore = attempt.Score;
                var previousReviewedAt = attempt.ReviewedAt;

                RecalculateAttemptOutcome(attempt, attempt.SubmittedAt.Value);

                if (attempt.Status != previousStatus ||
                    attempt.Score != previousScore ||
                    attempt.ReviewedAt != previousReviewedAt)
                {
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static bool IsActiveUnfinishedAttempt(QuizAttempt attempt, DateTime nowUtc) =>
        attempt.Status == QuizAttemptStatus.InProgress &&
        !attempt.SubmittedAt.HasValue &&
        attempt.DeadlineUtc >= nowUtc;

    private static bool IsCompletedAttempt(QuizAttempt attempt) =>
        attempt.Status == QuizAttemptStatus.Submitted ||
        attempt.Status == QuizAttemptStatus.PendingReview ||
        attempt.Status == QuizAttemptStatus.Graded ||
        attempt.SubmittedAt.HasValue;

    private static void RecalculateAttemptOutcome(QuizAttempt attempt, DateTime outcomeTimestampUtc)
    {
        attempt.Score = attempt.Answers.Sum(a => a.AwardedMarks);

        var hasPendingReview = attempt.Answers.Any(a => a.ReviewStatus == StudentAnswerReviewStatus.PendingReview);
        if (attempt.Status == QuizAttemptStatus.Expired)
        {
            return;
        }

        attempt.Status = hasPendingReview ? QuizAttemptStatus.PendingReview : QuizAttemptStatus.Graded;
        attempt.ReviewedAt = hasPendingReview ? null : outcomeTimestampUtc;
    }

    private static void ValidateSubmitAttemptRequest(SubmitQuizAttemptDto dto)
    {
        var validationResults = new List<ValidationResult>();
        ValidateObject(dto, validationResults);

        foreach (var answer in dto.Answers ?? [])
        {
            ValidateObject(answer, validationResults);
        }

        if (validationResults.Count == 0)
        {
            return;
        }

        var message = string.Join(
            " ",
            validationResults
                .Select(result => result.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct(StringComparer.Ordinal));

        throw new ArgumentException(
            string.IsNullOrWhiteSpace(message)
                ? "Quiz submission request is invalid."
                : message);
    }

    private static List<Guid> NormalizeSelectedOptionIds(List<Guid>? selectedOptionIds) =>
        (selectedOptionIds ?? []).Distinct().ToList();

    private static string GetObjectiveSelectionRequiredMessage(QuestionType type) =>
        type switch
        {
            QuestionType.SingleMcq => "Single choice questions require exactly one selected option.",
            QuestionType.MultipleMcq => "Multiple choice questions require at least one selected option.",
            QuestionType.TrueFalse => "True/false questions require exactly one selected option.",
            _ => "Objective questions require selected options."
        };

    private static string GetSingleSelectionMessage(QuestionType type) =>
        type switch
        {
            QuestionType.TrueFalse => "True/false questions require exactly one selected option.",
            _ => "Single choice questions require exactly one selected option."
        };

    private static string GetTextAnswerRequiredMessage(QuestionType type) =>
        type switch
        {
            QuestionType.ShortAnswer => "Short answer questions require answerText.",
            QuestionType.Essay => "Essay questions require answerText.",
            _ => "This question requires answerText."
        };

    private static IReadOnlyList<QuestionOptionRequestDto> NormalizeQuestionOptions(
        List<QuestionOptionRequestDto>? options) =>
        options ?? [];

    private static void ValidateQuestionRequest(
        object dto,
        IReadOnlyList<QuestionOptionRequestDto> options)
    {
        var validationResults = new List<ValidationResult>();
        ValidateObject(dto, validationResults);

        foreach (var option in options)
        {
            ValidateObject(option, validationResults);
        }

        if (validationResults.Count == 0)
        {
            return;
        }

        var message = string.Join(
            " ",
            validationResults
                .Select(result => result.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct(StringComparer.Ordinal));

        throw new ArgumentException(
            string.IsNullOrWhiteSpace(message)
                ? "Question request is invalid."
                : message);
    }

    private static void ValidateObject(
        object instance,
        ICollection<ValidationResult> validationResults) =>
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            validationResults,
            validateAllProperties: true);

    private static List<QuestionOption> BuildQuestionOptions(
        IEnumerable<QuestionOptionRequestDto> options,
        Guid? questionId = null)
    {
        var questionOptions = new List<QuestionOption>();

        foreach (var option in options.OrderBy(o => o.OrderIndex))
        {
            var questionOption = new QuestionOption
            {
                Text = option.Text.Trim(),
                IsCorrect = option.IsCorrect,
                OrderIndex = option.OrderIndex
            };

            if (questionId.HasValue)
            {
                questionOption.QuestionId = questionId.Value;
            }

            questionOptions.Add(questionOption);
        }

        return questionOptions;
    }

    private static QuizResponseDto ToQuizResponseDto(Quiz quiz) =>
        new()
        {
            Id = quiz.Id,
            CourseId = quiz.CourseId,
            Title = quiz.Title,
            Description = quiz.Description,
            DurationMinutes = quiz.DurationMinutes,
            StartTimeUtc = quiz.StartTimeUtc,
            EndTimeUtc = quiz.EndTimeUtc,
            TotalMarks = quiz.TotalMarks,
            RandomizeQuestions = quiz.RandomizeQuestions,
            AllowMultipleAttempts = quiz.AllowMultipleAttempts,
            IsPublished = quiz.IsPublished,
            AreResultsPublished = quiz.AreResultsPublished,
            QuestionCount = quiz.Questions.Count,
            AttemptCount = quiz.Attempts.Count,
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt
        };

    private static QuestionResponseDto ToQuestionResponseDto(Question question) =>
        new()
        {
            Id = question.Id,
            QuizId = question.QuizId,
            Text = question.Text,
            Type = question.Type,
            Marks = question.Marks,
            OrderIndex = question.OrderIndex,
            Options = question.Options
                .OrderBy(o => o.OrderIndex)
                .Select(o => new QuestionOptionResponseDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    OrderIndex = o.OrderIndex
                })
                .ToList()
        };

    private static StudentQuizDetailDto ToStudentQuizDetailDto(Quiz quiz, string studentId, bool randomizeQuestions)
    {
        var questions = quiz.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(q => new StudentQuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type,
                Marks = q.Marks,
                OrderIndex = q.OrderIndex,
                Options = q.Options
                    .OrderBy(o => o.OrderIndex)
                    .Select(o => new StudentQuestionOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        OrderIndex = o.OrderIndex
                    })
                    .ToList()
            })
            .ToList();

        if (randomizeQuestions)
        {
            questions = questions.OrderBy(_ => Random.Shared.Next()).ToList();
        }

        return new StudentQuizDetailDto
        {
            QuizId = quiz.Id,
            CourseId = quiz.CourseId,
            CourseTitle = quiz.Course.Title,
            Title = quiz.Title,
            Description = quiz.Description,
            DurationMinutes = quiz.DurationMinutes,
            StartTimeUtc = quiz.StartTimeUtc,
            EndTimeUtc = quiz.EndTimeUtc,
            TotalMarks = quiz.TotalMarks,
            RandomizeQuestions = quiz.RandomizeQuestions,
            AllowMultipleAttempts = quiz.AllowMultipleAttempts,
            AttemptCount = quiz.Attempts.Count(a => a.StudentId == studentId),
            Questions = questions
        };
    }

    private static QuizAttemptDetailDto ToTeacherAttemptDetailDto(QuizAttempt attempt) =>
        new()
        {
            AttemptId = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz.Title,
            StudentId = attempt.StudentId,
            StudentName = BuildFullName(attempt.Student.FirstName, attempt.Student.LastName, attempt.Student.UserName),
            AttemptNumber = attempt.AttemptNumber,
            Status = attempt.Status,
            StartedAt = attempt.StartedAt,
            DeadlineUtc = attempt.DeadlineUtc,
            SubmittedAt = attempt.SubmittedAt,
            Score = attempt.Score,
            ResultsPublished = attempt.Quiz.AreResultsPublished,
            Answers = attempt.Answers
                .OrderBy(a => a.Question.OrderIndex)
                .Select(a => ToAttemptAnswerDto(a, includeResults: true))
                .ToList()
        };

    private static QuizAttemptDetailDto ToStudentAttemptDetailDto(QuizAttempt attempt)
    {
        var includeResults = attempt.Quiz.AreResultsPublished;

        return new QuizAttemptDetailDto
        {
            AttemptId = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz.Title,
            StudentId = attempt.StudentId,
            StudentName = BuildFullName(attempt.Student.FirstName, attempt.Student.LastName, attempt.Student.UserName),
            AttemptNumber = attempt.AttemptNumber,
            Status = attempt.Status,
            StartedAt = attempt.StartedAt,
            DeadlineUtc = attempt.DeadlineUtc,
            SubmittedAt = attempt.SubmittedAt,
            Score = includeResults ? attempt.Score : null,
            ResultsPublished = includeResults,
            Answers = attempt.Answers
                .OrderBy(a => a.Question.OrderIndex)
                .Select(a => ToAttemptAnswerDto(a, includeResults))
                .ToList()
        };
    }

    private static QuizAttemptAnswerDto ToAttemptAnswerDto(StudentAnswer answer, bool includeResults) =>
        new()
        {
            AnswerId = answer.Id,
            QuestionId = answer.QuestionId,
            QuestionText = answer.Question.Text,
            QuestionType = answer.Question.Type,
            MaxMarks = answer.Question.Marks,
            SelectedOptionIds = answer.SelectedOptions.Select(o => o.QuestionOptionId).ToList(),
            SelectedOptionTexts = answer.SelectedOptions
                .OrderBy(o => o.QuestionOption.OrderIndex)
                .Select(o => o.QuestionOption.Text)
                .ToList(),
            AnswerText = answer.AnswerText,
            FileReference = answer.FileReference,
            IsCorrect = includeResults ? answer.IsCorrect : null,
            AwardedMarks = includeResults ? answer.AwardedMarks : null,
            ReviewStatus = includeResults ? answer.ReviewStatus : HidePendingStatus(answer.ReviewStatus),
            TeacherFeedback = includeResults ? answer.TeacherFeedback : null,
            Options = includeResults
                ? answer.Question.Options
                    .OrderBy(o => o.OrderIndex)
                    .Select(o => new QuestionOptionResponseDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect,
                        OrderIndex = o.OrderIndex
                    })
                    .ToList()
                : new List<QuestionOptionResponseDto>()
        };

    private static StudentAnswerReviewStatus HidePendingStatus(StudentAnswerReviewStatus reviewStatus) =>
        reviewStatus == StudentAnswerReviewStatus.PendingReview
            ? StudentAnswerReviewStatus.PendingReview
            : StudentAnswerReviewStatus.NotRequired;

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    private static string BuildFullName(string? firstName, string? lastName, string? fallback)
    {
        var fullName = string.Join(" ", new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(fullName) ? (fallback ?? string.Empty) : fullName;
    }
}
