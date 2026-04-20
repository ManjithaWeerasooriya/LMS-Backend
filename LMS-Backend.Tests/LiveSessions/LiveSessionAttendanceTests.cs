using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.LiveSessions;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LMS_Backend.Tests.LiveSessions;

public class LiveSessionAttendanceControllerTests
{
    [Fact]
    public async Task TeacherGetAttendanceSummary_ReturnsSuccessResponse_AndUsesTeacherIdentity()
    {
        var sessionId = Guid.NewGuid();

        var responseDto = new LiveSessionAttendanceSummaryDto
        {
            SessionId = sessionId
        };

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock
            .Setup(service => service.GetLiveSessionAttendanceSummaryAsync(
                "teacher-1",
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.GetLiveSessionAttendance(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionAttendanceSummaryDto>>(ok.Value);

        Assert.True(payload.Success);
        Assert.Equal(sessionId, payload.Data!.SessionId);

        serviceMock.VerifyAll();
    }

    [Fact]
    public async Task TeacherGetAttendanceSummary_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateTeacherController(serviceMock.Object, userId: null);

        var result = await controller.GetLiveSessionAttendance(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        serviceMock.Verify(
            service => service.GetLiveSessionAttendanceSummaryAsync(
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