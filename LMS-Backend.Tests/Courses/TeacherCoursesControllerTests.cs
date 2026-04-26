using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LMS_Backend.Tests.Courses;

public class TeacherCoursesControllerTests
{
    [Fact]
    public async Task GetCourse_WhenCourseDoesNotExist_ReturnsNotFound()
    {
        var service = new Mock<ICourseService>();
        var courseId = Guid.NewGuid();
        service
            .Setup(s => s.GetCourseDetailForTeacherAsync(courseId, "teacher-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseDetailDto?)null);

        var controller = CreateController(service.Object, CreateUser("teacher-1", AppRoles.Teacher));

        var result = await controller.GetCourse(courseId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateCourse_WithoutAuthenticatedTeacher_ReturnsUnauthorized()
    {
        var service = new Mock<ICourseService>();
        var controller = CreateController(service.Object, user: null);

        var result = await controller.CreateCourse(CreateRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        service.Verify(
            s => s.CreateCourseAsync(
                It.IsAny<string>(),
                It.IsAny<CreateCourseRequestDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateCourse_WithStudentRole_ReturnsForbid()
    {
        var service = new Mock<ICourseService>();
        var controller = CreateController(service.Object, CreateUser("student-1", AppRoles.Student));

        var result = await controller.CreateCourse(CreateRequest(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        service.Verify(
            s => s.CreateCourseAsync(
                It.IsAny<string>(),
                It.IsAny<CreateCourseRequestDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateCourse_WithTeacherRole_ReturnsCreatedForOwnedCourse()
    {
        var service = new Mock<ICourseService>();
        var courseId = Guid.NewGuid();
        service
            .Setup(s => s.CreateCourseAsync("teacher-1", It.IsAny<CreateCourseRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course
            {
                Id = courseId,
                TeacherId = "teacher-1",
                Title = "Algorithms"
            });

        var controller = CreateController(service.Object, CreateUser("teacher-1", AppRoles.Teacher));
        var request = CreateRequest();

        var result = await controller.CreateCourse(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TeacherCoursesController.GetMyCourses), created.ActionName);
        service.Verify(
            s => s.CreateCourseAsync(
                "teacher-1",
                It.Is<CreateCourseRequestDto>(dto => dto.Title == request.Title),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static TeacherCoursesController CreateController(ICourseService service, ClaimsPrincipal? user)
    {
        return new TeacherCoursesController(service)
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

    private static ClaimsPrincipal CreateUser(string userId, string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static CreateCourseRequestDto CreateRequest()
    {
        return new CreateCourseRequestDto
        {
            Title = "Algorithms",
            Category = "Computer Science",
            Description = "Foundations",
            DurationHours = 12,
            Price = 99,
            MaxStudents = 25,
            DifficultyLevel = "Intermediate",
            Prerequisites = "Basic programming",
            Status = "Active"
        };
    }
}
