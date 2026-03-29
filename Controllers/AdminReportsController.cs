using System.Linq;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Reports;
using LMS_Backend.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Policy = AppPolicies.TeacherOnly)]
public class AdminReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public AdminReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ReportOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        var overview = new ReportOverviewDto
        {
            Enrollment = await _reportingService.GetEnrollmentStatisticsAsync(null, cancellationToken),
            Quizzes = await _reportingService.GetQuizStatisticsAsync(null, cancellationToken),
            Attendance = await _reportingService.GetAttendanceStatisticsAsync(null, cancellationToken)
        };

        return Ok(overview);
    }

    [HttpGet("courses")]
    public async Task<ActionResult<CoursesReportDto>> GetCoursesReport(CancellationToken cancellationToken)
    {
        var enrollment = await _reportingService.GetEnrollmentStatisticsAsync(null, cancellationToken);
        var completionRates = await _reportingService.GetCourseCompletionRatesAsync(null, cancellationToken);

        var response = new CoursesReportDto
        {
            Enrollment = enrollment,
            CompletionRates = completionRates.ToList()
        };

        return Ok(response);
    }

    [HttpGet("quizzes")]
    public async Task<ActionResult<QuizStatisticsDto>> GetQuizReport(CancellationToken cancellationToken)
    {
        var quizStats = await _reportingService.GetQuizStatisticsAsync(null, cancellationToken);
        return Ok(quizStats);
    }
}
