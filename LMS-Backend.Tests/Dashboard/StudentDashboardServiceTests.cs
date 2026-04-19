using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using LMS_Backend.Tests.LiveSessions;

namespace LMS_Backend.Tests.Dashboard;

public class StudentDashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsUpcomingLiveSessions_ForEnrolledStudent()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = new StudentDashboardService(fixture.Context);

        var includedSession = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Scheduled,
            startTime: DateTime.UtcNow.AddHours(2));

        await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Cancelled,
            startTime: DateTime.UtcNow.AddHours(3));

        await fixture.SeedSessionAsync(
            course: fixture.OtherTeachersCourse,
            teacher: fixture.OtherTeacher,
            status: LiveSessionStatus.Scheduled,
            startTime: DateTime.UtcNow.AddHours(4));

        await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Ended,
            startTime: DateTime.UtcNow.AddHours(-3));

        var dashboard = await service.GetDashboardAsync(
            fixture.EnrolledStudent.Id,
            CancellationToken.None);

        Assert.Equal(1, dashboard.Summary.UpcomingLiveSessions);

        var upcomingSession = Assert.Single(dashboard.UpcomingLiveSessions);
        Assert.Equal(includedSession.Id, upcomingSession.LiveSessionId);
        Assert.Equal(includedSession.Title, upcomingSession.Title);
        Assert.Equal(includedSession.StartTime, upcomingSession.StartTime);
        Assert.Equal(includedSession.DurationMinutes, upcomingSession.DurationMinutes);
        Assert.Equal(LiveSessionStatus.Scheduled, upcomingSession.Status);
        Assert.Equal(fixture.OwnedCourse.Title, upcomingSession.CourseTitle);
    }
}
