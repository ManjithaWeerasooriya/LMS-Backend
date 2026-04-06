using System.ComponentModel.DataAnnotations;
using System.Threading;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        var quiz = await CreateQuizAsync(service, course.Id, 10);

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
        var quiz = await CreateQuizAsync(service, course.Id, 5);

        await service.CreateQuestionAsync("teacher-1", quiz.Id, BuildMcq(1, 5), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateQuestionAsync("teacher-1", quiz.Id, BuildMcq(2, 1), CancellationToken.None));

        Assert.Contains("sum of question marks", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(CreateQuestionDto))]
    [InlineData(typeof(UpdateQuestionDto))]
    public void QuestionRequestDtos_OptionsMetadata_IsNotRequired(Type dtoType)
    {
        var services = new ServiceCollection();
        services.AddControllers();

        using var provider = services.BuildServiceProvider();
        var metadataProvider = provider.GetRequiredService<IModelMetadataProvider>();
        var metadata = metadataProvider.GetMetadataForType(dtoType);
        var optionsMetadata = metadata.Properties.Single(property =>
            property.PropertyName == nameof(CreateQuestionDto.Options));

        Assert.False(optionsMetadata.IsRequired);
    }

    [Theory]
    [InlineData(QuestionType.Essay)]
    [InlineData(QuestionType.ShortAnswer)]
    [InlineData(QuestionType.FileUpload)]
    public async Task CreateQuestionAsync_SubjectiveTypesWithoutOptions_Succeed(QuestionType type)
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await CreateQuizAsync(service, course.Id, 10);

        var result = await service.CreateQuestionAsync(
            "teacher-1",
            quiz.Id,
            new CreateQuestionDto
            {
                Text = $"Prompt for {type}",
                Type = type,
                Marks = 5,
                OrderIndex = 1
            },
            CancellationToken.None);

        Assert.Equal(type, result.Type);
        Assert.Empty(result.Options);
    }

    [Fact]
    public async Task CreateQuestionAsync_SingleChoiceWithoutOptions_FailsValidation()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await CreateQuizAsync(service, course.Id, 10);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateQuestionAsync(
                "teacher-1",
                quiz.Id,
                new CreateQuestionDto
                {
                    Text = "Capital?",
                    Type = QuestionType.SingleMcq,
                    Marks = 1,
                    OrderIndex = 1
                },
                CancellationToken.None));

        Assert.Contains("Single choice questions must define at least two options.", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateQuestionAsync_SingleChoiceWithoutCorrectOption_FailsValidation()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await CreateQuizAsync(service, course.Id, 10);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateQuestionAsync(
                "teacher-1",
                quiz.Id,
                new CreateQuestionDto
                {
                    Text = "Capital?",
                    Type = QuestionType.SingleMcq,
                    Marks = 1,
                    OrderIndex = 1,
                    Options = new List<QuestionOptionRequestDto>
                    {
                        new() { Text = "Paris", IsCorrect = false, OrderIndex = 1 },
                        new() { Text = "Rome", IsCorrect = false, OrderIndex = 2 }
                    }
                },
                CancellationToken.None));

        Assert.Contains("Single choice questions must have exactly one correct option.", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateQuestionAsync_MultipleChoiceWithEmptyOptions_FailsValidation()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await CreateQuizAsync(service, course.Id, 10);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateQuestionAsync(
                "teacher-1",
                quiz.Id,
                new CreateQuestionDto
                {
                    Text = "Pick all correct answers",
                    Type = QuestionType.MultipleMcq,
                    Marks = 2,
                    OrderIndex = 1,
                    Options = new List<QuestionOptionRequestDto>()
                },
                CancellationToken.None));

        Assert.Contains("Multiple choice questions must define at least two options.", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateQuestionAsync_SubjectiveTypeWithoutOptions_SucceedsAndClearsOptions()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await CreateQuizAsync(service, course.Id, 10);
        var question = await service.CreateQuestionAsync("teacher-1", quiz.Id, BuildMcq(1, 5), CancellationToken.None);

        var updated = await service.UpdateQuestionAsync(
            "teacher-1",
            quiz.Id,
            question.Id,
            new UpdateQuestionDto
            {
                Text = "Explain the concept.",
                Type = QuestionType.Essay,
                Marks = 5,
                OrderIndex = 1
            },
            CancellationToken.None);

        Assert.Equal(QuestionType.Essay, updated.Type);
        Assert.Empty(updated.Options);
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

        Assert.Contains(validationResults, result =>
            result.ErrorMessage != null &&
            result.ErrorMessage.Contains("Essay questions must not define options.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateQuestionDto_ShortAnswer_WithoutOptions_PassesValidation()
    {
        var dto = new UpdateQuestionDto
        {
            Text = "Provide a short answer.",
            Type = QuestionType.ShortAnswer,
            Marks = 2,
            OrderIndex = 1
        };

        var validationResults = Validate(dto);

        Assert.Empty(validationResults);
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

    private static Task<QuizResponseDto> CreateQuizAsync(
        QuizService service,
        Guid courseId,
        decimal totalMarks) =>
        service.CreateQuizAsync(
            "teacher-1",
            new CreateQuizDto
            {
                CourseId = courseId,
                Title = "Validation Quiz",
                DurationMinutes = 10,
                StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
                EndTimeUtc = DateTime.UtcNow.AddHours(1),
                TotalMarks = totalMarks,
                RandomizeQuestions = false,
                AllowMultipleAttempts = false,
                IsPublished = true,
                AreResultsPublished = true
            },
            CancellationToken.None);

    private static List<ValidationResult> Validate(object dto)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        Validator.TryValidateObject(dto, context, validationResults, true);
        return validationResults;
    }
}
