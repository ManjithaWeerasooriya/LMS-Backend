using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Moq;

namespace LMS_Backend.Tests.LiveSessions;

public class LiveSessionJoinServiceTests
{
    [Fact]
    public async Task CreateJoinTokenAsync_AllowsCourseTeacher_AndEnsuresChatParticipation()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateJoinService();
        var session = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Live,
            roomId: "room-join",
            chatThreadId: "chat-thread-join");

        var token = await service.CreateJoinTokenAsync(
            fixture.Teacher.Id,
            session.Id,
            CancellationToken.None);

        Assert.Equal($"acs-{fixture.Teacher.Id}", token.AcsUserId);
        Assert.Equal("limited-token", token.Token);
        Assert.Equal(MeetingType.Room, token.MeetingType);
        Assert.Equal("room-join", token.RoomId);
        Assert.Null(token.GroupId);
        Assert.Null(token.MeetingLink);
        Assert.Equal("chat-thread-join", token.ChatThreadId);
        Assert.Equal(session.Id, token.Session.Id);

        fixture.AzureIdentityServiceMock.Verify(identityService => identityService.CreateJoinTokenAsync(
            It.Is<User>(user => user.Id == fixture.Teacher.Id),
            It.IsAny<string>(),
            true,
            It.IsAny<CancellationToken>()), Times.Once);

        fixture.AzureLiveSessionServiceMock.Verify(liveSessionService => liveSessionService.EnsureChatParticipantAsync(
            It.Is<User>(user => user.Id == fixture.Teacher.Id),
            It.IsAny<string>(),
            "chat-thread-join",
            It.Is<User>(user => user.Id == fixture.Teacher.Id),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateJoinTokenAsync_AllowsEnrolledStudents_ToJoinAndAccessChat()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateJoinService();
        var session = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Live,
            meetingType: MeetingType.Group,
            roomId: null,
            groupId: Guid.NewGuid().ToString(),
            chatThreadId: "chat-thread-class");

        var token = await service.CreateJoinTokenAsync(
            fixture.EnrolledStudent.Id,
            session.Id,
            CancellationToken.None);

        Assert.Equal($"acs-{fixture.EnrolledStudent.Id}", token.AcsUserId);
        Assert.Equal("full-token", token.Token);
        Assert.Equal(MeetingType.Group, token.MeetingType);
        Assert.Equal(session.GroupId, token.GroupId);
        Assert.Null(token.RoomId);
        Assert.Equal("chat-thread-class", token.ChatThreadId);
        Assert.Equal(session.Title, token.Session.Title);

        fixture.AzureIdentityServiceMock.Verify(identityService => identityService.CreateJoinTokenAsync(
            It.Is<User>(user => user.Id == fixture.EnrolledStudent.Id),
            It.IsAny<string>(),
            false,
            It.IsAny<CancellationToken>()), Times.Once);

        fixture.AzureLiveSessionServiceMock.Verify(liveSessionService => liveSessionService.EnsureChatParticipantAsync(
            It.Is<User>(user => user.Id == fixture.Teacher.Id),
            It.IsAny<string>(),
            "chat-thread-class",
            It.Is<User>(user => user.Id == fixture.EnrolledStudent.Id),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateJoinTokenAsync_RejectsNonParticipants_AndDoesNotGrantChatAccess()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateJoinService();
        var session = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Live,
            roomId: "room-join",
            chatThreadId: "chat-thread-private");

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateJoinTokenAsync(
            fixture.OutsiderStudent.Id,
            session.Id,
            CancellationToken.None));

        fixture.AzureIdentityServiceMock.Verify(identityService => identityService.CreateJoinTokenAsync(
            It.IsAny<User>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);

        fixture.AzureLiveSessionServiceMock.Verify(liveSessionService => liveSessionService.EnsureChatParticipantAsync(
            It.IsAny<User>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<User>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateJoinTokenAsync_ReturnsTeamsMeetingLocatorOnly()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateJoinService();
        var session = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Live,
            meetingType: MeetingType.Teams,
            roomId: null,
            meetingLink: "https://teams.microsoft.com/l/meetup-join/example",
            chatThreadId: "chat-thread-teams");

        var token = await service.CreateJoinTokenAsync(
            fixture.EnrolledStudent.Id,
            session.Id,
            CancellationToken.None);

        Assert.Equal(MeetingType.Teams, token.MeetingType);
        Assert.Equal(session.MeetingLink, token.MeetingLink);
        Assert.Null(token.RoomId);
        Assert.Null(token.GroupId);
        Assert.Null(token.MeetingId);
        Assert.Null(token.Passcode);
    }
}
