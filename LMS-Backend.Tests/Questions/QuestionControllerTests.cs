using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LMS_Backend.Tests.Questions;

public class QuestionControllerTests
{
    [Fact]
    public async Task GetQuestions_WithTeacher_ReturnsOrderedList()
    {
        var quizId = Guid.NewGuid();
        var response = new List<QuestionResponseDto>
        {
            new() { Id = Guid.NewGuid(), QuizId = quizId, Text = "Q1", OrderIndex = 1, Type = QuestionType.SingleMcq },
            new() { Id = Guid.NewGuid(), QuizId = quizId, Text = "Q2", OrderIndex = 2, Type = QuestionType.Essay }
        };

        var mockService = new Mock<IQuizService>();
        mockService
            .Setup(s => s.GetQuestionsAsync("teacher-1", quizId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController(mockService, CreateUser("teacher-1"));
        var result = await controller.GetQuestions(quizId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<IReadOnlyList<QuestionResponseDto>>>(ok.Value);
        Assert.Equal(2, payload.Data!.Count);
        Assert.Equal(QuestionType.SingleMcq, payload.Data[0].Type);
    }

    [Fact]
    public async Task CreateQuestion_WithMcqOptions_ReturnsCreated()
    {
        var quizId = Guid.NewGuid();
        var question = new QuestionResponseDto
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            Text = "Capital of France?",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionResponseDto>
            {
                new() { Id = Guid.NewGuid(), Text = "Paris", IsCorrect = true },
                new() { Id = Guid.NewGuid(), Text = "Rome", IsCorrect = false }
            }
        };

        var mockService = new Mock<IQuizService>();
        mockService
            .Setup(s => s.CreateQuestionAsync("teacher-1", quizId, It.IsAny<CreateQuestionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        var controller = CreateController(mockService, CreateUser("teacher-1"));
        var dto = new CreateQuestionDto
        {
            Text = "Capital of France?",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "Paris", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "Rome", IsCorrect = false, OrderIndex = 2 }
            }
        };

        var result = await controller.CreateQuestion(quizId, dto, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var payload = Assert.IsType<ApiResponse<QuestionResponseDto>>(created.Value);
        Assert.Equal(QuestionType.SingleMcq, payload.Data!.Type);
        Assert.Equal(2, payload.Data.Options.Count);
    }

    [Fact]
    public async Task CreateQuestion_EssayType_ReturnsCreatedWithoutOptions()
    {
        var quizId = Guid.NewGuid();
        var question = new QuestionResponseDto
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            Text = "Explain SOLID principles",
            Type = QuestionType.Essay,
            Marks = 10,
            OrderIndex = 2
        };

        var mockService = new Mock<IQuizService>();
        mockService
            .Setup(s => s.CreateQuestionAsync("teacher-1", quizId, It.IsAny<CreateQuestionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        var controller = CreateController(mockService, CreateUser("teacher-1"));
        var dto = new CreateQuestionDto
        {
            Text = "Explain SOLID principles",
            Type = QuestionType.Essay,
            Marks = 10,
            OrderIndex = 2
        };

        var result = await controller.CreateQuestion(quizId, dto, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var payload = Assert.IsType<ApiResponse<QuestionResponseDto>>(created.Value);
        Assert.Empty(payload.Data!.Options);
    }

    [Fact]
    public async Task DeleteQuestion_WhenForbidden_ReturnsStatus403()
    {
        var mockService = new Mock<IQuizService>();
        mockService
            .Setup(s => s.DeleteQuestionAsync("teacher-1", It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("cannot"));

        var controller = CreateController(mockService, CreateUser("teacher-1"));
        var result = await controller.DeleteQuestion(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    private static TeacherQuizQuestionsController CreateController(Mock<IQuizService> serviceMock, ClaimsPrincipal? user)
    {
        var controller = new TeacherQuizQuestionsController(serviceMock.Object)
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
