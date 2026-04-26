using LMS_Backend.Data;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Tests.ParameterizedXUnitTests;

// Parameterized tests for course enrollment scenarios using in-memory database
public class CourseEnrollmentParameterizedTests
{
    // Tests multiple enrollment outcomes (not found, inactive, full, already enrolled, success)
    [Theory]
    [InlineData(false, CourseStatus.Active, false, 0, 0, false, "CourseNotFound", 0)]
    [InlineData(true, CourseStatus.Draft, false, 0, 0, false, "CourseNotActive", 0)]
    [InlineData(true, CourseStatus.Archived, false, 0, 0, false, "CourseNotActive", 0)]
    [InlineData(true, CourseStatus.Active, true, 0, 1, false, "AlreadyEnrolled", 1)]
    [InlineData(true, CourseStatus.Active, false, 1, 1, false, "CourseFull", 1)]
    [InlineData(true, CourseStatus.Active, false, 2, 1, true, null, 2)]
    public async Task EnrollStudentInCourseAsync_ShouldReturnExpectedResult(
        bool createCourse,
        CourseStatus courseStatus,
        bool alreadyEnrolled,
        int maxStudents,
        int existingEnrollmentCount,
        bool expectedSuccess,
        string? expectedErrorCode,
        int expectedEnrollmentCount)
    {
        // Arrange
        // Setup in-memory database and test course data based on input parameters
        await using var dbContext = CreateDbContext();
        var courseId = Guid.NewGuid();

        if (createCourse)
        {
            var course = new Course
            {
                Id = courseId,
                TeacherId = "teacher-1",
                Title = "Parameterized Course",
                Status = courseStatus,
                MaxStudents = maxStudents
            };

            for (var index = 0; index < existingEnrollmentCount; index++)
            {
                var studentId = alreadyEnrolled && index == 0
                    ? "student-1"
                    : $"existing-student-{index + 1}";

                course.Enrollments.Add(new CourseEnrollment
                {
                    CourseId = courseId,
                    StudentId = studentId,
                    ProgressPercent = 10
                });
            }

            dbContext.Courses.Add(course);
            await dbContext.SaveChangesAsync();
        }

        var service = new CourseService(dbContext);

        // Act
        var result = await service.EnrollStudentInCourseAsync(courseId, "student-1", CancellationToken.None);
        var enrollmentCount = await dbContext.CourseEnrollments.CountAsync();

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
        Assert.Equal(expectedEnrollmentCount, enrollmentCount);
    }

    private static ApplicationDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDBContext(options);
    }
}
