using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Exceptions;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LMS_Backend.Tests.Quizzes;

public class QuizControllerTests
{
    [Fact]
    public async Task GetQuizzesByCourse_WithTeacherUser_ReturnsOk()
    {
        var mockService = new Mock<IQuizService>();
        var quizzes = new List<QuizResponseDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                Title = "Quiz 1",
                TotalMarks = 50,
                QuestionCount = 3
            }
        };
        mockService
            .Setup(s => s.GetTeacherQuizzesByCourseAsync("teacher-1", quizzes[0].CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quizzes);

        var controller = CreateController(mockService, CreateUser("teacher-1"));

        var result = await controller.GetQuizzesByCourse(quizzes[0].CourseId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<IReadOnlyList<QuizResponseDto>>>(okResult.Value);
        Assert.True(payload.Success);
        Assert.Single(payload.Data!);
    }

    [Fact]
    public async Task GetQuizzesByCourse_WithoutUser_ReturnsUnauthorized()
    {
        var controller = CreateController(new Mock<IQuizService>(), user: null);
        var result = await controller.GetQuizzesByCourse(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CreateQuiz_WithValidPayload_ReturnsCreatedResponse()
    {
        var mockService = new Mock<IQuizService>();
        var courseId = Guid.NewGuid();
        var quizResponse = new QuizResponseDto
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = "Midterm",
            TotalMarks = 100,
            RandomizeQuestions = true
        };

        mockService
            .Setup(s => s.CreateQuizAsync("teacher-1", It.IsAny<CreateQuizDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(quizResponse);

        var controller = CreateController(mockService, CreateUser("teacher-1"));
        var dto = new CreateQuizDto
        {
            CourseId = courseId,
            Title = "Midterm",
            Description = "Units 1-3",
            DurationMinutes = 60,
            StartTimeUtc = DateTime.UtcNow.AddHours(-1),
            EndTimeUtc = DateTime.UtcNow.AddHours(2),
            TotalMarks = 100,
            RandomizeQuestions = true,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = false
        };

        var result = await controller.CreateQuiz(dto, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var payload = Assert.IsType<ApiResponse<QuizResponseDto>>(created.Value);
        Assert.Equal(courseId, payload.Data!.CourseId);
        mockService.Verify(s => s.CreateQuizAsync("teacher-1", It.Is<CreateQuizDto>(d => d.CourseId == courseId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateQuiz_WhenServiceThrowsForbidden_ReturnsStatus403()
    {
        var mockService = new Mock<IQuizService>();
        var quizId = Guid.NewGuid();
        mockService
            .Setup(s => s.UpdateQuizAsync("teacher-1", quizId, It.IsAny<UpdateQuizDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("denied"));

        var controller = CreateController(mockService, CreateUser("teacher-1"));
        var result = await controller.UpdateQuiz(quizId, new UpdateQuizDto
        {
            Title = "Midterm",
            Description = "Updated",
            DurationMinutes = 60,
            StartTimeUtc = DateTime.UtcNow.AddHours(-1),
            EndTimeUtc = DateTime.UtcNow.AddHours(2),
            TotalMarks = 100,
            RandomizeQuestions = true,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = false
        }, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task DeleteQuiz_WithAuthenticatedTeacher_ReturnsSuccessMessage()
    {
        var mockService = new Mock<IQuizService>();
        mockService
            .Setup(s => s.DeleteQuizAsync("teacher-1", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(mockService, CreateUser("teacher-1"));
        var result = await controller.DeleteQuiz(Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<object?>>(ok.Value);
        Assert.Equal("Quiz deleted successfully.", payload.Message);
    }

    [Fact]
    public async Task GetQuizById_WhenNotFound_Returns404()
    {
        var mockService = new Mock<IQuizService>();
        var quizId = Guid.NewGuid();
        mockService
            .Setup(s => s.GetTeacherQuizByIdAsync("teacher-1", quizId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Quiz not found."));

        var controller = CreateController(mockService, CreateUser("teacher-1"));
        var result = await controller.GetQuizById(quizId, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<object?>>(notFound.Value);
        Assert.Equal("Quiz not found.", payload.Message);
    }

    private static TeacherQuizzesController CreateController(Mock<IQuizService> serviceMock, ClaimsPrincipal? user)
    {
        var controller = new TeacherQuizzesController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        return controller;
    }

    private static ClaimsPrincipal CreateUser(string userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Teacher")
        }, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
