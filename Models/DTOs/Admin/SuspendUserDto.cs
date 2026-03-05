namespace LMS_Backend.Models.DTOs.Admin;

public class SuspendUserDto
{
    public required string UserId { get; init; }
    public string? Reason { get; init; }
}

