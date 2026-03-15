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

        [AllowAnonymous]
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _publicService.GetPlatformStatsAsync();
            return Ok(stats);
        }
    }
}