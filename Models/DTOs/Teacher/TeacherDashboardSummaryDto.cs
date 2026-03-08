namespace LMS_Backend.Models.DTOs.Teacher;

public class TeacherDashboardSummaryDto
{
    public int MyCourses { get; set; }
    public int TotalStudents { get; set; }
    public int PendingSubmissions { get; set; }
    public int UpcomingLiveSessions { get; set; }
}

