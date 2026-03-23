using System.Linq;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Teacher;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services.Reporting;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class TeacherDashboardService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IReportingService _reportingService;

    public TeacherDashboardService(ApplicationDBContext dbContext, IReportingService reportingService)
    {
        _dbContext = dbContext;
        _reportingService = reportingService;
    }

    public async Task<TeacherDashboardResponseDto> GetDashboardAsync(
        string teacherId,
        CancellationToken cancellationToken)
    {
        var coursesQuery = _dbContext
            .Courses
            .Include(c => c.Enrollments)
            .Where(c => c.TeacherId == teacherId && c.Status == CourseStatus.Active);

        var myCoursesCount = await coursesQuery.CountAsync(cancellationToken);

        var enrollmentStats = await _reportingService.GetEnrollmentStatisticsAsync(teacherId, cancellationToken);
        var quizStats = await _reportingService.GetQuizStatisticsAsync(teacherId, cancellationToken);
        var attendanceStats = await _reportingService.GetAttendanceStatisticsAsync(teacherId, cancellationToken);
        var completionRates = await _reportingService.GetCourseCompletionRatesAsync(teacherId, cancellationToken);

        var pendingSubmissionsCount = await _dbContext.AssignmentSubmissions
            .Where(s =>
                s.Status == SubmissionStatus.Pending &&
                s.Assignment.Course.TeacherId == teacherId)
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

        var upcomingLiveSessions = attendanceStats.UpcomingSessionDetails
            .Select(l => new TeacherDashboardLiveSessionItemDto
            {
                LiveClassId = l.LiveClassId,
                Topic = l.Topic,
                ScheduledAt = l.ScheduledAt,
                CourseTitle = l.CourseTitle,
                StudentsEnrolled = l.StudentsEnrolled,
                MeetingLink = l.MeetingLink
            })
            .ToList();

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
                TotalStudents = enrollmentStats.TotalStudents,
                PendingSubmissions = pendingSubmissionsCount,
                UpcomingLiveSessions = attendanceStats.UpcomingSessions
            },
            MyCourses = myCourses,
            Performance = new TeacherDashboardPerformanceDto
            {
                ExcellentPercentage = quizStats.PerformanceBands.ExcellentPercentage,
                GoodPercentage = quizStats.PerformanceBands.GoodPercentage,
                AveragePercentage = quizStats.PerformanceBands.AveragePercentage,
                NeedsImprovementPercentage = quizStats.PerformanceBands.NeedsImprovementPercentage
            },
            CompletionRates = completionRates.ToList(),
            UpcomingLiveSessions = upcomingLiveSessions,
            PendingSubmissions = pendingSubmissions
        };
    }
}
