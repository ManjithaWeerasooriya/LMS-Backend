using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.User;
public class UpdateMyProfileRequest
{
    [StringLength(50, ErrorMessage = "First name is too long.")]
    public string? FirstName { get; init; }

    [StringLength(50, ErrorMessage = "Last name is too long.")]
    public string? LastName { get; init; }

    [StringLength(10, ErrorMessage = "Phone number is too long.")]
    public string? Phone { get; init; }
}