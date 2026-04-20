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
    public async Task TeacherCreateLiveSession_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateTeacherController(serviceMock.Object, userId: null);

        var result = await controller.CreateLiveSession(
            Guid.NewGuid(),
            new CreateLiveSessionRequestDto
            {
                Title = "Unauthorized session",
                StartTime = DateTime.UtcNow.AddHours(2),
                DurationMinutes = 60
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        serviceMock.Verify(
            service => service.CreateLiveSessionAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CreateLiveSessionRequestDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TeacherStartLiveSession_ReturnsSuccessResponse_AndUsesTeacherIdentity()
    {
        var sessionId = Guid.NewGuid();
        var responseDto = new LiveSessionDto
        {
            Id = sessionId,
            CourseId = Guid.NewGuid(),
            Title = "Started session",
            Status = LiveSessionStatus.Live
        };

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock
            .Setup(service => service.StartLiveSessionAsync(
                "teacher-1",
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.StartLiveSession(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionDto>>(ok.Value);

        Assert.True(payload.Success);
        Assert.Equal(sessionId, payload.Data!.Id);
        Assert.Equal(LiveSessionStatus.Live, payload.Data.Status);

        serviceMock.VerifyAll();
    }

    [Fact]
    public async Task TeacherStartLiveSession_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateTeacherController(serviceMock.Object, userId: null);

        var result = await controller.StartLiveSession(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        serviceMock.Verify(
            service => service.StartLiveSessionAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TeacherEndLiveSession_ReturnsSuccessResponse_AndUsesTeacherIdentity()
    {
        var sessionId = Guid.NewGuid();
        var responseDto = new LiveSessionDto
        {
            Id = sessionId,
            CourseId = Guid.NewGuid(),
            Title = "Ended session",
            Status = LiveSessionStatus.Ended
        };

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock
            .Setup(service => service.EndLiveSessionAsync(
                "teacher-1",
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.EndLiveSession(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionDto>>(ok.Value);

        Assert.True(payload.Success);
        Assert.Equal(sessionId, payload.Data!.Id);
        Assert.Equal(LiveSessionStatus.Ended, payload.Data.Status);

        serviceMock.VerifyAll();
    }

    [Fact]
    public async Task TeacherEndLiveSession_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateTeacherController(serviceMock.Object, userId: null);

        var result = await controller.EndLiveSession(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        serviceMock.Verify(
            service => service.EndLiveSessionAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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
    public async Task StudentGetLiveSessionById_WithoutAuthenticatedStudent_ReturnsUnauthorized()
    {
        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateStudentController(serviceMock.Object, userId: null);

        var result = await controller.GetLiveSessionById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        serviceMock.Verify(
            service => service.GetStudentLiveSessionByIdAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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
    public async Task CreateJoinToken_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        var liveSessionService = new Mock<ILiveSessionService>();
        var joinService = new Mock<ILiveSessionJoinService>();

        var controller = CreateLiveSessionsController(
            liveSessionService.Object,
            joinService.Object,
            userId: null,
            AppRoles.Student);

        var result = await controller.CreateJoinToken(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        joinService.Verify(
            service => service.CreateJoinTokenAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateJoinToken_AsTeacher_ReturnsSuccessResponse()
    {
        var sessionId = Guid.NewGuid();

        var joinToken = new LiveSessionJoinTokenResponseDto
        {
            AcsUserId = "acs-teacher",
            Token = "token",
            Session = new LiveSessionJoinMetadataDto
            {
                Id = sessionId,
                CourseId = Guid.NewGuid(),
                Title = "Teacher joinable session"
            }
        };

        var liveSessionService = new Mock<ILiveSessionService>();
        var joinService = new Mock<ILiveSessionJoinService>();

        joinService
            .Setup(service => service.CreateJoinTokenAsync(
                "teacher-1",
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(joinToken);

        var controller = CreateLiveSessionsController(
            liveSessionService.Object,
            joinService.Object,
            "teacher-1",
            AppRoles.Teacher);

        var result = await controller.CreateJoinToken(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionJoinTokenResponseDto>>(ok.Value);

        Assert.True(payload.Success);
        Assert.Equal("acs-teacher", payload.Data!.AcsUserId);

        joinService.VerifyAll();
    }

    [Fact]
    public async Task TeacherCreateLiveSession_PassesCorrectTeacherIdAndCourseId_ToService()
    {
        var courseId = Guid.NewGuid();

        var request = new CreateLiveSessionRequestDto
        {
            Title = "Teacher session",
            StartTime = DateTime.UtcNow.AddHours(2),
            DurationMinutes = 60
        };

        var serviceMock = new Mock<ILiveSessionService>();

        serviceMock
            .Setup(service => service.CreateLiveSessionAsync(
                "teacher-1",
                courseId,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveSessionDto
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Teacher session",
                Status = LiveSessionStatus.Scheduled
            });

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        await controller.CreateLiveSession(courseId, request, CancellationToken.None);

        serviceMock.Verify(service => service.CreateLiveSessionAsync(
            "teacher-1",
            courseId,
            request,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TeacherUpdateLiveSession_ReturnsSuccessResponse_AndUsesTeacherIdentity()
    {
        var sessionId = Guid.NewGuid();

        var responseDto = new LiveSessionDto
        {
            Id = sessionId,
            CourseId = Guid.NewGuid(),
            Title = "Updated session",
            Status = LiveSessionStatus.Scheduled
        };

        var request = new UpdateLiveSessionRequestDto
        {
            Title = "Updated session",
            StartTime = DateTime.UtcNow.AddHours(3),
            DurationMinutes = 90
        };

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock
            .Setup(service => service.UpdateLiveSessionAsync(
                "teacher-1",
                sessionId,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.UpdateLiveSession(
            sessionId,
            request,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<LiveSessionDto>>(ok.Value);

        Assert.True(payload.Success);
        Assert.Equal(sessionId, payload.Data!.Id);
        Assert.Equal("Updated session", payload.Data.Title);

        serviceMock.VerifyAll();
    }

    [Fact]
    public async Task TeacherUpdateLiveSession_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var sessionId = Guid.NewGuid();

        var request = new UpdateLiveSessionRequestDto
        {
            Title = "Updated session",
            StartTime = DateTime.UtcNow.AddHours(3),
            DurationMinutes = 90
        };

        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateTeacherController(serviceMock.Object, userId: null);

        var result = await controller.UpdateLiveSession(
            sessionId,
            request,
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        serviceMock.Verify(
            service => service.UpdateLiveSessionAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<UpdateLiveSessionRequestDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TeacherCancelLiveSession_ReturnsSuccessResponse_AndUsesTeacherIdentity()
    {
        var sessionId = Guid.NewGuid();

        var serviceMock = new Mock<ILiveSessionService>();
        serviceMock
            .Setup(service => service.CancelLiveSessionAsync(
                "teacher-1",
                sessionId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateTeacherController(serviceMock.Object, "teacher-1");

        var result = await controller.CancelLiveSession(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<object>>(ok.Value);

        Assert.True(payload.Success);

        serviceMock.Verify(
            service => service.CancelLiveSessionAsync(
                "teacher-1",
                sessionId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TeacherCancelLiveSession_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var serviceMock = new Mock<ILiveSessionService>();
        var controller = CreateTeacherController(serviceMock.Object, userId: null);

        var result = await controller.CancelLiveSession(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);

        serviceMock.Verify(
            service => service.CancelLiveSessionAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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