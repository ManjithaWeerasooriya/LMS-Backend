namespace LMS_Backend.Models.DTOs.Admin;

public class UserQueryParametersDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Role { get; set; }
    public string? Status { get; set; }
}
