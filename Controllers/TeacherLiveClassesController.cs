using System.Security.Claims;
using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.LiveClasses;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/live-classes")]
[Authorize(Policy = AppPolicies.TeacherOnly)]
public class TeacherLiveClassesController : ControllerBase
{
    private readonly LiveClassService _liveClassService;

    public TeacherLiveClassesController(LiveClassService liveClassService)
    {
        _liveClassService = liveClassService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LiveClassListItemDto>>> GetUpcoming(
        CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var sessions = await _liveClassService.GetUpcomingForTeacherAsync(
            teacherId,
            cancellationToken);

        return Ok(sessions);
    }

    [HttpPost]
    public async Task<IActionResult> Schedule(
        [FromBody] ScheduleLiveClassRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var liveClass = await _liveClassService.ScheduleLiveClassAsync(
            teacherId,
            dto,
            cancellationToken);

        if (liveClass == null)
        {
            return BadRequest(new { message = "Invalid course for this teacher." });
        }

        return CreatedAtAction(nameof(GetUpcoming), new { id = liveClass.Id }, new { liveClass.Id });
    }
}
