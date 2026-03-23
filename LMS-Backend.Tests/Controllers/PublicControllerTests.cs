using LMS_Backend.Controllers;
using LMS_Backend.Models.DTOs.Public;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LMS_Backend.Tests.Controllers
{
    public class PublicControllerTests
    {
        [Fact]
        public async Task GetCourses_Returns_Ok_With_Courses()
        {
            // Arrange
            var mockService = new Mock<IPublicService>();

            var courses = new List<PublicCourseListItemDto>
            {
                new PublicCourseListItemDto
                {
                    Id = Guid.NewGuid(),
                    Title = "English Basics",
                    Description = "Beginner course",
                    Category = "English",
                    Price = 100,
                    DurationHours = 10,
                    TeacherName = "teacher1"
                }
            };

            mockService
                .Setup(s => s.GetPublicCoursesAsync(null))
                .ReturnsAsync(courses);

            var controller = new PublicController(mockService.Object);

            // Act
            var result = await controller.GetCourses(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCourses = Assert.IsAssignableFrom<List<PublicCourseListItemDto>>(okResult.Value);
            Assert.Single(returnedCourses);
        }

        [Fact]
        public async Task GetCourseById_Returns_NotFound_When_Course_Does_Not_Exist()
        {
            // Arrange
            var mockService = new Mock<IPublicService>();
            var courseId = Guid.NewGuid();

            mockService
                .Setup(s => s.GetPublicCourseByIdAsync(courseId))
                .ReturnsAsync((PublicCourseDetailDto?)null);

            var controller = new PublicController(mockService.Object);

            // Act
            var result = await controller.GetCourseById(courseId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetCourseById_Returns_Ok_When_Course_Exists()
        {
            // Arrange
            var mockService = new Mock<IPublicService>();
            var courseId = Guid.NewGuid();

            var course = new PublicCourseDetailDto
            {
                Id = courseId,
                Title = "English Basics",
                Description = "Beginner course",
                Category = "English",
                Price = 100,
                DurationHours = 10,
                DifficultyLevel = "Beginner",
                Prerequisites = null,
                AverageRating = 4.5,
                TeacherName = "teacher1"
            };

            mockService
                .Setup(s => s.GetPublicCourseByIdAsync(courseId))
                .ReturnsAsync(course);

            var controller = new PublicController(mockService.Object);

            // Act
            var result = await controller.GetCourseById(courseId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCourse = Assert.IsType<PublicCourseDetailDto>(okResult.Value);
            Assert.Equal(courseId, returnedCourse.Id);
        }
    }
}