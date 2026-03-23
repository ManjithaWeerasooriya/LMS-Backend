using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Public;
using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services
{
    public class PublicService : IPublicService
    {
        private readonly ApplicationDBContext _context;
        private readonly UserManager<User> _userManager;

        public PublicService(ApplicationDBContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<PlatformStatsDto> GetPlatformStatsAsync()
        {
            var totalCourses = await _context.Courses
                .CountAsync(c => c.Status == CourseStatus.Active);

            var totalStudents = (await _userManager.GetUsersInRoleAsync("Student")).Count;
            var totalTeachers = (await _userManager.GetUsersInRoleAsync("Teacher")).Count;

            return new PlatformStatsDto
            {
                TotalCourses = totalCourses,
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers
            };
        }

        public async Task<List<PublicCourseListItemDto>> GetPublicCoursesAsync(string? search)
        {
            var query = _context.Courses
                .Where(c => c.Status == CourseStatus.Active)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                query = query.Where(c =>
                    c.Title.ToLower().Contains(term) ||
                    (c.Description != null && c.Description.ToLower().Contains(term)) ||
                    (c.Category != null && c.Category.ToLower().Contains(term)));
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new PublicCourseListItemDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Category = c.Category,
                    Price = c.Price,
                    DurationHours = c.DurationHours,
                    TeacherName = c.Teacher.UserName
                })
                .ToListAsync();
        }

        public async Task<PublicCourseDetailDto?> GetPublicCourseByIdAsync(Guid id)
        {
            return await _context.Courses
                .Where(c => c.Status == CourseStatus.Active && c.Id == id)
                .Select(c => new PublicCourseDetailDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Category = c.Category,
                    Price = c.Price,
                    DurationHours = c.DurationHours,
                    DifficultyLevel = c.DifficultyLevel,
                    Prerequisites = c.Prerequisites,
                    AverageRating = c.AverageRating,
                    TeacherName = c.Teacher.UserName
                })
                .FirstOrDefaultAsync();
        }
    }
}