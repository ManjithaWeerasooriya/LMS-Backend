using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LMS_Backend.Tests.Quizzes;

public class StudentQuizzesControllerTests
{
    [Fact]
    public async Task GetQuizResult_WithPublishedResults_ReturnsOk()
    {
        var quizId = Guid.NewGuid();
        var service = new Mock<IQuizService>();
        service
            .Setup(s => s.GetStudentQuizResultAsync("student-1", quizId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudentQuizResultDto
            {
                QuizId = quizId,
                QuizTitle = "Final Quiz",
                CourseId = Guid.NewGuid(),
                CourseTitle = "Algorithms",
                AttemptId = Guid.NewGuid(),
                SubmittedAt = DateTime.UtcNow,
                Status = QuizAttemptStatus.Graded,
                TotalMarks = 100,
                AwardedMarks = 82,
                Percentage = 82,
                AreResultsPublished = true
            });

        var controller = CreateController(service, CreateStudent("student-1"));

        var result = await controller.GetQuizResult(quizId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<StudentQuizResultDto>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("Quiz result retrieved successfully.", payload.Message);
        Assert.Equal(quizId, payload.Data!.QuizId);
    }

    [Fact]
    public async Task GetQuizResult_WithUnpublishedResults_ReturnsFriendlyMessage()
    {
        var quizId = Guid.NewGuid();
        var service = new Mock<IQuizService>();
        service
            .Setup(s => s.GetStudentQuizResultAsync("student-1", quizId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StudentQuizResultDto
            {
                QuizId = quizId,
                QuizTitle = "Midterm Quiz",
                CourseId = Guid.NewGuid(),
                CourseTitle = "Databases",
                AttemptId = Guid.NewGuid(),
                SubmittedAt = DateTime.UtcNow,
                Status = QuizAttemptStatus.PendingReview,
                TotalMarks = 50,
                AreResultsPublished = false
            });

        var controller = CreateController(service, CreateStudent("student-1"));

        var result = await controller.GetQuizResult(quizId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<StudentQuizResultDto>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("Quiz results are not yet released.", payload.Message);
        Assert.False(payload.Data!.AreResultsPublished);
    }

    [Fact]
    public async Task GetQuizResult_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        var controller = CreateController(new Mock<IQuizService>(), user: null);

        var result = await controller.GetQuizResult(Guid.NewGuid(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<object?>>(unauthorized.Value);
        Assert.Equal("Authentication is required.", payload.Message);
    }

    private static StudentQuizzesController CreateController(Mock<IQuizService> service, ClaimsPrincipal? user)
    {
        return new StudentQuizzesController(service.Object)
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

    private static ClaimsPrincipal CreateStudent(string userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Student")
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }
}
