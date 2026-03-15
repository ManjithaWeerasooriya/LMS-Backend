using System;

namespace LMS_Backend.Models.DTOs.LiveClasses;

public class LiveClassListItemDto
{
    public Guid Id { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? CourseTitle { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int StudentsEnrolled { get; set; }
    public string? MeetingLink { get; set; }
}

