using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using LMS_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LMS_Backend.Tests.Services;

public class QuizServiceTests
{
    private static ApplicationDBContext GetDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
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

    [Fact]
    public async Task CreateQuizAsync_Should_Create_Quiz_For_Owning_Teacher()
    {
        await using var context = GetDbContext();
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);

        var result = await service.CreateQuizAsync(
            "teacher-1",
            new CreateQuizDto
            {
                CourseId = course.Id,
                Title = "Midterm Quiz",
                Description = "Covers units 1-3",
                DurationMinutes = 45,
                StartTimeUtc = DateTime.UtcNow.AddHours(-1),
                EndTimeUtc = DateTime.UtcNow.AddHours(8),
                TotalMarks = 100,
                RandomizeQuestions = true,
                AllowMultipleAttempts = false,
                IsPublished = true,
                AreResultsPublished = false
            },
            CancellationToken.None);

        Assert.Equal(course.Id, result.CourseId);
        Assert.Equal("Midterm Quiz", result.Title);
        Assert.Equal(100, result.TotalMarks);
        Assert.True(result.RandomizeQuestions);
        Assert.True(result.IsPublished);
        Assert.False(result.AreResultsPublished);
    }

    [Fact]
    public async Task CreateQuizAsync_Should_Reject_NonOwner_Teacher()
    {
        await using var context = GetDbContext();
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateQuizAsync(
                "teacher-2",
                new CreateQuizDto
                {
                    CourseId = course.Id,
                    Title = "Unauthorized Quiz",
                    DurationMinutes = 30,
                    StartTimeUtc = DateTime.UtcNow.AddMinutes(-10),
                    EndTimeUtc = DateTime.UtcNow.AddHours(2),
                    TotalMarks = 20,
                    IsPublished = true
                },
                CancellationToken.None));

        Assert.Equal("You do not have access to manage quizzes for this course.", exception.Message);
    }

    [Fact]
    public async Task StartQuizAttemptAsync_Should_Reject_Student_Who_Is_Not_Enrolled()
    {
        await using var context = GetDbContext();
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);

        var quiz = new Quiz
        {
            CourseId = course.Id,
            Title = "Quiz 1",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddHours(-1),
            EndTimeUtc = DateTime.UtcNow.AddHours(2),
            TotalMarks = 10,
            IsPublished = true
        };

        context.Quizzes.Add(quiz);
        context.Questions.Add(new Question
        {
            QuizId = quiz.Id,
            Text = "2 + 2 = ?",
            Type = QuestionType.SingleMcq,
            Marks = 10,
            OrderIndex = 1,
            Options = new List<QuestionOption>
            {
                new() { Text = "4", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "5", IsCorrect = false, OrderIndex = 2 }
            }
        });

        await context.SaveChangesAsync();

        var service = new QuizService(context);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None));

        Assert.Equal("You must be enrolled in the course to access this quiz.", exception.Message);
    }

    [Fact]
    public async Task SubmitQuizAttemptAsync_Should_AutoGrade_Objective_Answers_And_Require_Manual_Review_For_Subjective()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var context = GetDbContext(databaseName);
        context.Users.AddRange(
            CreateUser("teacher-1", "teacher1", "Ada", "Lovelace"),
            CreateUser("student-1", "student1", "Grace", "Hopper"));

        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment
        {
            CourseId = course.Id,
            StudentId = "student-1"
        });

        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);

        var quiz = await service.CreateQuizAsync(
            "teacher-1",
            new CreateQuizDto
            {
                CourseId = course.Id,
                Title = "Assessment 1",
                DurationMinutes = 60,
                StartTimeUtc = DateTime.UtcNow.AddHours(-1),
                EndTimeUtc = DateTime.UtcNow.AddHours(3),
                TotalMarks = 10,
                RandomizeQuestions = false,
                AllowMultipleAttempts = false,
                IsPublished = true,
                AreResultsPublished = true
            },
            CancellationToken.None);

        var mcq = await service.CreateQuestionAsync(
            "teacher-1",
            quiz.Id,
            new CreateQuestionDto
            {
                Text = "The capital of France is?",
                Type = QuestionType.SingleMcq,
                Marks = 4,
                OrderIndex = 1,
                Options = new List<QuestionOptionRequestDto>
                {
                    new() { Text = "Paris", IsCorrect = true, OrderIndex = 1 },
                    new() { Text = "Rome", IsCorrect = false, OrderIndex = 2 }
                }
            },
            CancellationToken.None);

        var essay = await service.CreateQuestionAsync(
            "teacher-1",
            quiz.Id,
            new CreateQuestionDto
            {
                Text = "Explain clean architecture.",
                Type = QuestionType.Essay,
                Marks = 6,
                OrderIndex = 2
            },
            CancellationToken.None);

        await using var startContext = GetDbContext(databaseName);
        var startService = new QuizService(startContext);
        var attempt = await startService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, attempt.AttemptId);

        await using var submitContext = GetDbContext(databaseName);
        var submitService = new QuizService(submitContext);
        var submittedAttempt = await submitService.SubmitQuizAttemptAsync(
            "student-1",
            attempt.AttemptId,
            new SubmitQuizAttemptDto
            {
                Answers = new List<SubmitStudentAnswerDto>
                {
                    new()
                    {
                        QuestionId = mcq.Id,
                        SelectedOptionIds = new List<Guid>
                        {
                            mcq.Options.Single(o => o.IsCorrect).Id
                        }
                    },
                    new()
                    {
                        QuestionId = essay.Id,
                        AnswerText = "A layered approach that protects the domain."
                    }
                }
            },
            CancellationToken.None);

        Assert.Equal(QuizAttemptStatus.PendingReview, submittedAttempt.Status);
        Assert.Equal(4, submittedAttempt.Score);

        var essayAnswer = submittedAttempt.Answers.Single(a => a.QuestionId == essay.Id);
        Assert.Equal(StudentAnswerReviewStatus.PendingReview, essayAnswer.ReviewStatus);
        Assert.Equal(0, essayAnswer.AwardedMarks);

        await using var gradingContext = GetDbContext(databaseName);
        var gradingService = new QuizService(gradingContext);
        var gradedAttempt = await gradingService.GradeAnswerAsync(
            "teacher-1",
            quiz.Id,
            submittedAttempt.AttemptId,
            essayAnswer.AnswerId,
            new ManualGradeAnswerDto
            {
                AwardedMarks = 5.5m,
                TeacherFeedback = "Good coverage of boundaries and application layers."
            },
            CancellationToken.None);

        Assert.Equal(QuizAttemptStatus.Graded, gradedAttempt.Status);
        Assert.Equal(9.5m, gradedAttempt.Score);

        var gradedEssayAnswer = gradedAttempt.Answers.Single(a => a.AnswerId == essayAnswer.AnswerId);
        Assert.Equal(StudentAnswerReviewStatus.Reviewed, gradedEssayAnswer.ReviewStatus);
        Assert.Equal(5.5m, gradedEssayAnswer.AwardedMarks);
    }
}
