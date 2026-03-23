using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers
{
    [ApiController]
    [Route("api/public")]
    public class PublicController : ControllerBase
    {
        private readonly IPublicService _publicService;

        public PublicController(IPublicService publicService)
        {
            _publicService = publicService;
        }

        // ✅ Stats
        [AllowAnonymous]
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _publicService.GetPlatformStatsAsync();
            return Ok(stats);
        }

        // ✅ Course list (with optional search)
        [AllowAnonymous]
        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses([FromQuery] string? search)
        {
            var courses = await _publicService.GetPublicCoursesAsync(search);
            return Ok(courses);
        }

        // ✅ Course detail (IMPORTANT: Guid, not int)
        [AllowAnonymous]
        [HttpGet("courses/{id:guid}")]
        public async Task<IActionResult> GetCourseById(Guid id)
        {
            var course = await _publicService.GetPublicCourseByIdAsync(id);

            if (course == null)
                return NotFound(new { message = "Course not found" });

            return Ok(course);
        }
    }
}