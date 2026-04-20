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

public class LiveSessionControllerTests
{
    [Fact]
    public async Task TeacherCreateLiveSession_ReturnsCreatedResponse_AndUsesTeacherIdentity()
    {
        var courseId = Guid.NewGuid();

        var responseDto = new LiveSessionDto
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = "Teacher session",
            Status = LiveSessionStatus.Scheduled
        };

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock.Setup(x => x.CreateLiveSessionAsync(
            "teacher-1",
            courseId,
            It.IsAny<CreateLiveSessionRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.CreateLiveSession(courseId,
            new CreateLiveSessionRequestDto
            {
                Title = "Teacher session",
                StartTime = DateTime.UtcNow.AddHours(2),
                DurationMinutes = 60
            },
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionDto>>(created.Value);

        Assert.True(payload.Success);
        Assert.Equal(responseDto.Id, payload.Data!.Id);
    }

    [Fact]
    public async Task TeacherCancelLiveSession_ReturnsSuccessResponse_AndUsesTeacherIdentity()
    {
        var sessionId = Guid.NewGuid();

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock.Setup(x => x.CancelLiveSessionAsync(
            "teacher-1",
            sessionId,
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.CancelLiveSession(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<object>>(ok.Value);

        Assert.True(payload.Success);

        serviceMock.Verify(x => x.CancelLiveSessionAsync(
            "teacher-1",
            sessionId,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TeacherCancelLiveSession_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateTeacherController(serviceMock.Object, null);

        var result = await controller.CancelLiveSession(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ---------------------------
    // 🔧 Helper Methods
    // ---------------------------

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