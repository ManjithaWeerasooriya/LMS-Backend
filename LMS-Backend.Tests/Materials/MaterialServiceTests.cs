using LMS_Backend.Data;
using LMS_Backend.Models.DTOs;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LMS_Backend.Tests.Materials;

public class MaterialServiceTests
{
    [Fact]
    public async Task UploadTeacherMaterialAsync_SavesMaterialRecord_WhenUploadSucceeds()
    {
        await using var context = CreateDbContext();
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var storage = new Mock<IAzureStorageService>();
        storage
            .Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadFileResult
            {
                FileUrl = "https://cdn/materials/week-01.pdf",
                BlobName = "week-01.pdf"
            });

        var service = CreateService(context, storage.Object);
        var file = CreateFormFile("week-01.pdf", 1024, "application/pdf");

        var material = await service.UploadTeacherMaterialAsync(
            "teacher-1",
            course.Id,
            file,
            "Week 01",
            "application/pdf",
            "pdf",
            CancellationToken.None);

        Assert.Equal("Week 01", material.Title);
        Assert.Equal(course.Id, material.CourseId);

        var savedMaterial = await context.Materials.SingleAsync();
        Assert.Equal("week-01.pdf", savedMaterial.BlobName);
        Assert.Equal("https://cdn/materials/week-01.pdf", savedMaterial.FileUrl);
    }

    [Fact]
    public async Task UploadTeacherMaterialAsync_DeletesBlob_WhenDatabaseSaveFails()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var seedContext = CreateDbContext(databaseName);
        var course = CreateCourse("teacher-1");
        seedContext.Courses.Add(course);
        await seedContext.SaveChangesAsync();

        await using var failingContext = new FailingSaveChangesContext(databaseName);

        var storage = new Mock<IAzureStorageService>();
        storage
            .Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadFileResult
            {
                FileUrl = "https://cdn/materials/week-02.pdf",
                BlobName = "week-02.pdf"
            });

        var service = CreateService(failingContext, storage.Object);
        var file = CreateFormFile("week-02.pdf", 1024, "application/pdf");

        await Assert.ThrowsAsync<DbUpdateException>(() => service.UploadTeacherMaterialAsync(
            "teacher-1",
            course.Id,
            file,
            "Week 02",
            "application/pdf",
            "pdf",
            CancellationToken.None));

        storage.Verify(s => s.DeleteFileIfExistsAsync("week-02.pdf"), Times.Once);
    }

    [Fact]
    public async Task GetStudentMaterialsByCourseAsync_ReturnsMaterials_ForEnrolledStudent()
    {
        await using var context = CreateDbContext();
        var course = CreateCourse("teacher-1");

        context.Courses.Add(course);
        context.CourseEnrollments.Add(new CourseEnrollment
        {
            CourseId = course.Id,
            StudentId = "student-1"
        });
        context.Materials.Add(new Material
        {
            Title = "Week 01",
            FileUrl = "https://cdn/materials/week-01.pdf",
            BlobName = "week-01.pdf",
            ContentType = "application/pdf",
            MaterialType = "pdf",
            FileSize = 1024,
            CourseId = course.Id,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var materials = await service.GetStudentMaterialsByCourseAsync(
            "student-1",
            course.Id,
            CancellationToken.None);

        Assert.Single(materials);
        Assert.Equal("Week 01", materials[0].Title);
        Assert.Equal(course.Id, materials[0].CourseId);
    }

    [Fact]
    public async Task GetStudentMaterialsByCourseAsync_ThrowsForbidden_ForNonEnrolledStudent()
    {
        await using var context = CreateDbContext();
        var course = CreateCourse("teacher-1");
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetStudentMaterialsByCourseAsync(
            "student-2",
            course.Id,
            CancellationToken.None));

        Assert.Equal("You must be enrolled in the course to access its materials.", exception.Message);
    }

    [Fact]
    public async Task GetStudentMaterialByIdAsync_ThrowsForbidden_WhenMaterialBelongsToAnotherCourse()
    {
        await using var context = CreateDbContext();
        var enrolledCourse = CreateCourse("teacher-1");
        var otherCourse = CreateCourse("teacher-2");

        context.Courses.AddRange(enrolledCourse, otherCourse);
        context.CourseEnrollments.Add(new CourseEnrollment
        {
            CourseId = enrolledCourse.Id,
            StudentId = "student-3"
        });
        var foreignMaterial = new Material
        {
            Id = 55,
            Title = "Hidden Notes",
            FileUrl = "https://cdn/materials/hidden.pdf",
            BlobName = "hidden.pdf",
            ContentType = "application/pdf",
            MaterialType = "pdf",
            FileSize = 2048,
            CourseId = otherCourse.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Materials.Add(foreignMaterial);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetStudentMaterialByIdAsync(
            "student-3",
            foreignMaterial.Id,
            CancellationToken.None));

        Assert.Equal("You must be enrolled in the course to access this material.", exception.Message);
    }

    [Fact]
    public async Task GetStudentMaterialsByCourseAsync_ThrowsNotFound_WhenCourseDoesNotExist()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetStudentMaterialsByCourseAsync(
            "student-4",
            Guid.NewGuid(),
            CancellationToken.None));

        Assert.Equal("Course not found.", exception.Message);
    }

    [Fact]
    public async Task GetStudentMaterialByIdAsync_ThrowsNotFound_WhenMaterialDoesNotExist()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetStudentMaterialByIdAsync(
            "student-5",
            999,
            CancellationToken.None));

        Assert.Equal("Material not found.", exception.Message);
    }

    private static MaterialService CreateService(ApplicationDBContext context, IAzureStorageService? storage = null)
    {
        return new MaterialService(
            context,
            storage ?? Mock.Of<IAzureStorageService>(),
            NullLogger<MaterialService>.Instance);
    }

    private static ApplicationDBContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDBContext(options);
    }

    private static Course CreateCourse(string teacherId)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            Title = "Course",
            TeacherId = teacherId,
            Status = CourseStatus.Active
        };
    }

    private static IFormFile CreateFormFile(string fileName, long length, string contentType)
    {
        var stream = new MemoryStream(new byte[1]);
        return new FormFile(stream, 0, length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class FailingSaveChangesContext : ApplicationDBContext
    {
        public FailingSaveChangesContext(string? databaseName)
            : base(new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("Simulated database failure.", new Exception("Simulated database failure."));
        }
    }
}
