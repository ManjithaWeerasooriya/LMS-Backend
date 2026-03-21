using System;

namespace LMS_Backend.Models.DTOs.Student;

public class StudentDashboardQuizItemDto
{
    public Guid QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}

