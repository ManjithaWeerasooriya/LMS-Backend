using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LMS_Backend.Tests.Grading;

public class QuizSubmissionTests
{
    [Fact]
    public void SubmitStudentAnswerDto_SelectedOptionIdsMetadata_IsNotRequired()
    {
        var services = new ServiceCollection();
        services.AddControllers();

        using var provider = services.BuildServiceProvider();
        var metadataProvider = provider.GetRequiredService<IModelMetadataProvider>();
        var metadata = metadataProvider.GetMetadataForType(typeof(SubmitStudentAnswerDto));
        var selectedOptionIdsMetadata = metadata.Properties.Single(property =>
            property.PropertyName == nameof(SubmitStudentAnswerDto.SelectedOptionIds));

        Assert.False(selectedOptionIdsMetadata.IsRequired);
    }

    [Fact]
    public async Task SubmitQuizAttempt_EssayOnly_ChangesStatusFromInProgress_AndPersistsSubmittedAt()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Essay Only",
            DurationMinutes = 30,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 10,
            RandomizeQuestions = false,
            AllowMultipleAttempts = true,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var questionIds = new List<Guid>();
        for (var orderIndex = 1; orderIndex <= 5; orderIndex++)
        {
            var question = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
            {
                Text = $"Essay question {orderIndex}",
                Type = QuestionType.Essay,
                Marks = 2,
                OrderIndex = orderIndex
            }, CancellationToken.None);

