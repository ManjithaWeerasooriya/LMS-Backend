using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Student;
using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class StudentDashboardService
{
    private readonly ApplicationDBContext _dbContext;

    public StudentDashboardService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentDashboardResponseDto> GetDashboardAsync(
        string studentId,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var enrollmentsQuery = _dbContext.CourseEnrollments
            .Include(e => e.Course)
            .ThenInclude(c => c.Teacher)
            .Where(e => e.StudentId == studentId && e.Course.Status == CourseStatus.Active);

        var enrolledCoursesCount = await enrollmentsQuery
            .Select(e => e.CourseId)
            .Distinct()
            .CountAsync(cancellationToken);

        var myCourses = (await enrollmentsQuery
            .GroupBy(e => new
            {
                e.CourseId,
                e.Course.Title,
                e.Course.Teacher.FirstName,
                e.Course.Teacher.LastName
            })
            .Select(g => new
            {
                CourseId = g.Key.CourseId,
                Title = g.Key.Title,
                g.Key.FirstName,
                g.Key.LastName,
                ProgressPercent = g.Average(e => e.ProgressPercent)
            })
            .OrderByDescending(c => c.ProgressPercent)
            .Take(4)
            .ToListAsync(cancellationToken))
            .Select(course => new StudentDashboardCourseItemDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                InstructorName = BuildDisplayName(course.FirstName, course.LastName),
                ProgressPercent = course.ProgressPercent
            })
            .ToList();

        var upcomingLiveSessions = await _dbContext.LiveSessions
            .AsNoTracking()
            .Where(s =>
                s.StartTime >= nowUtc &&
                s.Status != LiveSessionStatus.Cancelled &&
                s.Course.Enrollments.Any(e => e.StudentId == studentId))
            .OrderBy(s => s.StartTime)
            .Take(5)
            .Select(s => new StudentDashboardLiveSessionItemDto
            {
                LiveSessionId = s.Id,
                Title = s.Title,
                CourseTitle = s.Course.Title,
                StartTime = s.StartTime,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status
            })
            .ToListAsync(cancellationToken);

        var pendingQuizzes = await _dbContext.Quizzes
            .Include(q => q.Course)
            .ThenInclude(c => c.Enrollments)
            .Include(q => q.Attempts)
            .Where(q =>
                q.IsPublished &&
                q.StartTimeUtc <= nowUtc &&
                q.EndTimeUtc >= nowUtc &&
                q.Course.Enrollments.Any(e => e.StudentId == studentId) &&
                !q.Attempts.Any(a =>
                    a.StudentId == studentId &&
                    a.Status == QuizAttemptStatus.InProgress &&
                    a.SubmittedAt == null &&
                    a.DeadlineUtc >= nowUtc) &&
                (q.AllowMultipleAttempts || !q.Attempts.Any(a =>
                    a.StudentId == studentId &&
                    (a.Status == QuizAttemptStatus.Submitted ||
                     a.Status == QuizAttemptStatus.PendingReview ||
                     a.Status == QuizAttemptStatus.Graded ||
                     a.SubmittedAt != null))))
            .OrderBy(q => q.StartTimeUtc)
            .Take(5)
            .Select(q => new StudentDashboardQuizItemDto
            {
                QuizId = q.Id,
                Title = q.Title,
                CourseTitle = q.Course.Title,
                DurationMinutes = q.DurationMinutes
            })
            .ToListAsync(cancellationToken);

        return new StudentDashboardResponseDto
        {
            Summary = new StudentDashboardSummaryDto
            {
                EnrolledCourses = enrolledCoursesCount,
                UpcomingLiveSessions = upcomingLiveSessions.Count,
                PendingQuizzes = pendingQuizzes.Count
            },
            MyCourses = myCourses,
            UpcomingLiveSessions = upcomingLiveSessions,
            PendingQuizzes = pendingQuizzes
        };
    }

    private static string BuildDisplayName(string? firstName, string? lastName)
    {
        return string.Join(
            " ",
            new[] { firstName, lastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));
    }
}
