using System.Security.Claims;
using LMS_Backend.Controllers;
using LMS_Backend.Data;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs;

namespace LMS_Backend.Tests.Materials;

public class MaterialsPositiveFlowTests
{
    [Fact]
    public async Task GetByCourse_TeacherOwner_ReturnsOrderedMaterials()
    {
        await using var context = CreateDbContext();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Cloud",
            TeacherId = "teacher-1",
            Status = CourseStatus.Active
        };

        context.Courses.Add(course);
        context.Materials.AddRange(
            new Material
            {
                Title = "Week 01",
                FileUrl = "https://cdn/1",
                BlobName = "1",
                ContentType = "application/pdf",
                MaterialType = "pdf",
                FileSize = 1024,
                CourseId = course.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new Material
            {
                Title = "Week 02",
                FileUrl = "https://cdn/2",
                BlobName = "2",
                ContentType = "application/pdf",
                MaterialType = "pdf",
                FileSize = 2048,
                CourseId = course.Id,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateStorageStub(), CreateUser("teacher-1", "Teacher"));
        var result = await controller.GetByCourse(course.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var materials = Assert.IsAssignableFrom<List<Material>>(ok.Value);
        Assert.Equal(2, materials.Count);
        Assert.Equal("Week 02", materials[0].Title);
    }

    [Fact]
    public async Task GetById_TeacherOwner_ReturnsMaterialMetadata()
    {
        await using var context = CreateDbContext();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Math",
            TeacherId = "teacher-1",
            Status = CourseStatus.Active
        };
        context.Courses.Add(course);
        var material = new Material
        {
            Title = "Lecture",
            FileUrl = "https://cdn/lecture.pdf",
            BlobName = "lecture.pdf",
            ContentType = "application/pdf",
            MaterialType = "pdf",
            FileSize = 512,
            CourseId = course.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Materials.Add(material);
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateStorageStub(), CreateUser("teacher-1", "Teacher"));
        var result = await controller.GetById(material.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<Material>(ok.Value);
        Assert.Equal(material.Title, returned.Title);
        Assert.Equal(material.FileUrl, returned.FileUrl);
    }

    [Fact]
    public async Task GetByCourse_TeacherFromAnotherCourse_IsForbidden()
    {
        await using var context = CreateDbContext();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Physics",
            TeacherId = "teacher-1",
            Status = CourseStatus.Active
        };
        context.Courses.Add(course);
        context.Materials.Add(new Material
        {
            Title = "Slides",
            FileUrl = "https://cdn/slides.pdf",
            BlobName = "slides.pdf",
            ContentType = "application/pdf",
            MaterialType = "pdf",
            FileSize = 1024,
            CourseId = course.Id,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateStorageStub(), CreateUser("teacher-2", "Teacher"));
        var result = await controller.GetByCourse(course.Id);

        Assert.IsType<ForbidResult>(result);
    }

    private static MaterialsController CreateController(ApplicationDBContext context, AzureStorageService storage, ClaimsPrincipal user)
    {
        return new MaterialsController(storage, context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
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

    private static ApplicationDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDBContext(options);
    }

    private static AzureStorageService CreateStorageStub()
    {
        return new AzureStorageService(
            new BlobServiceClient("UseDevelopmentStorage=true"),
            Options.Create(new AzureStorageOptions()));
    }
}
