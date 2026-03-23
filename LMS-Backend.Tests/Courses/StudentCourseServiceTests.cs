using LMS_Backend.Data;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Tests.Courses;

public class StudentCourseServiceTests
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

    [Fact]
    public async Task EnrollStudentInCourseAsync_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = new CourseService(dbContext);

        var result = await service.EnrollStudentInCourseAsync(Guid.NewGuid(), "student-1", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("CourseNotFound", result.ErrorCode);
        Assert.Equal("Course not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task EnrollStudentInCourseAsync_ReturnsBadRequest_WhenCourseIsNotActive()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Courses.Add(new Course
        {
            TeacherId = "teacher-1",
            Title = "Hidden Course",
            Status = CourseStatus.Draft
        });
        await dbContext.SaveChangesAsync();

        var courseId = await dbContext.Courses.Select(c => c.Id).SingleAsync();
        var service = new CourseService(dbContext);

        var result = await service.EnrollStudentInCourseAsync(courseId, "student-1", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("CourseNotActive", result.ErrorCode);
        Assert.Equal("Course is not open for enrollment.", result.ErrorMessage);
    }

    [Fact]
    public async Task EnrollStudentInCourseAsync_ReturnsSuccessWithoutDuplicate_WhenStudentAlreadyEnrolled()
    {
        await using var dbContext = CreateDbContext();
        var course = new Course
        {
            TeacherId = "teacher-1",
            Title = "Active Course",
            Status = CourseStatus.Active,
            Enrollments =
            {
                new CourseEnrollment
                {
                    StudentId = "student-1",
                    ProgressPercent = 35
                }
            }
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var result = await service.EnrollStudentInCourseAsync(course.Id, "student-1", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(dbContext.CourseEnrollments);
    }

    [Fact]
    public async Task EnrollStudentInCourseAsync_ReturnsConflict_WhenCourseIsFull()
    {
        await using var dbContext = CreateDbContext();
        var course = new Course
        {
            TeacherId = "teacher-1",
            Title = "Full Course",
            Status = CourseStatus.Active,
            MaxStudents = 1,
            Enrollments =
            {
                new CourseEnrollment { StudentId = "student-1" }
            }
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var result = await service.EnrollStudentInCourseAsync(course.Id, "student-2", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("CourseFull", result.ErrorCode);
        Assert.Equal("Course has reached its maximum capacity.", result.ErrorMessage);
    }

    [Fact]
    public async Task EnrollStudentInCourseAsync_CreatesEnrollment_WithZeroProgress()
    {
        await using var dbContext = CreateDbContext();
        var course = new Course
        {
            TeacherId = "teacher-1",
            Title = "Open Course",
            Status = CourseStatus.Active,
            MaxStudents = 2
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var result = await service.EnrollStudentInCourseAsync(course.Id, "student-1", CancellationToken.None);

        Assert.True(result.Success);

        var enrollment = await dbContext.CourseEnrollments.SingleAsync();
        Assert.Equal(course.Id, enrollment.CourseId);
        Assert.Equal("student-1", enrollment.StudentId);
        Assert.Equal(0, enrollment.ProgressPercent);
        Assert.True(enrollment.EnrolledAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task GetCoursesForStudentAsync_ReturnsActiveCourses_WithEnrollmentFlagsAndSearch()
    {
        await using var dbContext = CreateDbContext();
        var teacher = CreateTeacher("teacher-1", "Linus", "Torvalds");
        dbContext.Users.Add(teacher);

        var enrolledCourse = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "Algorithms",
            Category = "Computer Science",
            Status = CourseStatus.Active,
            Price = 120,
            AverageRating = 4.7,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Enrollments =
            {
                new CourseEnrollment { StudentId = "student-1" },
                new CourseEnrollment { StudentId = "student-2" }
            }
        };

        var availableCourse = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "Algorithms Advanced",
            Category = "Computer Science",
            Status = CourseStatus.Active,
            Price = 180,
            CreatedAt = DateTime.UtcNow
        };

        var draftCourse = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "Algorithms Draft",
            Category = "Computer Science",
            Status = CourseStatus.Draft,
            CreatedAt = DateTime.UtcNow.AddDays(1)
        };

        dbContext.Courses.AddRange(enrolledCourse, availableCourse, draftCourse);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var courses = await service.GetCoursesForStudentAsync("student-1", "Algorithms", CancellationToken.None);

        Assert.Equal(2, courses.Count);
        Assert.Equal(availableCourse.Id, courses[0].Id);
        Assert.Equal(enrolledCourse.Id, courses[1].Id);
        Assert.False(courses[0].IsEnrolled);
        Assert.True(courses[1].IsEnrolled);
        Assert.Equal(2, courses[1].StudentsEnrolled);
        Assert.All(courses, c => Assert.Equal("Linus Torvalds", c.InstructorName));
    }

    [Fact]
    public async Task GetEnrolledCoursesForStudentAsync_ReturnsOnlyActiveEnrolledCourses()
    {
        await using var dbContext = CreateDbContext();
        var teacher = CreateTeacher("teacher-1", "Margaret", "Hamilton");
        dbContext.Users.Add(teacher);

        var enrolledActiveCourse = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "Operating Systems",
            Category = "Computer Science",
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Enrollments =
            {
                new CourseEnrollment { StudentId = "student-1" }
            }
        };

        var notEnrolledCourse = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "Databases",
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var enrolledArchivedCourse = new Course
        {
            TeacherId = teacher.Id,
            Teacher = teacher,
            Title = "Legacy Systems",
            Status = CourseStatus.Archived,
            CreatedAt = DateTime.UtcNow.AddDays(1),
            Enrollments =
            {
                new CourseEnrollment { StudentId = "student-1" }
            }
        };

        dbContext.Courses.AddRange(enrolledActiveCourse, notEnrolledCourse, enrolledArchivedCourse);
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext);
        var courses = await service.GetEnrolledCoursesForStudentAsync("student-1", CancellationToken.None);

        var course = Assert.Single(courses);
        Assert.Equal(enrolledActiveCourse.Id, course.Id);
        Assert.True(course.IsEnrolled);
        Assert.Equal("Margaret Hamilton", course.InstructorName);
    }
}
