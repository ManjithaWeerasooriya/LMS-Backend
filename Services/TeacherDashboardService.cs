using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Teacher;
using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class TeacherDashboardService
{
    private readonly ApplicationDBContext _dbContext;

    public TeacherDashboardService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TeacherDashboardResponseDto> GetDashboardAsync(
        string teacherId,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var coursesQuery = _dbContext
            .Courses
            .Include(c => c.Enrollments)
            .Where(c => c.TeacherId == teacherId && c.Status == CourseStatus.Active);

        var myCoursesCount = await coursesQuery.CountAsync(cancellationToken);

        var totalStudents = await _dbContext.CourseEnrollments
            .Where(e => e.Course.TeacherId == teacherId)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);

        var pendingSubmissionsCount = await _dbContext.AssignmentSubmissions
            .Where(s =>
                s.Status == SubmissionStatus.Pending &&
                s.Assignment.Course.TeacherId == teacherId)
            .CountAsync(cancellationToken);

        var upcomingLiveSessionsCount = await _dbContext.LiveClasses
            .Where(l => l.TeacherId == teacherId && l.ScheduledAt >= nowUtc)
            .CountAsync(cancellationToken);

        var myCourses = await coursesQuery
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(4)
            .Select(c => new TeacherDashboardCourseItemDto
            {
                CourseId = c.Id,
                Title = c.Title,
                Students = c.Enrollments.Count,
                AverageProgressPercent = c.Enrollments.Any()
                    ? c.Enrollments.Average(e => e.ProgressPercent)
                    : 0,
                Status = c.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        var performance = await CalculatePerformanceAsync(teacherId, cancellationToken);

        var completionRates = await coursesQuery
            .Select(c => new CourseCompletionRateItemDto
            {
                CourseId = c.Id,
                CourseTitle = c.Title,
                CompletionRate = c.Enrollments.Any()
                    ? (double)c.Enrollments.Count(e => e.CompletedAt != null) /
                      c.Enrollments.Count * 100.0
                    : 0
            })
            .ToListAsync(cancellationToken);

        var upcomingLiveSessions = await _dbContext.LiveClasses
            .Where(l => l.TeacherId == teacherId && l.ScheduledAt >= nowUtc)
            .OrderBy(l => l.ScheduledAt)
            .Take(5)
            .Select(l => new TeacherDashboardLiveSessionItemDto
            {
                LiveClassId = l.Id,
                Topic = l.Topic,
                ScheduledAt = l.ScheduledAt,
                CourseTitle = l.Course != null ? l.Course.Title : null,
                StudentsEnrolled = l.Course != null
                    ? l.Course.Enrollments.Count
                    : 0
            })
            .ToListAsync(cancellationToken);

        var pendingSubmissions = await _dbContext.Assignments
            .Where(a => a.Course.TeacherId == teacherId)
            .OrderBy(a => a.DueDate)
            .Select(a => new TeacherDashboardSubmissionItemDto
            {
                AssignmentId = a.Id,
                AssignmentTitle = a.Title,
                CourseTitle = a.Course.Title,
                DueDate = a.DueDate,
                PendingCount = a.Submissions.Count(s => s.Status == SubmissionStatus.Pending),
                TotalCount = a.Submissions.Count
            })
            .Where(x => x.PendingCount > 0)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new TeacherDashboardResponseDto
        {
            Summary = new TeacherDashboardSummaryDto
            {
                MyCourses = myCoursesCount,
                TotalStudents = totalStudents,
                PendingSubmissions = pendingSubmissionsCount,
                UpcomingLiveSessions = upcomingLiveSessionsCount
            },
            MyCourses = myCourses,
            Performance = performance,
            CompletionRates = completionRates,
            UpcomingLiveSessions = upcomingLiveSessions,
            PendingSubmissions = pendingSubmissions
        };
    }

    private async Task<TeacherDashboardPerformanceDto> CalculatePerformanceAsync(
        string teacherId,
        CancellationToken cancellationToken)
    {
        var attempts = await _dbContext.QuizAttempts
            .Include(a => a.Quiz)
            .ThenInclude(q => q.Course)
            .Where(a => a.Quiz.Course.TeacherId == teacherId && a.Quiz.TotalMarks > 0)
            .ToListAsync(cancellationToken);

        if (attempts.Count == 0)
        {
            return new TeacherDashboardPerformanceDto();
        }

        var total = attempts.Count;
        var excellent = 0;
        var good = 0;
        var average = 0;
        var needsImprovement = 0;

        foreach (var attempt in attempts)
        {
            var percent = (double)attempt.Score / attempt.Quiz.TotalMarks * 100.0;

            if (percent >= 80)
            {
                excellent++;
            }
            else if (percent >= 60)
            {
                good++;
            }
            else if (percent >= 40)
            {
                average++;
            }
            else
            {
                needsImprovement++;
            }
        }

        static double Percent(int count, int totalCount) =>
            totalCount == 0 ? 0 : Math.Round((double)count / totalCount * 100.0, 1);

        return new TeacherDashboardPerformanceDto
        {
            ExcellentPercentage = Percent(excellent, total),
            GoodPercentage = Percent(good, total),
            AveragePercentage = Percent(average, total),
            NeedsImprovementPercentage = Percent(needsImprovement, total)
        };
    }
}

