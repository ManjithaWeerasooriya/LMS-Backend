using System.Collections.Generic;
using LMS_Backend.Models.DTOs.Teacher;

namespace LMS_Backend.Models.DTOs.Reports;

public class ReportOverviewDto
{
    public EnrollmentStatisticsDto Enrollment { get; set; } = new();
    public QuizStatisticsDto Quizzes { get; set; } = new();
    public AttendanceStatisticsDto Attendance { get; set; } = new();
}

public class CoursesReportDto
{
    public EnrollmentStatisticsDto Enrollment { get; set; } = new();
    public List<CourseCompletionRateItemDto> CompletionRates { get; set; } = new();
}
