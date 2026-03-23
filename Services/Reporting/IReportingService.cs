using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMS_Backend.Models.DTOs.Reports;
using LMS_Backend.Models.DTOs.Teacher;

namespace LMS_Backend.Services.Reporting;

public interface IReportingService
{
    Task<EnrollmentStatisticsDto> GetEnrollmentStatisticsAsync(string? teacherId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseCompletionRateItemDto>> GetCourseCompletionRatesAsync(string? teacherId, CancellationToken cancellationToken);
    Task<QuizStatisticsDto> GetQuizStatisticsAsync(string? teacherId, CancellationToken cancellationToken);
    Task<AttendanceStatisticsDto> GetAttendanceStatisticsAsync(string? teacherId, CancellationToken cancellationToken);
}
