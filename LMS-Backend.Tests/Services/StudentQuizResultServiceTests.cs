using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using LMS_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Tests.Services;

public class StudentQuizResultServiceTests
{
    [Fact]
    public async Task GetStudentQuizResultAsync_WhenResultsPublished_ReturnsLatestSubmittedAttempt()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext, "student-1");
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Published Quiz",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-10),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = true,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var question = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "2 + 2 = ?",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "4", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "5", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var firstAttempt = await setupService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        await setupService.SubmitQuizAttemptAsync("student-1", firstAttempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = new List<Guid> { question.Options.Single(o => o.IsCorrect).Id }
                }
            }
        }, CancellationToken.None);

        var secondAttempt = await setupService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        await setupService.SubmitQuizAttemptAsync("student-1", secondAttempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = new List<Guid> { question.Options.Single(o => !o.IsCorrect).Id }
                }
            }
        }, CancellationToken.None);

        await using var verificationContext = CreateDbContext(databaseName);
        var verificationService = new QuizService(verificationContext);

        var result = await verificationService.GetStudentQuizResultAsync("student-1", quiz.Id, CancellationToken.None);

        Assert.Equal(secondAttempt.AttemptId, result.AttemptId);
        Assert.True(result.AreResultsPublished);
        Assert.Equal(0m, result.AwardedMarks);
        Assert.Equal(0m, result.Percentage);
        Assert.Single(result.QuestionResults);
        Assert.Equal(0m, result.QuestionResults[0].AwardedMarks);
    }

    [Fact]
    public async Task GetStudentQuizResultAsync_WhenResultsUnpublished_HidesMarks()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext, "student-1");
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Hidden Results Quiz",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-10),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = false
        }, CancellationToken.None);

        var question = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
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
        }, CancellationToken.None);

        var attempt = await setupService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        await setupService.SubmitQuizAttemptAsync("student-1", attempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = new List<Guid> { question.Options.Single(o => o.IsCorrect).Id }
                }
            }
        }, CancellationToken.None);

        await using var verificationContext = CreateDbContext(databaseName);
        var verificationService = new QuizService(verificationContext);

        var result = await verificationService.GetStudentQuizResultAsync("student-1", quiz.Id, CancellationToken.None);

        Assert.False(result.AreResultsPublished);
        Assert.Equal(QuizAttemptStatus.Graded, result.Status);
        Assert.Null(result.AwardedMarks);
        Assert.Null(result.Percentage);
        Assert.Single(result.QuestionResults);
        Assert.Null(result.QuestionResults[0].AwardedMarks);
        Assert.Null(result.QuestionResults[0].Feedback);
    }

    [Fact]
    public async Task GetStudentQuizResultAsync_WhenStudentRequestsAnotherStudentsResult_ReturnsNotFound()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext, "student-1", "student-2");
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-2" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Student Isolation Quiz",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-10),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var question = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "Largest planet?",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "Jupiter", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "Mars", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var otherStudentAttempt = await setupService.StartQuizAttemptAsync("student-2", quiz.Id, CancellationToken.None);
        await setupService.SubmitQuizAttemptAsync("student-2", otherStudentAttempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = new List<Guid> { question.Options.Single(o => o.IsCorrect).Id }
                }
            }
        }, CancellationToken.None);

        await using var verificationContext = CreateDbContext(databaseName);
        var verificationService = new QuizService(verificationContext);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            verificationService.GetStudentQuizResultAsync("student-1", quiz.Id, CancellationToken.None));

        Assert.Equal("No submitted attempt found for this quiz.", exception.Message);
    }

    [Fact]
    public async Task GetStudentQuizResultAsync_WhenStudentIsNoLongerEnrolled_ReturnsForbidden()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext, "student-1");
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Enrollment Check Quiz",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-10),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var question = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "Water freezes at?",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "0C", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "10C", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var attempt = await setupService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        await setupService.SubmitQuizAttemptAsync("student-1", attempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = new List<Guid> { question.Options.Single(o => o.IsCorrect).Id }
                }
            }
        }, CancellationToken.None);

        var enrollment = setupContext.CourseEnrollments.Single(e => e.CourseId == course.Id && e.StudentId == "student-1");
        setupContext.CourseEnrollments.Remove(enrollment);
        await setupContext.SaveChangesAsync();

        await using var verificationContext = CreateDbContext(databaseName);
        var verificationService = new QuizService(verificationContext);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            verificationService.GetStudentQuizResultAsync("student-1", quiz.Id, CancellationToken.None));

        Assert.Equal("You must be enrolled in the course to access this quiz result.", exception.Message);
    }

    [Fact]
    public async Task GetStudentQuizResultAsync_WhenManualGradingPending_ReturnsCurrentStatusAndAvailableMarks()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext, "student-1");
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Mixed Quiz",
            DurationMinutes = 30,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-10),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 10,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var mcq = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "2 + 3 = ?",
            Type = QuestionType.SingleMcq,
            Marks = 4,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "5", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "6", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var essay = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "Explain encapsulation.",
            Type = QuestionType.Essay,
            Marks = 6,
            OrderIndex = 2
        }, CancellationToken.None);

        var attempt = await setupService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        await setupService.SubmitQuizAttemptAsync("student-1", attempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = mcq.Id,
                    SelectedOptionIds = new List<Guid> { mcq.Options.Single(o => o.IsCorrect).Id }
                },
                new()
                {
                    QuestionId = essay.Id,
                    AnswerText = "It keeps state private behind methods."
                }
            }
        }, CancellationToken.None);

        await using var verificationContext = CreateDbContext(databaseName);
        var verificationService = new QuizService(verificationContext);

        var result = await verificationService.GetStudentQuizResultAsync("student-1", quiz.Id, CancellationToken.None);

        Assert.True(result.AreResultsPublished);
        Assert.Equal(QuizAttemptStatus.PendingReview, result.Status);
        Assert.Equal(4m, result.AwardedMarks);
        Assert.Equal(40m, result.Percentage);
        Assert.Equal(2, result.QuestionResults.Count);

        var objectiveResult = result.QuestionResults.Single(q => q.QuestionId == mcq.Id);
        Assert.Equal(StudentAnswerReviewStatus.NotRequired, objectiveResult.ReviewStatus);
        Assert.Equal(4m, objectiveResult.AwardedMarks);

        var essayResult = result.QuestionResults.Single(q => q.QuestionId == essay.Id);
        Assert.Equal(StudentAnswerReviewStatus.PendingReview, essayResult.ReviewStatus);
        Assert.Equal(0m, essayResult.AwardedMarks);
        Assert.Null(essayResult.Feedback);
    }

    private static ApplicationDBContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDBContext(options);
    }

    private static Course CreateCourse(string teacherId) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Algorithms",
        TeacherId = teacherId,
        Status = CourseStatus.Active
    };

    private static User CreateUser(string id, string userName, string firstName, string lastName) => new()
    {
        Id = id,
        UserName = userName,
        Email = $"{userName}@example.com",
        FirstName = firstName,
        LastName = lastName
    };

    private static async Task SeedUsersAsync(ApplicationDBContext context, params string[] studentIds)
    {
        if (!context.Users.Any())
        {
            context.Users.Add(CreateUser("teacher-1", "teacher1", "Ada", "Lovelace"));
        }

        foreach (var studentId in studentIds.Distinct(StringComparer.Ordinal))
        {
            if (await context.Users.FindAsync(studentId) == null)
            {
                var suffix = studentId.Replace("-", string.Empty, StringComparison.Ordinal);
                context.Users.Add(CreateUser(studentId, suffix, "Student", suffix));
            }
        }

        await context.SaveChangesAsync();
    }
}
