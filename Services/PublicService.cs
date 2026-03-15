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
    }
}
