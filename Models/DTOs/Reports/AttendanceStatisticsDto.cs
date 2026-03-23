using System;
using System.Collections.Generic;

namespace LMS_Backend.Models.DTOs.Reports;

public class AttendanceStatisticsDto
{
    public int UpcomingSessions { get; set; }
    public int CompletedSessionsLast30Days { get; set; }
    public double? AttendanceRate { get; set; }
    public bool AttendanceTrackingAvailable { get; set; }
    public string? AttendanceTrackingNote { get; set; }
    public List<LiveSessionSummaryDto> UpcomingSessionDetails { get; set; } = new();
}

public class LiveSessionSummaryDto
{
    public Guid LiveClassId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string? CourseTitle { get; set; }
    public int StudentsEnrolled { get; set; }
    public string? MeetingLink { get; set; }
}
