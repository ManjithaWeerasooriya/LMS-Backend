namespace LMS_Backend.Models.DTOs.Auth;

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}