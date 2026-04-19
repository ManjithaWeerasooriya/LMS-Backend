using LMS_Backend.Data;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace LMS_Backend.Tests.Materials;

public class MaterialServiceTests
{
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

    private static MaterialService CreateService(ApplicationDBContext context)
    {
        return new MaterialService(context, CreateStorageStub());
    }

    private static ApplicationDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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

    private static AzureStorageService CreateStorageStub()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(env => env.EnvironmentName).Returns("Development");

        return new AzureStorageService(
            Options.Create(new AzureStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            }),
            environment.Object);
    }
}
