using System.Collections.Generic;

namespace LMS_Backend.Models.DTOs.Teacher;

public class TeacherDashboardResponseDto
{
    public TeacherDashboardSummaryDto Summary { get; set; } = new();
    public List<TeacherDashboardCourseItemDto> MyCourses { get; set; } = new();
    public TeacherDashboardPerformanceDto Performance { get; set; } = new();
    public List<CourseCompletionRateItemDto> CompletionRates { get; set; } = new();
    public List<TeacherDashboardLiveSessionItemDto> UpcomingLiveSessions { get; set; } = new();
    public List<TeacherDashboardSubmissionItemDto> PendingSubmissions { get; set; } = new();
}

