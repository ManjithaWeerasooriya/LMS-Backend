namespace LMS_Backend.Models.DTOs.Admin;

public class UserListItemDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Role { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
