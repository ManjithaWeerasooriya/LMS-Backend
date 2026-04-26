using LMS_Backend.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace LMS_Backend.Services;

public interface IAzureStorageService
{
    Task<UploadFileResult> UploadFileAsync(IFormFile file);

    Task<UploadFileResult> UploadProfileImageAsync(
        IFormFile file,
        string blobName,
        string contentType);

    Task DeleteFileIfExistsAsync(string blobName);

    Task DeleteProfileImageIfExistsAsync(string blobName);

    Task<(Stream Stream, string ContentType)> DownloadFileAsync(string blobName);
}
