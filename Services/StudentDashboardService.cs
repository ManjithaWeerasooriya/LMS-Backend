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

        var myCourses = await enrollmentsQuery
            .GroupBy(e => new
            {
                e.CourseId,
                e.Course.Title,
                e.Course.Teacher.FirstName,
                e.Course.Teacher.LastName
            })
            .Select(g => new StudentDashboardCourseItemDto
            {
                CourseId = g.Key.CourseId,
                Title = g.Key.Title,
                InstructorName = string.Join(" ",
                    new[] { g.Key.FirstName, g.Key.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                ProgressPercent = g.Average(e => e.ProgressPercent)
            })
            .OrderByDescending(c => c.ProgressPercent)
            .Take(4)
            .ToListAsync(cancellationToken);

        var upcomingClasses = await _dbContext.LiveClasses
            .Include(l => l.Course)
            .ThenInclude(c => c.Enrollments)
            .Where(l =>
                l.ScheduledAt >= nowUtc &&
                l.CourseId != null &&
                l.Course.Enrollments.Any(e => e.StudentId == studentId))
            .OrderBy(l => l.ScheduledAt)
            .Take(5)
            .Select(l => new StudentDashboardLiveClassItemDto
            {
                LiveClassId = l.Id,
                Topic = l.Topic,
                CourseTitle = l.Course != null ? l.Course.Title : null,
                ScheduledAt = l.ScheduledAt,
                DurationMinutes = l.DurationMinutes
            })
            .ToListAsync(cancellationToken);

        var pendingQuizzes = await _dbContext.Quizzes
            .Include(q => q.Course)
            .ThenInclude(c => c.Enrollments)
            .Include(q => q.Attempts)
            .Where(q =>
                q.IsPublished &&
                q.Course.Enrollments.Any(e => e.StudentId == studentId) &&
                !q.Attempts.Any(a => a.StudentId == studentId))
            .OrderBy(q => q.CreatedAt)
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
                UpcomingClasses = upcomingClasses.Count,
                PendingQuizzes = pendingQuizzes.Count
            },
            MyCourses = myCourses,
            UpcomingClasses = upcomingClasses,
            PendingQuizzes = pendingQuizzes
        };
    }
}

