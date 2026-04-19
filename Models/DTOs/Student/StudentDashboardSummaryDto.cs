using System;

namespace LMS_Backend.Models.DTOs.Student;

public class StudentDashboardSummaryDto
{
    public int EnrolledCourses { get; set; }
    public int UpcomingLiveSessions { get; set; }
    public int PendingQuizzes { get; set; }
}
