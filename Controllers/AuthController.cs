using LMS_Backend.Models.DTOs.Auth;
using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<User> _signInManager;

    public AuthController(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var role = req.Role.Trim();

        if (role != "Student" && role != "Teacher")
            return BadRequest(new { message = "Role must be Student or Teacher." });

        // Check email uniqueness
        var existing = await _userManager.FindByEmailAsync(req.Email);
        if (existing != null)
            return Conflict(new { message = "Email already exists." });

        var user = new User
        {
            UserName = req.Email,
            Email = req.Email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Status = role == "Teacher" ? UserStatus.Pending : UserStatus.Active,
            Phone = null,
            CreatedAt = DateTime.UtcNow
        };

        // Identity will hash the password into PasswordHash
        var createResult = await _userManager.CreateAsync(user, req.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed.",
                errors = createResult.Errors.Select(e => e.Description)
            });
        }

        // Ensure role exists, then assign
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));

        await _userManager.AddToRoleAsync(user, role);

        return Ok(new
        {
            message = role == "Teacher"
                ? "Registered. Waiting for admin approval."
                : "Registered successfully.",
            userId = user.Id,
            status = user.Status.ToString(),
            role
        });
    }

    // Placeholder login (NO JWT YET)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials." });

        // Block pending/suspended users (matches backlog tests)
        if (user.Status != UserStatus.Active)
            return Unauthorized(new { message = $"User is {user.Status}." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        // Later: generate JWT and return it.
        return Ok(new
        {
            message = "Login successful (JWT not implemented yet).",
            userId = user.Id
        });
    }
}