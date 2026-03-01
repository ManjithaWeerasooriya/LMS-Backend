namespace LMS_Backend.Models.DTOs.Admin;

public class UserListResponseDto
{
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public required List<UserListItemDto> Users { get; init; }
}
