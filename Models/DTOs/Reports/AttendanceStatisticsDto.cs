using System;
using System.Collections.Generic;
using LMS_Backend.Models.Entities;

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
    public Guid LiveSessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string? CourseTitle { get; set; }
    public int StudentsEnrolled { get; set; }
    public LiveSessionStatus Status { get; set; }
}
