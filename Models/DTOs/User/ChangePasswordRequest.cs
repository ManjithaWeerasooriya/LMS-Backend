using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.User;

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = default!;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = default!;
}