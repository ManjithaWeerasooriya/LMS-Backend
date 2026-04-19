using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Materials;
using LMS_Backend.Models.Exceptions;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LMS_Backend.Tests.Materials;

public class StudentMaterialsControllerTests
{
    [Fact]
    public async Task GetMaterialsByCourse_EnrolledStudent_ReturnsWrappedMaterials()
    {
        var service = new Mock<IMaterialService>();
        service
            .Setup(s => s.GetStudentMaterialsByCourseAsync("student-1", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MaterialDto>
            {
                new()
                {
                    Id = 10,
                    Title = "Week 01",
                    FileUrl = "https://cdn/materials/week-01.pdf",
                    BlobName = "week-01.pdf",
                    ContentType = "application/pdf",
                    MaterialType = "pdf",
                    FileSize = 2048,
                    CourseId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                }
            });

        var controller = CreateController(service.Object, CreateUser("student-1"));

        var result = await controller.GetMaterialsByCourse(Guid.NewGuid());

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<MaterialDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Materials retrieved successfully.", response.Message);
        Assert.Single(response.Data!);
    }

    [Fact]
    public async Task GetMaterialsByCourse_NotEnrolledStudent_ReturnsForbiddenResponse()
    {
        var service = new Mock<IMaterialService>();
        service
            .Setup(s => s.GetStudentMaterialsByCourseAsync("student-2", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("You must be enrolled in the course to access its materials."));

        var controller = CreateController(service.Object, CreateUser("student-2"));

        var result = await controller.GetMaterialsByCourse(Guid.NewGuid());

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<object?>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("You must be enrolled in the course to access its materials.", response.Message);
    }

    [Fact]
    public async Task GetMaterialsByCourse_MissingCourse_ReturnsNotFoundResponse()
    {
        var service = new Mock<IMaterialService>();
        service
            .Setup(s => s.GetStudentMaterialsByCourseAsync("student-3", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Course not found."));

        var controller = CreateController(service.Object, CreateUser("student-3"));

        var result = await controller.GetMaterialsByCourse(Guid.NewGuid());

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object?>>(notFound.Value);
        Assert.False(response.Success);
        Assert.Equal("Course not found.", response.Message);
    }

    [Fact]
    public async Task GetMaterialById_EnrolledStudent_ReturnsWrappedMaterial()
    {
        var service = new Mock<IMaterialService>();
        service
            .Setup(s => s.GetStudentMaterialByIdAsync("student-4", 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto
            {
                Id = 42,
                Title = "Slides",
                FileUrl = "https://cdn/materials/slides.pdf",
                BlobName = "slides.pdf",
                ContentType = "application/pdf",
                MaterialType = "pdf",
                FileSize = 4096,
                CourseId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            });

        var controller = CreateController(service.Object, CreateUser("student-4"));

        var result = await controller.GetMaterialById(42);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<MaterialDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Material retrieved successfully.", response.Message);
        Assert.Equal(42, response.Data!.Id);
    }

    [Fact]
    public async Task DownloadMaterial_EnrolledStudent_ReturnsFileStreamResult()
    {
        var service = new Mock<IMaterialService>();
        service
            .Setup(s => s.DownloadStudentMaterialAsync("student-5", 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDownloadResult
            {
                Stream = new MemoryStream(new byte[] { 1, 2, 3 }),
                ContentType = "application/pdf",
                FileName = "lecture-notes.pdf"
            });

        var controller = CreateController(service.Object, CreateUser("student-5"));

        var result = await controller.DownloadMaterial(7);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("lecture-notes.pdf", fileResult.FileDownloadName);
    }

    private static StudentMaterialsController CreateController(IMaterialService service, ClaimsPrincipal? user)
    {
        return new StudentMaterialsController(service)
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
            new Claim(ClaimTypes.Role, "Student")
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }
}
