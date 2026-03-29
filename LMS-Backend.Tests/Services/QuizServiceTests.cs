using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LMS_Backend.Tests.Services;

public class QuizServiceTests
{
    private static Course CreateCourse(string title) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        TeacherId = "teacher-1",
        Status = CourseStatus.Active
    };

    private static ApplicationDBContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDBContext(options);
    }

    [Fact]
    public async Task CreateQuizAsync_Should_Create_Quiz_When_Course_Exists()
    {
        // Arrange
        var context = GetDbContext();

        var course = CreateCourse("English Basics");

        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);

        var dto = new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Grammar Quiz",
            DurationMinutes = 30,
            TotalMarks = 100,
            PassingMarks = 40,
            IsPublished = true
        };

        // Act
        var result = await service.CreateQuizAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.CourseId, result.CourseId);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.DurationMinutes, result.DurationMinutes);
        Assert.Equal(dto.TotalMarks, result.TotalMarks);
        Assert.Equal(dto.PassingMarks, result.PassingMarks);
        Assert.True(result.IsPublished);

        Assert.Equal(1, await context.Quizzes.CountAsync());
    }

    [Fact]
    public async Task CreateQuizAsync_Should_Throw_When_Course_Does_Not_Exist()
    {
        // Arrange
        var context = GetDbContext();
        var service = new QuizService(context);

        var dto = new CreateQuizDto
        {
            CourseId = Guid.NewGuid(),
            Title = "Grammar Quiz",
            DurationMinutes = 30,
            TotalMarks = 100,
            PassingMarks = 40,
            IsPublished = true
        };

        // Act
        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateQuizAsync(dto));

        // Assert
        Assert.Equal("Course not found.", ex.Message);
    }

    [Fact]
    public async Task CreateQuizAsync_Should_Throw_When_PassingMarks_Greater_Than_TotalMarks()
    {
        // Arrange
        var context = GetDbContext();

        var course = CreateCourse("English Basics");

        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = new QuizService(context);

        var dto = new CreateQuizDto
        {
            CourseId = course.Id,
            Title = "Grammar Quiz",
            DurationMinutes = 30,
            TotalMarks = 50,
            PassingMarks = 60,
            IsPublished = true
        };

        // Act
        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateQuizAsync(dto));

        // Assert
        Assert.Equal("Passing marks cannot be greater than total marks.", ex.Message);
    }

    [Fact]
    public async Task GetQuizzesByCourseAsync_Should_Return_Only_Matching_Course_Quizzes()
    {
        // Arrange
        var context = GetDbContext();

        var course1 = CreateCourse("Course 1");
        var course2 = CreateCourse("Course 2");

        context.Courses.AddRange(course1, course2);

        context.Quizzes.AddRange(
            new Quiz
            {
                Id = Guid.NewGuid(),
                CourseId = course1.Id,
                Title = "Quiz A",
                DurationMinutes = 20,
                TotalMarks = 100,
                PassingMarks = 40,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            },
            new Quiz
            {
                Id = Guid.NewGuid(),
                CourseId = course1.Id,
                Title = "Quiz B",
                DurationMinutes = 25,
                TotalMarks = 100,
                PassingMarks = 50,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new Quiz
            {
                Id = Guid.NewGuid(),
                CourseId = course2.Id,
                Title = "Quiz C",
                DurationMinutes = 15,
                TotalMarks = 50,
                PassingMarks = 20,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();

        var service = new QuizService(context);

        // Act
        var result = await service.GetQuizzesByCourseAsync(course1.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, q => Assert.Equal(course1.Id, q.CourseId));
    }
}
