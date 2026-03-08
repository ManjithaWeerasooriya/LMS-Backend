using System;

namespace LMS_Backend.Models.DTOs.Assignments;

public class AssignmentListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int PendingSubmissions { get; set; }
    public int TotalSubmissions { get; set; }
}

