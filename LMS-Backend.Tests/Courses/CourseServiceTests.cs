using System.Linq;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Tests.Courses;

public class CourseServiceTests
{
    private static ApplicationDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDBContext(options);
    }

    private static User CreateTeacher(string id, string firstName, string lastName) =>
        new()
        {
            Id = id,
            UserName = $"{id}@example.com",
            Email = $"{id}@example.com",
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active
        };

    private static CreateCourseRequestDto CreateRequest(string? status = null) =>
        new()
        {
            Title = "  Advanced C#  ",
            Category = "  Programming  ",
            Description = "  Deep dive into .NET  ",
            DurationHours = 24,
            Price = 149.99m,
            MaxStudents = 30,
            DifficultyLevel = "  Advanced  ",
            Prerequisites = "  C# Basics  ",
            Status = status
        };

    [Fact]
    public async Task CreateCourseAsync_TrimsFields_AndDefaultsStatusToActive()
    {
        await using var dbContext = CreateDbContext();
        var service = new CourseService(dbContext);

        var course = await service.CreateCourseAsync("teacher-1", CreateRequest(), CancellationToken.None);

        Assert.Equal("teacher-1", course.TeacherId);
        Assert.Equal("Advanced C#", course.Title);
        Assert.Equal("Programming", course.Category);
        Assert.Equal("Deep dive into .NET", course.Description);
        Assert.Equal("Advanced", course.DifficultyLevel);
        Assert.Equal("C# Basics", course.Prerequisites);
        Assert.Equal(CourseStatus.Active, course.Status);
        Assert.True(course.CreatedAt > DateTime.UtcNow.AddMinutes(-1));

        var stored = await dbContext.Courses.SingleAsync();
        Assert.Equal(course.Id, stored.Id);
    }

    [Fact]
    public async Task CreateCourseAsync_UsesProvidedStatus_WhenValid()
    {
        await using var dbContext = CreateDbContext();
        var service = new CourseService(dbContext);

        var course = await service.CreateCourseAsync("teacher-1", CreateRequest("Draft"), CancellationToken.None);

        Assert.Equal(CourseStatus.Draft, course.Status);
    }

    [Fact]
    public async Task GetCoursesForTeacherAsync_ReturnsOnlyTeachersCourses_FilteredAndOrdered()
    {
        await using var dbContext = CreateDbContext();
        var teacher = CreateTeacher("teacher-1", "Ada", "Lovelace");
        var otherTeacher = CreateTeacher("teacher-2", "Grace", "Hopper");

        dbContext.Users.AddRange(teacher, otherTeacher);

        var olderCourse = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "C# Fundamentals",
            Category = "Programming",
            Price = 99,
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Enrollments =
            {
                new CourseEnrollment { StudentId = "student-1" },
                new CourseEnrollment { StudentId = "student-2" }
            }
        };

        var newerCourse = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "C# Web APIs",
            Category = "Programming",
            Price = 149,
            Status = CourseStatus.Draft,
            AverageRating = 4.8,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var otherTeachersCourse = new Course
        {
            TeacherId = otherTeacher.Id,
            Teacher = otherTeacher,
            Title = "Python Basics",
            Category = "Programming",
            Price = 49,
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Courses.AddRange(olderCourse, newerCourse, otherTeachersCourse);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var courses = await service.GetCoursesForTeacherAsync(teacher.Id, "C#", CancellationToken.None);

        Assert.Equal(2, courses.Count);
        Assert.Equal(newerCourse.Id, courses[0].Id);
        Assert.Equal(olderCourse.Id, courses[1].Id);
        Assert.All(courses, c => Assert.Equal("Ada Lovelace", c.InstructorName));
        Assert.Equal(0, courses[0].Students);
        Assert.Equal(2, courses[1].Students);
        Assert.Equal("Draft", courses[0].Status);
        Assert.Equal(4.8, courses[0].Rating);
    }

    [Fact]
    public async Task GetCourseDetailForTeacherAsync_ReturnsNull_WhenCourseIsNotOwnedByTeacher()
    {
        await using var dbContext = CreateDbContext();
        var course = new Course
        {
            TeacherId = "teacher-1",
            Title = "Architecture",
            Status = CourseStatus.Active
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var result = await service.GetCourseDetailForTeacherAsync(course.Id, "teacher-2", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCourseAsync_UpdatesCourseValues_AndSetsUpdatedAt()
    {
        await using var dbContext = CreateDbContext();
        var course = new Course
        {
            TeacherId = "teacher-1",
            Title = "Old Title",
            Category = "Old Category",
            Description = "Old Description",
            DurationHours = 10,
            Price = 25,
            MaxStudents = 15,
            DifficultyLevel = "Beginner",
            Prerequisites = "None",
            Status = CourseStatus.Active
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var updated = await service.UpdateCourseAsync(course.Id, "teacher-1", CreateRequest("Archived"), CancellationToken.None);

        Assert.True(updated);

        var stored = await dbContext.Courses.SingleAsync();
        Assert.Equal("Advanced C#", stored.Title);
        Assert.Equal("Programming", stored.Category);
        Assert.Equal("Deep dive into .NET", stored.Description);
        Assert.Equal(24, stored.DurationHours);
        Assert.Equal(149.99m, stored.Price);
        Assert.Equal(30, stored.MaxStudents);
        Assert.Equal("Advanced", stored.DifficultyLevel);
        Assert.Equal("C# Basics", stored.Prerequisites);
        Assert.Equal(CourseStatus.Archived, stored.Status);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCourseAsync_ReturnsFalse_WhenCourseIsNotOwnedByTeacher()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Courses.Add(new Course
        {
            TeacherId = "teacher-1",
            Title = "Secured Course",
            Status = CourseStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var course = await dbContext.Courses.SingleAsync();
        var service = new CourseService(dbContext);

        var updated = await service.UpdateCourseAsync(course.Id, "teacher-2", CreateRequest("Draft"), CancellationToken.None);

        Assert.False(updated);
        Assert.Equal("Secured Course", (await dbContext.Courses.SingleAsync()).Title);
    }

    [Fact]
    public async Task DeleteCourseAsync_RemovesOwnedCourse()
    {
        await using var dbContext = CreateDbContext();
        var course = new Course
        {
            TeacherId = "teacher-1",
            Title = "Disposable Course",
            Status = CourseStatus.Active
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var deleted = await service.DeleteCourseAsync(course.Id, "teacher-1", CancellationToken.None);

        Assert.True(deleted);
        Assert.Empty(dbContext.Courses);
    }

    [Fact]
    public async Task GetCoursesForAdminAsync_AppliesFiltersAndPagination()
    {
        await using var dbContext = CreateDbContext();
        var teacher = CreateTeacher("teacher-1", "Ada", "Lovelace");
        var otherTeacher = CreateTeacher("teacher-2", "Grace", "Hopper");

        dbContext.Users.AddRange(teacher, otherTeacher);

        var includedCourse = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "GraphQL Fundamentals",
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var excludedByStatus = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "GraphQL Advanced",
            Status = CourseStatus.Archived,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var excludedByTeacher = new Course
        {
            TeacherId = otherTeacher.Id,
            Teacher = otherTeacher,
            Title = "GraphQL For Ops",
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        dbContext.Courses.AddRange(includedCourse, excludedByStatus, excludedByTeacher);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var options = new CourseQueryOptions
        {
            PageNumber = 1,
            PageSize = 1,
            TeacherId = teacher.Id,
            Status = CourseStatus.Active,
            Search = "GraphQL"
        };

        var result = await service.GetCoursesForAdminAsync(options);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(includedCourse.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task DisableCourseAdminAsync_SetsStatusToArchived()
    {
        await using var dbContext = CreateDbContext();
        var course = new Course
        {
            TeacherId = "teacher-1",
            Title = "Security 101",
            Status = CourseStatus.Active
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var disabled = await service.DisableCourseAdminAsync(course.Id);

        Assert.True(disabled);
        var stored = await dbContext.Courses.SingleAsync();
        Assert.Equal(CourseStatus.Archived, stored.Status);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task DeleteCourseAdminAsync_RemovesCourseWithoutTeacherCheck()
    {
        await using var dbContext = CreateDbContext();
        var course = new Course
        {
            TeacherId = "teacher-1",
            Title = "Legacy Course",
            Status = CourseStatus.Active
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var deleted = await service.DeleteCourseAdminAsync(course.Id);

        Assert.True(deleted);
        Assert.Empty(dbContext.Courses);
    }
}
