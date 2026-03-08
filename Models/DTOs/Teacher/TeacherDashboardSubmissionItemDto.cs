using System;

namespace LMS_Backend.Models.DTOs.Teacher;

public class TeacherDashboardSubmissionItemDto
{
    public Guid AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int PendingCount { get; set; }
    public int TotalCount { get; set; }
}

