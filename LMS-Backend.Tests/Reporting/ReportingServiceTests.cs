using LMS_Backend.Models.Entities;
using LMS_Backend.Services.Reporting;
using LMS_Backend.Tests.LiveSessions;

namespace LMS_Backend.Tests.Reporting;

public class ReportingServiceTests
{
    [Fact]
    public async Task GetAttendanceStatisticsAsync_UsesLiveSessionsAndAttendanceRecords()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        await fixture.EnrollStudentAsync(fixture.SecondEnrolledStudent);
        var service = new ReportingService(fixture.Context);

        var upcomingSession = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Scheduled,
            startTime: DateTime.UtcNow.AddHours(2));

        var endedSession = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Ended,
            startTime: DateTime.UtcNow.AddDays(-1));

        await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Cancelled,
            startTime: DateTime.UtcNow.AddHours(3));

        fixture.Context.LiveSessionAttendances.Add(new LiveSessionAttendance
        {
            SessionId = endedSession.Id,
            Session = endedSession,
            StudentId = fixture.EnrolledStudent.Id,
            Student = fixture.EnrolledStudent,
            JoinTime = endedSession.StartTime.AddMinutes(3),
            LeaveTime = endedSession.StartTime.AddMinutes(50),
            DurationSeconds = 47 * 60,
            AttendanceStatus = AttendanceStatus.Present,
            LastSeenAt = endedSession.StartTime.AddMinutes(50)
        });

        await fixture.Context.SaveChangesAsync();

        var statistics = await service.GetAttendanceStatisticsAsync(
            fixture.Teacher.Id,
            CancellationToken.None);

        Assert.Equal(1, statistics.UpcomingSessions);
        Assert.Equal(1, statistics.CompletedSessionsLast30Days);
        Assert.True(statistics.AttendanceTrackingAvailable);
        Assert.Equal(50.0, statistics.AttendanceRate);
        Assert.Contains("joined ended live sessions", statistics.AttendanceTrackingNote);

        var upcomingSummary = Assert.Single(statistics.UpcomingSessionDetails);
        Assert.Equal(upcomingSession.Id, upcomingSummary.LiveSessionId);
        Assert.Equal(upcomingSession.Title, upcomingSummary.Title);
        Assert.Equal(upcomingSession.StartTime, upcomingSummary.StartTime);
        Assert.Equal(LiveSessionStatus.Scheduled, upcomingSummary.Status);
        Assert.Equal(fixture.OwnedCourse.Title, upcomingSummary.CourseTitle);
        Assert.Equal(2, upcomingSummary.StudentsEnrolled);
    }
}
