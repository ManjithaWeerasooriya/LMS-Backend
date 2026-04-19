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
        serviceMock
            .Setup(service => service.CreateLiveSessionAsync(
                "teacher-1",
                courseId,
                It.IsAny<CreateLiveSessionRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.CreateLiveSession(
            courseId,
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

        serviceMock.VerifyAll();
    }

    [Fact]
    public async Task TeacherStartLiveSession_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var controller = CreateTeacherController(new Mock<ILiveSessionService>().Object, userId: null);

        var result = await controller.StartLiveSession(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task StudentGetLiveSessionById_ReturnsSuccessResponse_AndUsesStudentIdentity()
    {
        var sessionId = Guid.NewGuid();
        var responseDto = new LiveSessionDto
        {
            Id = sessionId,
            CourseId = Guid.NewGuid(),
            Title = "Student visible session",
            Status = LiveSessionStatus.Live
        };

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock
            .Setup(service => service.GetStudentLiveSessionByIdAsync(
                "student-1",
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateStudentController(serviceMock.Object, "student-1");

        var result = await controller.GetLiveSessionById(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionDto>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(sessionId, payload.Data!.Id);

        serviceMock.VerifyAll();
    }

    [Fact]
    public async Task CreateJoinToken_ReturnsSuccessResponse_AndUsesAuthenticatedUser()
    {
        var sessionId = Guid.NewGuid();
        var joinToken = new LiveSessionJoinTokenResponseDto
        {
            AcsUserId = "acs-user",
            Token = "token",
            Session = new LiveSessionJoinMetadataDto
            {
                Id = sessionId,
                CourseId = Guid.NewGuid(),
                Title = "Joinable session"
            }
        };

        var liveSessionService = new Mock<ILiveSessionService>();
        var joinService = new Mock<ILiveSessionJoinService>();
        joinService
            .Setup(service => service.CreateJoinTokenAsync(
                "student-1",
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(joinToken);

        var controller = CreateLiveSessionsController(
            liveSessionService.Object,
            joinService.Object,
            "student-1",
            AppRoles.Student);

        var result = await controller.CreateJoinToken(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionJoinTokenResponseDto>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("acs-user", payload.Data!.AcsUserId);

        joinService.VerifyAll();
    }

    [Fact]
    public async Task GetAttendanceSummary_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var controller = CreateTeacherController(new Mock<ILiveSessionService>().Object, userId: null);

        var result = await controller.GetLiveSessionAttendance(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
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

    private static StudentLiveSessionsController CreateStudentController(
        ILiveSessionService service,
        string? userId)
    {
        return new StudentLiveSessionsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreatePrincipal(userId, AppRoles.Student)
                }
            }
        };
    }

    private static LiveSessionsController CreateLiveSessionsController(
        ILiveSessionService liveSessionService,
        ILiveSessionJoinService joinService,
        string? userId,
        string role)
    {
        return new LiveSessionsController(liveSessionService, joinService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreatePrincipal(userId, role)
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
