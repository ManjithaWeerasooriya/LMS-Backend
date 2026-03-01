using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.Auth;

public sealed class ResetPasswordRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
