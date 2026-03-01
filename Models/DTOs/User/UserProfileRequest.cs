using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.User;

public class UserProfileRequest
{
    public string Id { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public UserStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}