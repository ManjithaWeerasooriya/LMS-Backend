namespace LMS_Backend.Models.DTOs.Auth;

public class RefreshRequest
{
    public required string RefreshToken { get; set; }
    public required string DeviceId { get; set; }
}