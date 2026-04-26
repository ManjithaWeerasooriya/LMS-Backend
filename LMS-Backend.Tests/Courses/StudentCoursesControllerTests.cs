using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LMS_Backend.Tests.Courses;

public class StudentCoursesControllerTests
{
    [Fact]
    public async Task EnrollInCourse_WhenAlreadyEnrolled_ReturnsConflict()
    {
        var service = new Mock<ICourseService>();
        var courseId = Guid.NewGuid();
        service
            .Setup(s => s.EnrollStudentInCourseAsync(courseId, "student-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollmentResult
            {
                Success = false,
                ErrorCode = "AlreadyEnrolled",
                ErrorMessage = "Student is already enrolled in this course."
            });

        var controller = CreateController(service.Object, CreateUser("student-1"));

        var result = await controller.EnrollInCourse(courseId, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Student is already enrolled in this course.", GetMessage(conflict.Value));
    }

    [Fact]
    public async Task EnrollInCourse_WhenCourseDoesNotExist_ReturnsNotFound()
    {
        var service = new Mock<ICourseService>();
        var courseId = Guid.NewGuid();
        service
            .Setup(s => s.EnrollStudentInCourseAsync(courseId, "student-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollmentResult
            {
                Success = false,
                ErrorCode = "CourseNotFound",
                ErrorMessage = "Course not found."
            });

        var controller = CreateController(service.Object, CreateUser("student-1"));

        var result = await controller.EnrollInCourse(courseId, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Course not found.", GetMessage(notFound.Value));
    }

    private static StudentCoursesController CreateController(ICourseService service, ClaimsPrincipal? user)
    {
        return new StudentCoursesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
    }

    private static ClaimsPrincipal CreateUser(string userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, AppRoles.Student)
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static string? GetMessage(object? value)
    {
        return value?
            .GetType()
            .GetProperty("message")
            ?.GetValue(value) as string;
    }
}
