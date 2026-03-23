using LMS_Backend.Data;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LMS_Backend.Tests.Services
{
    public class PublicServiceTests
    {
        private static ApplicationDBContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDBContext(options);
        }

        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                store.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);
        }

        [Fact]
        public async Task GetPublicCoursesAsync_Returns_Only_Active_Courses()
        {
            // Arrange
            var context = CreateDbContext();

            var teacher = new User
            {
                Id = "teacher-1",
                UserName = "teacher1"
            };

            context.Users.Add(teacher);

            context.Courses.AddRange(
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Active Course 1",
                    Description = "Visible course",
                    Category = "English",
                    DurationHours = 10,
                    Price = 100,
                    MaxStudents = 20,
                    TeacherId = teacher.Id,
                    Teacher = teacher,
                    Status = CourseStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Draft Course",
                    Description = "Should not appear",
                    Category = "Math",
                    DurationHours = 8,
                    Price = 50,
                    MaxStudents = 15,
                    TeacherId = teacher.Id,
                    Teacher = teacher,
                    Status = CourseStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Archived Course",
                    Description = "Should not appear",
                    Category = "Science",
                    DurationHours = 12,
                    Price = 70,
                    MaxStudents = 10,
                    TeacherId = teacher.Id,
                    Teacher = teacher,
                    Status = CourseStatus.Archived,
                    CreatedAt = DateTime.UtcNow
                }
            );

            await context.SaveChangesAsync();

            var userManagerMock = CreateUserManagerMock();
            var service = new PublicService(context, userManagerMock.Object);

            // Act
            var result = await service.GetPublicCoursesAsync(null);

            // Assert
            Assert.Single(result);
            Assert.Equal("Active Course 1", result[0].Title);
        }

        [Fact]
        public async Task GetPublicCoursesAsync_Filters_By_Search()
        {
            // Arrange
            var context = CreateDbContext();

            var teacher = new User
            {
                Id = "teacher-1",
                UserName = "teacher1"
            };

            context.Users.Add(teacher);

            context.Courses.AddRange(
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "English Basics",
                    Description = "Beginner course",
                    Category = "English",
                    DurationHours = 10,
                    Price = 100,
                    MaxStudents = 20,
                    TeacherId = teacher.Id,
                    Teacher = teacher,
                    Status = CourseStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Physics Intro",
                    Description = "Science course",
                    Category = "Science",
                    DurationHours = 12,
                    Price = 80,
                    MaxStudents = 20,
                    TeacherId = teacher.Id,
                    Teacher = teacher,
                    Status = CourseStatus.Active,
                    CreatedAt = DateTime.UtcNow
                }
            );

            await context.SaveChangesAsync();

            var userManagerMock = CreateUserManagerMock();
            var service = new PublicService(context, userManagerMock.Object);

            // Act
            var result = await service.GetPublicCoursesAsync("english");

            // Assert
            Assert.Single(result);
            Assert.Equal("English Basics", result[0].Title);
        }

        [Fact]
        public async Task GetPublicCourseByIdAsync_Returns_Null_For_NonActive_Course()
        {
            // Arrange
            var context = CreateDbContext();

            var teacher = new User
            {
                Id = "teacher-1",
                UserName = "teacher1"
            };

            var draftCourseId = Guid.NewGuid();

            context.Users.Add(teacher);

            context.Courses.Add(
                new Course
                {
                    Id = draftCourseId,
                    Title = "Draft Course",
                    Description = "Hidden",
                    Category = "English",
                    DurationHours = 10,
                    Price = 100,
                    MaxStudents = 20,
                    TeacherId = teacher.Id,
                    Teacher = teacher,
                    Status = CourseStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                }
            );

            await context.SaveChangesAsync();

            var userManagerMock = CreateUserManagerMock();
            var service = new PublicService(context, userManagerMock.Object);

            // Act
            var result = await service.GetPublicCourseByIdAsync(draftCourseId);

            // Assert
            Assert.Null(result);
        }
    }
}