namespace LMS_Backend.Models.DTOs.Auth;

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }

    // Client generates once and reuses (GUID string)
    public required string DeviceId { get; set; }
}