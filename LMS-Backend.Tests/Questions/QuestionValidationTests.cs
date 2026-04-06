using System.ComponentModel.DataAnnotations;
using System.Threading;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Tests.Questions;

public class QuestionValidationTests
{
    [Fact]
    public async Task CreateQuestionAsync_WithDuplicateOrderIndex_ThrowsInvalidOperation()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Order Quiz",
            DurationMinutes = 10,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 10,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        await service.CreateQuestionAsync("teacher-1", quiz.Id, BuildMcq(1, 5), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateQuestionAsync("teacher-1", quiz.Id, BuildMcq(1, 5), CancellationToken.None));

        Assert.Contains("order", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateQuestionAsync_WhenMarksExceedQuizTotal_ThrowsInvalidOperation()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Marks Quiz",
            DurationMinutes = 10,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        await service.CreateQuestionAsync("teacher-1", quiz.Id, BuildMcq(1, 5), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateQuestionAsync("teacher-1", quiz.Id, BuildMcq(2, 1), CancellationToken.None));

        Assert.Contains("sum of question marks", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateQuestionDto_SingleChoice_WithSingleOption_FailsValidation()
    {
        var dto = new CreateQuestionDto
        {
            Text = "Capital?",
            Type = QuestionType.SingleMcq,
            Marks = 1,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "Paris", IsCorrect = true, OrderIndex = 1 }
            }
        };

        var validationResults = Validate(dto);
        Assert.Contains(validationResults, r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("at least two options", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateQuestionDto_Essay_WithOptions_FailsValidation()
    {
        var dto = new CreateQuestionDto
        {
            Text = "Explain",
            Type = QuestionType.Essay,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "Option", IsCorrect = true, OrderIndex = 1 }
            }
        };

        var validationResults = Validate(dto);
        Assert.Contains(validationResults, r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("must not define options", StringComparison.OrdinalIgnoreCase));
    }

    private static CreateQuestionDto BuildMcq(int orderIndex, decimal marks) => new()
    {
        Text = $"Question {orderIndex}",
        Type = QuestionType.SingleMcq,
        Marks = marks,
        OrderIndex = orderIndex,
        Options = new List<QuestionOptionRequestDto>
        {
            new() { Text = "A", IsCorrect = true, OrderIndex = 1 },
            new() { Text = "B", IsCorrect = false, OrderIndex = 2 }
        }
    };

    private static ApplicationDBContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDBContext(options);
    }

    private static Course CreateCourse(string teacherId) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Course",
        TeacherId = teacherId,
        Status = CourseStatus.Active
    };

    private static List<ValidationResult> Validate(object dto)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        Validator.TryValidateObject(dto, context, validationResults, true);
        return validationResults;
    }
}
