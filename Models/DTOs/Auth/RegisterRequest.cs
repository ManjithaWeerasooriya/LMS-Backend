namespace LMS_Backend.Models.DTOs.Auth;

public class RegisterRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // "Student" | "Teacher" | "Admin"
    public required string Role { get; set; }
}