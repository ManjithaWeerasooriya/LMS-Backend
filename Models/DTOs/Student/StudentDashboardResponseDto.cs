using System.Collections.Generic;

namespace LMS_Backend.Models.DTOs.Student;

public class StudentDashboardResponseDto
{
    public StudentDashboardSummaryDto Summary { get; set; } = new();
    public List<StudentDashboardCourseItemDto> MyCourses { get; set; } = new();
    public List<StudentDashboardLiveSessionItemDto> UpcomingLiveSessions { get; set; } = new();
    public List<StudentDashboardQuizItemDto> PendingQuizzes { get; set; } = new();
}
