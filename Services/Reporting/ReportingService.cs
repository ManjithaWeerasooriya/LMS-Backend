using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Reports;
using LMS_Backend.Models.DTOs.Teacher;
using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services.Reporting;

public class ReportingService : IReportingService
{
    private readonly ApplicationDBContext _dbContext;

    public ReportingService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EnrollmentStatisticsDto> GetEnrollmentStatisticsAsync(string? teacherId, CancellationToken cancellationToken)
    {
        IQueryable<CourseEnrollment> enrollmentsQuery = _dbContext.CourseEnrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .ThenInclude(c => c.Enrollments);

        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            enrollmentsQuery = enrollmentsQuery.Where(e => e.Course.TeacherId == teacherId);
        }

        var totalEnrollments = await enrollmentsQuery.CountAsync(cancellationToken);

        var totalStudents = await enrollmentsQuery
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);

        var perCourse = await enrollmentsQuery
            .GroupBy(e => new { e.CourseId, e.Course.Title, e.Course.Status })
            .Select(g => new CourseEnrollmentStatDto
            {
                CourseId = g.Key.CourseId,
                CourseTitle = g.Key.Title,
                StudentCount = g.Count(),
                AverageProgressPercent = g.Average(e => e.ProgressPercent),
                Status = g.Key.Status.ToString()
            })
            .OrderByDescending(g => g.StudentCount)
            .ToListAsync(cancellationToken);

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
        var growth = await enrollmentsQuery
            .Where(e => e.EnrolledAt >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1))
            .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
            .Select(g => new EnrollmentGrowthPointDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Enrollments = g.Count()
            })
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Month)
            .ToListAsync(cancellationToken);

        return new EnrollmentStatisticsDto
        {
            TotalEnrollments = totalEnrollments,
            TotalStudents = totalStudents,
            EnrollmentByCourse = perCourse,
            MonthlyGrowth = growth
        };
    }

    public async Task<IReadOnlyList<CourseCompletionRateItemDto>> GetCourseCompletionRatesAsync(string? teacherId, CancellationToken cancellationToken)
    {
        IQueryable<Course> coursesQuery = _dbContext.Courses
            .AsNoTracking()
            .Include(c => c.Enrollments);

        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            coursesQuery = coursesQuery.Where(c => c.TeacherId == teacherId);
        }

        var completionRates = await coursesQuery
            .Select(c => new CourseCompletionRateItemDto
            {
                CourseId = c.Id,
                CourseTitle = c.Title,
                CompletionRate = c.Enrollments.Any()
                    ? (double)c.Enrollments.Count(e => e.CompletedAt != null) / c.Enrollments.Count * 100.0
                    : 0
            })
            .OrderByDescending(c => c.CompletionRate)
            .ToListAsync(cancellationToken);

        return completionRates;
    }

    public async Task<QuizStatisticsDto> GetQuizStatisticsAsync(string? teacherId, CancellationToken cancellationToken)
    {
        IQueryable<QuizAttempt> attemptsQuery = _dbContext.QuizAttempts
            .AsNoTracking()
            .Include(a => a.Quiz)
            .ThenInclude(q => q.Course);

        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            attemptsQuery = attemptsQuery.Where(a => a.Quiz.Course.TeacherId == teacherId);
        }

        var perQuiz = await attemptsQuery
            .GroupBy(a => new { a.QuizId, a.Quiz.Title, CourseTitle = a.Quiz.Course.Title, a.Quiz.TotalMarks })
            .Select(g => new QuizAverageScoreDto
            {
                QuizId = g.Key.QuizId,
                QuizTitle = g.Key.Title,
                CourseTitle = g.Key.CourseTitle,
                Attempts = g.Count(),
                AverageScorePercent = g.Key.TotalMarks > 0
                    ? g.Average(a => (double)a.Score / g.Key.TotalMarks * 100.0)
                    : 0
            })
            .OrderByDescending(q => q.Attempts)
            .ToListAsync(cancellationToken);

        var attemptsData = await attemptsQuery
            .Select(a => new AttemptScore(a.Score, a.Quiz.TotalMarks))
            .ToListAsync(cancellationToken);

        var totalAttempts = attemptsData.Count;
        var averageScorePercent = totalAttempts == 0
            ? 0
            : attemptsData
                .Where(a => a.TotalMarks > 0)
                .Select(a => (double)a.Score / a.TotalMarks * 100.0)
                .DefaultIfEmpty(0)
                .Average();

        var performance = CalculatePerformanceBands(attemptsData);

        return new QuizStatisticsDto
        {
            TotalAttempts = totalAttempts,
            AverageScorePercent = Math.Round(averageScorePercent, 1),
            AverageScorePerQuiz = perQuiz,
            PerformanceBands = performance
        };
    }

    public async Task<AttendanceStatisticsDto> GetAttendanceStatisticsAsync(string? teacherId, CancellationToken cancellationToken)
    {
        IQueryable<LiveClass> liveClassesQuery = _dbContext.LiveClasses
            .AsNoTracking()
            .Include(l => l.Course)
            .ThenInclude(c => c.Enrollments);

        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            liveClassesQuery = liveClassesQuery.Where(l => l.TeacherId == teacherId);
        }

        var nowUtc = DateTime.UtcNow;
        var upcomingQuery = liveClassesQuery.Where(l => l.ScheduledAt >= nowUtc);
        var completedQuery = liveClassesQuery.Where(l => l.ScheduledAt < nowUtc && l.ScheduledAt >= nowUtc.AddDays(-30));

        var upcomingCount = await upcomingQuery.CountAsync(cancellationToken);
        var completedCount = await completedQuery.CountAsync(cancellationToken);

        var upcomingSessions = await upcomingQuery
            .OrderBy(l => l.ScheduledAt)
            .Take(5)
            .Select(l => new LiveSessionSummaryDto
            {
                LiveClassId = l.Id,
                Topic = l.Topic,
                ScheduledAt = l.ScheduledAt,
                CourseTitle = l.Course != null ? l.Course.Title : null,
                StudentsEnrolled = l.Course != null ? l.Course.Enrollments.Count : 0,
                MeetingLink = l.MeetingLink
            })
            .ToListAsync(cancellationToken);

        return new AttendanceStatisticsDto
        {
            UpcomingSessions = upcomingCount,
            CompletedSessionsLast30Days = completedCount,
            AttendanceRate = null,
            AttendanceTrackingAvailable = false,
            AttendanceTrackingNote = "Attendance tracking is not implemented; add an attendance entity to calculate attendance rates.",
            UpcomingSessionDetails = upcomingSessions
        };
    }

    private static PerformanceBandDto CalculatePerformanceBands(IEnumerable<AttemptScore> attempts)
    {
        var attemptList = attempts
            .Where(a => a.TotalMarks > 0)
            .Select(a => (double)a.Score / a.TotalMarks * 100.0)
            .ToList();

        if (attemptList.Count == 0)
        {
            return new PerformanceBandDto();
        }

        var total = attemptList.Count;
        var excellent = attemptList.Count(p => p >= 80);
        var good = attemptList.Count(p => p >= 60 && p < 80);
        var average = attemptList.Count(p => p >= 40 && p < 60);
        var needsImprovement = attemptList.Count(p => p < 40);

        return new PerformanceBandDto
        {
            ExcellentPercentage = Percent(excellent, total),
            GoodPercentage = Percent(good, total),
            AveragePercentage = Percent(average, total),
            NeedsImprovementPercentage = Percent(needsImprovement, total)
        };
    }

    private static double Percent(int count, int total)
    {
        return total == 0 ? 0 : Math.Round((double)count / total * 100.0, 1);
    }

    private readonly struct AttemptScore
    {
        public AttemptScore(int score, int totalMarks)
        {
            Score = score;
            TotalMarks = totalMarks;
        }

        public int Score { get; }
        public int TotalMarks { get; }
    }
}
