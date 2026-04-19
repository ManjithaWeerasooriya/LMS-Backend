using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using LMS_Backend.Services.Reporting;
using LMS_Backend.Tests.LiveSessions;

namespace LMS_Backend.Tests.Dashboard;

public class TeacherDashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsUpcomingLiveSessions_FromLiveSessions()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var reportingService = new ReportingService(fixture.Context);
        var service = new TeacherDashboardService(fixture.Context, reportingService);

        var upcomingSession = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Scheduled,
            startTime: DateTime.UtcNow.AddHours(2));

        var dashboard = await service.GetDashboardAsync(
            fixture.Teacher.Id,
            CancellationToken.None);

        Assert.Equal(1, dashboard.Summary.UpcomingLiveSessions);

        var liveSession = Assert.Single(dashboard.UpcomingLiveSessions);
        Assert.Equal(upcomingSession.Id, liveSession.LiveSessionId);
        Assert.Equal(upcomingSession.Title, liveSession.Title);
        Assert.Equal(upcomingSession.StartTime, liveSession.StartTime);
        Assert.Equal(LiveSessionStatus.Scheduled, liveSession.Status);
        Assert.Equal(fixture.OwnedCourse.Title, liveSession.CourseTitle);
        Assert.Equal(1, liveSession.StudentsEnrolled);
    }
}
