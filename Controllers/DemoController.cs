using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Data;

[ApiController]
[Route("api/v1/[controller]")]
public class DemoController : ControllerBase
{
    // 🔓 Public endpoint (no token required)
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public()
    {
        return Ok("This is a public endpoint. No token required.");
    }

    // 🔐 Any authenticated user
    [HttpGet("secure")]
    [Authorize]
    public IActionResult Secure()
    {
        return Ok("You are authenticated!");
    }

    // 🎓 Student only
    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public IActionResult StudentOnly()
    {
        return Ok("Hello Student 👋");
    }

    // 👩‍🏫 Teacher only
    [HttpGet("teacher")]
    [Authorize(Roles = "Teacher")]
    public IActionResult TeacherOnly()
    {
        return Ok("Hello Teacher 👩‍🏫");
    }

    // 🔎 View token claims (very useful for debugging)
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var status = User.FindFirstValue("status");

        return Ok(new
        {
            userId,
            email,
            username,
            role,
            status,
            allClaims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}