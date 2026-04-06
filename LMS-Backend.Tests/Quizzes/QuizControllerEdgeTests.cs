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

public class QuizControllerEdgeTests
{
    [Fact]
    public async Task UpdateQuiz_WithEmptyTitle_ReturnsBadRequest()
    {
        var quizId = Guid.NewGuid();
        var service = new Mock<IQuizService>();
        service
            .Setup(s => s.UpdateQuizAsync("teacher-1", quizId, It.IsAny<UpdateQuizDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Title is required."));

        var controller = CreateController(service, CreateTeacher("teacher-1"));
        var dto = new UpdateQuizDto
        {
            Title = string.Empty,
            Description = "desc",
            DurationMinutes = 30,
            StartTimeUtc = DateTime.UtcNow,
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 50,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = false
        };

        var result = await controller.UpdateQuiz(quizId, dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object?>>(badRequest.Value);
        Assert.Equal("Title is required.", response.Message);
        service.VerifyAll();
    }

    [Fact]
    public async Task UpdateQuiz_WithInvalidDuration_ReturnsBadRequest()
    {
        var quizId = Guid.NewGuid();
        var service = new Mock<IQuizService>();
        service
            .Setup(s => s.UpdateQuizAsync("teacher-1", quizId, It.IsAny<UpdateQuizDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Duration must be positive."));

        var controller = CreateController(service, CreateTeacher("teacher-1"));
        var dto = new UpdateQuizDto
        {
            Title = "Midterm",
            Description = "desc",
            DurationMinutes = 0,
            StartTimeUtc = DateTime.UtcNow,
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 50,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = false
        };

        var result = await controller.UpdateQuiz(quizId, dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object?>>(badRequest.Value);
        Assert.Equal("Duration must be positive.", response.Message);
    }

    [Fact]
    public async Task DeleteQuiz_WhenQuizNotFound_ReturnsNotFound()
    {
        var quizId = Guid.NewGuid();
        var service = new Mock<IQuizService>();
        service
            .Setup(s => s.DeleteQuizAsync("teacher-1", quizId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Quiz not found."));

        var controller = CreateController(service, CreateTeacher("teacher-1"));
        var result = await controller.DeleteQuiz(quizId, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object?>>(notFound.Value);
        Assert.Equal("Quiz not found.", response.Message);
    }

    [Fact]
    public async Task GetQuizById_WhenServiceReportsMissingQuiz_ReturnsNotFound()
    {
        var quizId = Guid.NewGuid();
        var service = new Mock<IQuizService>();
        service
            .Setup(s => s.GetTeacherQuizByIdAsync("teacher-1", quizId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Unable to locate quiz."));

        var controller = CreateController(service, CreateTeacher("teacher-1"));
        var result = await controller.GetQuizById(quizId, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object?>>(notFound.Value);
        Assert.Equal("Unable to locate quiz.", response.Message);
    }

    private static TeacherQuizzesController CreateController(Mock<IQuizService> service, ClaimsPrincipal? user)
    {
        return new TeacherQuizzesController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
    }

    private static ClaimsPrincipal CreateTeacher(string userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Teacher")
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }
}
