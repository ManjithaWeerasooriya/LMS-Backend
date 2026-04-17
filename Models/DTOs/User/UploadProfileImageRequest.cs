using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LMS_Backend.Models.DTOs.User;

public sealed class UploadProfileImageRequest
{
    [Required]
    public IFormFile? File { get; init; }
}
