using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Materials;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LMS_Backend.Tests.Materials;

public class MaterialsControllerTests
{
    [Fact]
    public async Task Upload_InvalidFileType_ReturnsBadRequest()
    {
        var service = new Mock<IMaterialService>();
        var controller = CreateController(service.Object, CreateUser("teacher-1", "Teacher"));
        var file = CreateFormFile("malware.exe", 1024, "application/octet-stream");

        var result = await controller.Upload(file, "Unit 1", Guid.NewGuid());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid file type.", badRequest.Value);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_FileTooLarge_ReturnsBadRequest()
    {
        var service = new Mock<IMaterialService>();
        var controller = CreateController(service.Object, CreateUser("teacher-1", "Teacher"));
        var hugeFileLength = 60L * 1024 * 1024;
        var file = CreateFormFile("module.pdf", hugeFileLength, "application/pdf");

        var result = await controller.Upload(file, "Module", Guid.NewGuid());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("File too large. Maximum allowed size is 50 MB.", badRequest.Value);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_TeacherNotOwner_ReturnsForbid()
    {
        await using var context = CreateDbContext();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Distributed Systems",
            TeacherId = "teacher-1",
            Status = CourseStatus.Active
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = CreateController(CreateService(context), CreateUser("teacher-2", "Teacher"));
        var file = CreateFormFile("notes.pdf", 2048, "application/pdf");

        var result = await controller.Upload(file, "Week 01", course.Id);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<object?>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("You do not have access to manage materials for this course.", response.Message);
    }

    [Fact]
    public async Task Upload_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        var controller = CreateController(Mock.Of<IMaterialService>(), user: null);
        var file = CreateFormFile("notes.pdf", 2048, "application/pdf");

        var result = await controller.Upload(file, "Week 01", Guid.NewGuid());

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object?>>(unauthorized.Value);
        Assert.False(response.Success);
        Assert.Equal("Authentication is required.", response.Message);
    }

    [Fact]
    public async Task GetByCourse_EnrolledStudent_ReturnsPersistedMaterials()
    {
        await using var context = CreateDbContext();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Algorithms",
            TeacherId = "teacher-1",
            Status = CourseStatus.Active
        };
        context.Courses.Add(course);
        context.CourseEnrollments.Add(new CourseEnrollment
        {
            CourseId = course.Id,
            StudentId = "student-1"
        });
        var material = new Material
        {
            Title = "Lecture Slides",
            FileUrl = "https://cdn/files/lecture.pdf",
            BlobName = "lecture.pdf",
            ContentType = "application/pdf",
            MaterialType = "pdf",
            FileSize = 1024,
            CourseId = course.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Materials.Add(material);
        await context.SaveChangesAsync();

        var controller = CreateController(CreateService(context), CreateUser("student-1", "Student"));

        var result = await controller.GetByCourse(course.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var materials = Assert.IsAssignableFrom<IReadOnlyList<MaterialDto>>(okResult.Value);
        Assert.Single(materials);
        Assert.Equal("Lecture Slides", materials[0].Title);
    }

    [Fact]
    public async Task GetByCourse_NotEnrolledStudent_ReturnsForbid()
    {
        await using var context = CreateDbContext();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Networks",
            TeacherId = "teacher-1",
            Status = CourseStatus.Active
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = CreateController(CreateService(context), CreateUser("student-99", "Student"));

        var result = await controller.GetByCourse(course.Id);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<object?>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("You must be enrolled in the course to access its materials.", response.Message);
    }

    [Fact]
    public async Task Download_NotEnrolledStudent_ReturnsForbid()
    {
        await using var context = CreateDbContext();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Databases",
            TeacherId = "teacher-1",
            Status = CourseStatus.Active
        };
        context.Courses.Add(course);
        context.Materials.Add(new Material
        {
            Id = 42,
            Title = "Schema.pdf",
            FileUrl = "https://cdn/schema.pdf",
            BlobName = "schema.pdf",
            ContentType = "application/pdf",
            MaterialType = "pdf",
            FileSize = 1024,
            CourseId = course.Id,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(CreateService(context), CreateUser("student-99", "Student"));

        var result = await controller.Download(42);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<object?>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("You must be enrolled in the course to access this material.", response.Message);
    }

    private static MaterialsController CreateController(IMaterialService materialService, ClaimsPrincipal? user)
    {
        var controller = new MaterialsController(materialService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        return controller;
    }

    private static IMaterialService CreateService(ApplicationDBContext context)
    {
        return new MaterialService(context, Mock.Of<IAzureStorageService>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<MaterialService>.Instance);
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

    private static ApplicationDBContext CreateDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDBContext(options);
    }

    private static IFormFile CreateFormFile(string fileName, long length, string contentType)
    {
        var stream = new MemoryStream(new byte[1])
        {
            Position = 0
        };

        return new FormFile(stream, 0, length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
