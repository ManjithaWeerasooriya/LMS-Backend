using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace LMS_Backend.Services;

public interface IProfileImageService
{
    Task<User> UploadProfileImageAsync(string userId, IFormFile file, CancellationToken cancellationToken);
}
