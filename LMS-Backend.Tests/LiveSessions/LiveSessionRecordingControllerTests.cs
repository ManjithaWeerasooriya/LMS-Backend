using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.LiveSessions;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LMS_Backend.Tests.LiveSessions;

public class LiveSessionRecordingControllerTests
{
    [Fact]
    public async Task TeacherStartRecording_ReturnsSuccessResponse_AndUsesTeacherIdentity()
    {
        var sessionId = Guid.NewGuid();

        var responseDto = new LiveSessionRecordingDto
        {
            SessionId = sessionId,
            CourseId = Guid.NewGuid(),
            SessionTitle = "Recorded session",
            PlaybackEnabled = true,
            RecordingStatus = LiveSessionRecordingStatus.InProgress,
            RecordingStartedAt = DateTime.UtcNow
        };

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock
            .Setup(service => service.StartRecordingAsync(
                "teacher-1",
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.StartRecording(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionRecordingDto>>(ok.Value);

        Assert.True(payload.Success);
        Assert.Equal(sessionId, payload.Data!.SessionId);
        Assert.Equal(LiveSessionRecordingStatus.InProgress, payload.Data.RecordingStatus);

        serviceMock.VerifyAll();
    }

    [Fact]
    public async Task TeacherStartRecording_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateTeacherController(serviceMock.Object, null);

        var result = await controller.StartRecording(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        serviceMock.Verify(
            s => s.StartRecordingAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TeacherStopRecording_ReturnsSuccessResponse_AndUsesTeacherIdentity()
    {
        var sessionId = Guid.NewGuid();

        var responseDto = new LiveSessionRecordingDto
        {
            SessionId = sessionId,
            CourseId = Guid.NewGuid(),
            SessionTitle = "Recorded session",
            PlaybackEnabled = true,
            RecordingStatus = LiveSessionRecordingStatus.Available,
            RecordingUrl = "https://recording-url",
            RecordingStartedAt = DateTime.UtcNow.AddMinutes(-30),
            RecordingStoppedAt = DateTime.UtcNow
        };

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock
            .Setup(service => service.StopRecordingAsync(
                "teacher-1",
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.StopRecording(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionRecordingDto>>(ok.Value);

        Assert.True(payload.Success);
        Assert.Equal(sessionId, payload.Data!.SessionId);
        Assert.Equal(LiveSessionRecordingStatus.Available, payload.Data.RecordingStatus);
        Assert.Equal("https://recording-url", payload.Data.RecordingUrl);

        serviceMock.VerifyAll();
    }

    [Fact]
    public async Task TeacherStopRecording_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateTeacherController(serviceMock.Object, null);

        var result = await controller.StopRecording(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        serviceMock.Verify(
            s => s.StopRecordingAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static TeacherLiveSessionsController CreateTeacherController(
        ILiveSessionService service,
        string? userId)
    {
        return new TeacherLiveSessionsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreatePrincipal(userId, AppRoles.Teacher)
                }
            }
        };
    }

    private static ClaimsPrincipal CreatePrincipal(string? userId, string role)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }
}