using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.DTOs.Auth;

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
