namespace LMS_Backend.Models.DTOs.Auth;

public class LogoutRequest
{
    public required string RefreshToken { get; set; }
    public required string DeviceId { get; set; }
}