using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LMS_Backend.Services;

public class ProfileImageService : IProfileImageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly UserManager<User> _userManager;
    private readonly IAzureStorageService _azureStorageService;
    private readonly ILogger<ProfileImageService> _logger;

    public ProfileImageService(
        UserManager<User> userManager,
        IAzureStorageService azureStorageService,
        ILogger<ProfileImageService> logger)
    {
        _userManager = userManager;
        _azureStorageService = azureStorageService;
        _logger = logger;
    }

    public async Task<User> UploadProfileImageAsync(
        string userId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidateFile(file);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new InvalidOperationException("Authenticated user was not found.");
        }

        var normalizedContentType = NormalizeContentType(file.ContentType);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var previousBlobName = user.ProfileImageBlobName;

        var uploadResult = await _azureStorageService.UploadProfileImageAsync(
            file,
            blobName,
            normalizedContentType);

        user.ProfileImageUrl = uploadResult.FileUrl;
        user.ProfileImageBlobName = uploadResult.BlobName;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await CleanupFailedUploadAsync(uploadResult.BlobName);

            var errors = string.Join(", ", updateResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to save the profile image: {errors}");
        }

        if (!string.IsNullOrWhiteSpace(previousBlobName) &&
            !string.Equals(previousBlobName, uploadResult.BlobName, StringComparison.Ordinal))
        {
            await DeletePreviousBlobAsync(previousBlobName);
        }

        return user;
    }

    private static void ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("No file uploaded.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("File too large. Maximum allowed size is 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Only JPG and PNG images are allowed.");
        }

        var contentType = NormalizeContentType(file.ContentType);
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException("Only image/jpeg and image/png content types are allowed.");
        }
    }

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "application/octet-stream";
        }

        var normalized = contentType
            .Split(';', 2)[0]
            .Trim()
            .ToLowerInvariant();

        return normalized switch
        {
            "image/jpg" => "image/jpeg",
            _ => normalized
        };
    }

    private async Task CleanupFailedUploadAsync(string blobName)
    {
        try
        {
            await _azureStorageService.DeleteProfileImageIfExistsAsync(blobName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up profile image blob {BlobName} after a database update failure.", blobName);
        }
    }

    private async Task DeletePreviousBlobAsync(string blobName)
    {
        try
        {
            await _azureStorageService.DeleteProfileImageIfExistsAsync(blobName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete previous profile image blob {BlobName}.", blobName);
        }
    }
}