            questionIds.Add(question.Id);
        }

        await using var startContext = CreateDbContext(databaseName);
        var startService = new QuizService(startContext);
        var startedAttempt = await startService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);

        await using var submitContext = CreateDbContext(databaseName);
        var submitService = new QuizService(submitContext);
        var submittedAttempt = await submitService.SubmitQuizAttemptAsync("student-1", startedAttempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = questionIds
                .Select((questionId, index) => new SubmitStudentAnswerDto
                {
                    QuestionId = questionId,
                    AnswerText = $"Essay answer {index + 1}"
                })
                .ToList()
        }, CancellationToken.None);

        Assert.NotEqual(QuizAttemptStatus.InProgress, submittedAttempt.Status);
        Assert.Equal(QuizAttemptStatus.PendingReview, submittedAttempt.Status);
        Assert.All(submittedAttempt.Answers, answer => Assert.Equal(StudentAnswerReviewStatus.PendingReview, answer.ReviewStatus));
        Assert.NotNull(submittedAttempt.SubmittedAt);

        await using var verificationContext = CreateDbContext(databaseName);
        var persistedAttempt = await verificationContext.QuizAttempts.FindAsync(startedAttempt.AttemptId);
        Assert.NotNull(persistedAttempt);
        Assert.Equal(QuizAttemptStatus.PendingReview, persistedAttempt!.Status);
        Assert.NotNull(persistedAttempt.SubmittedAt);
    }

    [Fact]
    public async Task SubmitQuizAttempt_ShortAnswer_SucceedsWithoutSelectedOptionIds()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Short Answer Quiz",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var question = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "Define encapsulation.",
            Type = QuestionType.ShortAnswer,
            Marks = 5,
            OrderIndex = 1
        }, CancellationToken.None);

        await using var startContext = CreateDbContext(databaseName);
        var startService = new QuizService(startContext);
        var attempt = await startService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);

        await using var submitContext = CreateDbContext(databaseName);
        var submitService = new QuizService(submitContext);
        var submittedAttempt = await submitService.SubmitQuizAttemptAsync("student-1", attempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    AnswerText = "Bundling data and behavior together."
                }
            }
        }, CancellationToken.None);

        Assert.Equal(QuizAttemptStatus.PendingReview, submittedAttempt.Status);
        var answer = submittedAttempt.Answers.Single();
        Assert.Equal(question.Id, answer.QuestionId);
        Assert.Equal("Bundling data and behavior together.", answer.AnswerText);
        Assert.Empty(answer.SelectedOptionIds);
    }

    [Fact]
    public async Task SubmitQuizAttempt_McqWithoutSelectedOptionIds_FailsValidation()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "MCQ Quiz",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var question = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "2 + 2?",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "4", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "5", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        await using var startContext = CreateDbContext(databaseName);
        var startService = new QuizService(startContext);
        var attempt = await startService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);

        await using var submitContext = CreateDbContext(databaseName);
        var submitService = new QuizService(submitContext);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            submitService.SubmitQuizAttemptAsync("student-1", attempt.AttemptId, new SubmitQuizAttemptDto
            {
                Answers = new List<SubmitStudentAnswerDto>
                {
                    new()
                    {
                        QuestionId = question.Id
                    }
                }
            }, CancellationToken.None));

        Assert.Equal("Single choice questions require exactly one selected option.", exception.Message);
    }

    [Fact]
    public async Task SubmitQuizAttempt_AutoGradesObjectiveQuestions_AndKeepsEssayPending()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(databaseName);
        await SeedUsersAsync(context);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Auto Grading",
            Description = "MCQ + Essay",
            DurationMinutes = 30,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-20),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 15,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var mcq = await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "2 + 2?",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "4", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "5", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var trueFalse = await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "The sky is green.",
            Type = QuestionType.TrueFalse,
            Marks = 2,
            OrderIndex = 2,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "True", IsCorrect = false, OrderIndex = 1 },
                new() { Text = "False", IsCorrect = true, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var essay = await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "Explain dependency inversion.",
            Type = QuestionType.Essay,
            Marks = 8,
            OrderIndex = 3
        }, CancellationToken.None);

        var attempt = await service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        var submitted = await service.SubmitQuizAttemptAsync("student-1", attempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = mcq.Id,
                    SelectedOptionIds = new List<Guid> { mcq.Options.Single(option => option.IsCorrect).Id }
                },
                new()
                {
                    QuestionId = trueFalse.Id,
                    SelectedOptionIds = new List<Guid> { trueFalse.Options.Single(option => option.IsCorrect == false).Id }
                },
                new()
                {
                    QuestionId = essay.Id,
                    AnswerText = "High-level modules depend on abstractions"
                }
            }
        }, CancellationToken.None);

        Assert.Equal(QuizAttemptStatus.PendingReview, submitted.Status);
        Assert.Equal(5m, submitted.Answers.Single(a => a.QuestionId == mcq.Id).AwardedMarks);
        Assert.Equal(0m, submitted.Answers.Single(a => a.QuestionId == trueFalse.Id).AwardedMarks);
        var essayAnswer = submitted.Answers.Single(a => a.QuestionId == essay.Id);
        Assert.Equal(StudentAnswerReviewStatus.PendingReview, essayAnswer.ReviewStatus);
        Assert.Equal(5m, submitted.Score);
    }

    [Fact]
    public async Task GradeAnswer_AssignsMarksAndBlocksOverMarking()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(databaseName);
        await SeedUsersAsync(context);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Manual Grading",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 10,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var essay = await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "Describe SOLID",
            Type = QuestionType.Essay,
            Marks = 10,
            OrderIndex = 1
        }, CancellationToken.None);

        var start = await service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        var submission = await service.SubmitQuizAttemptAsync("student-1", start.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new() { QuestionId = essay.Id, AnswerText = "In-depth answer" }
            }
        }, CancellationToken.None);

        var essayAnswer = submission.Answers.Single();
        var graded = await service.GradeAnswerAsync("teacher-1", quiz.Id, submission.AttemptId, essayAnswer.AnswerId, new ManualGradeAnswerDto
        {
            AwardedMarks = 7.5m,
            TeacherFeedback = "Well reasoned"
        }, CancellationToken.None);

        Assert.Equal(QuizAttemptStatus.Graded, graded.Status);
        Assert.Equal(7.5m, graded.Score);
        var gradedAnswer = graded.Answers.Single(a => a.AnswerId == essayAnswer.AnswerId);
        Assert.Equal(StudentAnswerReviewStatus.Reviewed, gradedAnswer.ReviewStatus);
        Assert.Equal("Well reasoned", gradedAnswer.TeacherFeedback);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GradeAnswerAsync(
            "teacher-1",
            quiz.Id,
            graded.AttemptId,
            essayAnswer.AnswerId,
            new ManualGradeAnswerDto { AwardedMarks = 15m },
            CancellationToken.None));
    }

    [Fact]
    public async Task StartQuizAttempt_BeforeStartTime_ThrowsConflict()
    {
        await using var context = CreateDbContext();
        await SeedUsersAsync(context);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Future Quiz",
            DurationMinutes = 15,
            StartTimeUtc = DateTime.UtcNow.AddHours(2),
            EndTimeUtc = DateTime.UtcNow.AddHours(3),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = false
        }, CancellationToken.None);

        await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "Placeholder",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "A", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "B", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None));
        Assert.Equal("This quiz is not yet available.", exception.Message);
    }

    [Fact]
    public async Task StartQuizAttempt_DisallowsSecondAttemptWhenRestricted()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(databaseName);
        await SeedUsersAsync(context);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Single Attempt",
            DurationMinutes = 10,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var question = await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "2 + 3",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "5", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "6", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var attempt = await service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        await service.SubmitQuizAttemptAsync("student-1", attempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = new List<Guid> { question.Options.Single(option => option.IsCorrect).Id }
                }
            }
        }, CancellationToken.None);

        var conflict = await Assert.ThrowsAsync<ConflictException>(() => service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None));
        Assert.Equal("Multiple attempts are not allowed for this quiz.", conflict.Message);
    }

    [Fact]
    public async Task StartQuizAttempt_AllowsSecondAttemptAfterEssaySubmission_WhenMultipleAttemptsEnabled()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Essay Retries",
            DurationMinutes = 30,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = true,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var essayQuestion = await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "Explain the topic",
            Type = QuestionType.Essay,
            Marks = 5,
            OrderIndex = 1
        }, CancellationToken.None);

        await using var firstStartContext = CreateDbContext(databaseName);
        var firstStartService = new QuizService(firstStartContext);
        var firstAttempt = await firstStartService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);

        await using var submitContext = CreateDbContext(databaseName);
        var submitService = new QuizService(submitContext);
        var submittedAttempt = await submitService.SubmitQuizAttemptAsync("student-1", firstAttempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = essayQuestion.Id,
                    AnswerText = "First attempt answer"
                }
            }
        }, CancellationToken.None);

        Assert.Equal(QuizAttemptStatus.PendingReview, submittedAttempt.Status);

        await using var secondStartContext = CreateDbContext(databaseName);
        var secondStartService = new QuizService(secondStartContext);
        var secondAttempt = await secondStartService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);

        Assert.Equal(2, secondAttempt.AttemptNumber);
        Assert.NotEqual(firstAttempt.AttemptId, secondAttempt.AttemptId);
    }

    [Fact]
    public async Task StartQuizAttempt_BlocksWhenExistingAttemptIsStillInProgress()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var setupContext = CreateDbContext(databaseName);
        await SeedUsersAsync(setupContext);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        setupContext.Courses.Add(course);
        await setupContext.SaveChangesAsync();

        var setupService = new QuizService(setupContext);
        var quiz = await setupService.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Active Attempt Guard",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = true,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        await setupService.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "Explain the concept",
            Type = QuestionType.Essay,
            Marks = 5,
            OrderIndex = 1
        }, CancellationToken.None);

        await using var firstStartContext = CreateDbContext(databaseName);
        var firstStartService = new QuizService(firstStartContext);
        await firstStartService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);

        await using var secondStartContext = CreateDbContext(databaseName);
        var secondStartService = new QuizService(secondStartContext);
        var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
            secondStartService.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None));

        Assert.Equal("You already have an in-progress attempt for this quiz.", conflict.Message);
    }

    [Fact]
    public async Task SubmitQuizAttempt_Twice_TriggersConflict()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(databaseName);
        await SeedUsersAsync(context);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Submission Guard",
            DurationMinutes = 20,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-10),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = true,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var question = await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "1 + 1",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "2", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "3", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var attempt = await service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        var submission = await service.SubmitQuizAttemptAsync("student-1", attempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = new List<Guid> { question.Options.Single(option => option.IsCorrect).Id }
                }
            }
        }, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.SubmitQuizAttemptAsync("student-1", submission.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = new List<Guid> { question.Options.Single(option => option.IsCorrect).Id }
                }
            }
        }, CancellationToken.None));
        Assert.Equal("Only in-progress attempts can be submitted.", exception.Message);
    }

    [Fact]
    public async Task SubmitQuizAttempt_AfterDeadline_MarksAttemptExpired()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(databaseName);
        await SeedUsersAsync(context);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Timer Quiz",
            DurationMinutes = 5,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-10),
            EndTimeUtc = DateTime.UtcNow.AddMinutes(10),
            TotalMarks = 5,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = true
        }, CancellationToken.None);

        var question = await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
        {
            Text = "1 + 0",
            Type = QuestionType.SingleMcq,
            Marks = 5,
            OrderIndex = 1,
            Options = new List<QuestionOptionRequestDto>
            {
                new() { Text = "1", IsCorrect = true, OrderIndex = 1 },
                new() { Text = "0", IsCorrect = false, OrderIndex = 2 }
            }
        }, CancellationToken.None);

        var attempt = await service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        var attemptRow = await context.QuizAttempts.FirstAsync(a => a.Id == attempt.AttemptId);
        attemptRow.DeadlineUtc = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.SubmitQuizAttemptAsync("student-1", attempt.AttemptId, new SubmitQuizAttemptDto
        {
            Answers = new List<SubmitStudentAnswerDto>
            {
                new()
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = new List<Guid> { question.Options.Single(option => option.IsCorrect).Id }
                }
            }
        }, CancellationToken.None));
        Assert.Equal("Quiz attempt has expired.", exception.Message);

        await using var verification = CreateDbContext(databaseName);
        var persistedAttempt = await verification.QuizAttempts.FindAsync(attempt.AttemptId);
        Assert.NotNull(persistedAttempt);
        Assert.Equal(QuizAttemptStatus.Expired, persistedAttempt.Status);
    }

    [Fact]
    public async Task StartQuizAttempt_WhenRandomizationDisabled_PreservesOrder()
    {
        await using var context = CreateDbContext();
        await SeedUsersAsync(context);
        var course = CreateCourse("teacher-1");
        course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);
        var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Ordering",
            DurationMinutes = 30,
            StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            TotalMarks = 9,
            RandomizeQuestions = false,
            AllowMultipleAttempts = false,
            IsPublished = true,
            AreResultsPublished = false
        }, CancellationToken.None);

        var order = new[] { 3, 1, 2 };
        var index = 0;
        foreach (var text in new[] { "C", "A", "B" })
        {
            await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
            {
                Text = text,
                Type = QuestionType.SingleMcq,
                Marks = 3,
                OrderIndex = order[index++],
                Options = new List<QuestionOptionRequestDto>
                {
                    new() { Text = "Yes", IsCorrect = true, OrderIndex = 1 },
                    new() { Text = "No", IsCorrect = false, OrderIndex = 2 }
                }
            }, CancellationToken.None);
        }

        var attempt = await service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
        var orderReturned = attempt.Quiz.Questions.Select(q => q.OrderIndex).ToList();
        Assert.Equal(new List<int> { 1, 2, 3 }, orderReturned);
    }

    [Fact]
    public async Task StartQuizAttempt_WhenRandomizationEnabled_ShufflesOrderAcrossAttempts()
    {
        var observedOrders = new HashSet<string>();

        for (var i = 0; i < 6; i++)
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateDbContext(dbName);
            await SeedUsersAsync(context);
            var course = CreateCourse("teacher-1");
            course.Enrollments.Add(new CourseEnrollment { CourseId = course.Id, StudentId = "student-1" });
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            var service = new QuizService(context);
            var quiz = await service.CreateQuizAsync("teacher-1", new CreateQuizDto
            {
                CourseId = course.Id,
                Title = "Random Quiz",
                DurationMinutes = 30,
                StartTimeUtc = DateTime.UtcNow.AddMinutes(-5),
                EndTimeUtc = DateTime.UtcNow.AddHours(1),
                TotalMarks = 12,
                RandomizeQuestions = true,
                AllowMultipleAttempts = true,
                IsPublished = true,
                AreResultsPublished = false
            }, CancellationToken.None);

            for (var orderIndex = 1; orderIndex <= 4; orderIndex++)
            {
                await service.CreateQuestionAsync("teacher-1", quiz.Id, new CreateQuestionDto
                {
                    Text = $"Question {orderIndex}",
                    Type = QuestionType.SingleMcq,
                    Marks = 3,
                    OrderIndex = orderIndex,
                    Options = new List<QuestionOptionRequestDto>
                    {
                        new() { Text = "Yes", IsCorrect = true, OrderIndex = 1 },
                        new() { Text = "No", IsCorrect = false, OrderIndex = 2 }
                    }
                }, CancellationToken.None);
            }

            var attempt = await service.StartQuizAttemptAsync("student-1", quiz.Id, CancellationToken.None);
            observedOrders.Add(string.Join(",", attempt.Quiz.Questions.Select(q => q.OrderIndex)));

            if (observedOrders.Count > 1)
            {
                break;
            }
        }

        Assert.True(observedOrders.Count > 1, "Expected at least two distinct question orderings when randomization is enabled.");
    }

    private static async Task SeedUsersAsync(ApplicationDBContext context)
    {
        if (!context.Users.Any())
        {
            context.Users.AddRange(
                CreateUser("teacher-1", "teacher1", "Ada", "Lovelace"),
                CreateUser("student-1", "student1", "Grace", "Hopper"));
            await context.SaveChangesAsync();
        }
    }

    private static ApplicationDBContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
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

    private static User CreateUser(string id, string userName, string firstName, string lastName) => new()
    {
        Id = id,
        UserName = userName,
        Email = $"{userName}@lms.test",
        FirstName = firstName,
        LastName = lastName
    };
}
