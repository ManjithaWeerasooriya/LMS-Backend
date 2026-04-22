using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LMS_Backend.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LMS_Backend.Services;

public class AzureStorageService
{
    private readonly string? _connectionString;
    private readonly string _defaultContainerName;
    private readonly string _profileImagesContainerName;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AzureStorageService> _logger;

    // Backward-compatible constructor for existing tests/code
    public AzureStorageService(
        IOptions<AzureStorageOptions> options,
        IWebHostEnvironment environment)
        : this(options, environment, NullLogger<AzureStorageService>.Instance)
    {
    }

    public AzureStorageService(
        IOptions<AzureStorageOptions> options,
        IWebHostEnvironment environment,
        ILogger<AzureStorageService> logger)
    {
        _connectionString = options.Value.ConnectionString;

        _defaultContainerName = string.IsNullOrWhiteSpace(options.Value.ContainerName)
            ? AzureStorageOptions.DefaultContainerName
            : options.Value.ContainerName.Trim().ToLowerInvariant();

        _profileImagesContainerName = string.IsNullOrWhiteSpace(options.Value.ProfileImagesContainerName)
            ? AzureStorageOptions.DefaultProfileImagesContainerName
            : options.Value.ProfileImagesContainerName.Trim().ToLowerInvariant();

        _environment = environment;
        _logger = logger ?? NullLogger<AzureStorageService>.Instance;
    }

    public async Task<UploadFileResult> UploadFileAsync(IFormFile file)
    {
        ValidateFile(file);

        var safeFileName = Path.GetFileName(file.FileName);
        var blobName = $"{DateTime.UtcNow.Ticks}-{safeFileName}";

        _logger.LogInformation(
            "Starting material upload. FileName={FileName}, ContentType={ContentType}, Length={Length}, Container={Container}, BlobName={BlobName}",
            safeFileName,
            file.ContentType,
            file.Length,
            _defaultContainerName,
            blobName);

        return await UploadToContainerAsync(
            file,
            _defaultContainerName,
            blobName,
            file.ContentType ?? "application/octet-stream");
    }

    public Task<UploadFileResult> UploadProfileImageAsync(
        IFormFile file,
        string blobName,
        string contentType)
    {
        ValidateFile(file);

        _logger.LogInformation(
            "Starting profile image upload. FileName={FileName}, ContentType={ContentType}, Length={Length}, Container={Container}, BlobName={BlobName}",
            file.FileName,
            contentType,
            file.Length,
            _profileImagesContainerName,
            blobName);

        return UploadToContainerAsync(file, _profileImagesContainerName, blobName, contentType);
    }

    public async Task DeleteProfileImageIfExistsAsync(string blobName)
    {
        try
        {
            var containerClient = await GetContainerClientAsync(_profileImagesContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            var result = await blobClient.DeleteIfExistsAsync();

            _logger.LogInformation(
                "DeleteProfileImageIfExists completed. BlobName={BlobName}, Deleted={Deleted}",
                blobName,
                result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed deleting profile image. BlobName={BlobName}, Container={Container}",
                blobName,
                _profileImagesContainerName);
            throw;
        }
    }

    public async Task<(Stream Stream, string ContentType)> DownloadFileAsync(string blobName)
    {
        try
        {
            var blobServiceClient = CreateBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(_defaultContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning(
                    "Download requested for missing blob. BlobName={BlobName}, Container={Container}",
                    blobName,
                    _defaultContainerName);

                throw new FileNotFoundException("Blob not found.");
            }

            var response = await blobClient.DownloadStreamingAsync();
            var contentType = response.Value.Details.ContentType;

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }

            _logger.LogInformation(
                "Download successful. BlobName={BlobName}, Container={Container}, ContentType={ContentType}",
                blobName,
                _defaultContainerName,
                contentType);

            return (response.Value.Content, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Download failed. BlobName={BlobName}, Container={Container}",
                blobName,
                _defaultContainerName);
            throw;
        }
    }

    private async Task<UploadFileResult> UploadToContainerAsync(
        IFormFile file,
        string containerName,
        string blobName,
        string contentType)
    {
        try
        {
            var containerClient = await GetContainerClientAsync(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();

            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(contentType)
                        ? "application/octet-stream"
                        : contentType
                }
            });

            _logger.LogInformation(
                "Upload successful. Container={Container}, BlobName={BlobName}, Url={Url}",
                containerName,
                blobName,
                blobClient.Uri.ToString());

            return new UploadFileResult
            {
                FileUrl = blobClient.Uri.ToString(),
                BlobName = blobName
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "Azure Blob request failed. Status={Status}, ErrorCode={ErrorCode}, Container={Container}, BlobName={BlobName}",
                ex.Status,
                ex.ErrorCode,
                containerName,
                blobName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Upload failed. Container={Container}, BlobName={BlobName}, FileName={FileName}",
                containerName,
                blobName,
                file.FileName);
            throw;
        }
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(string containerName)
    {
        try
        {
            var blobServiceClient = CreateBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync();

            _logger.LogInformation(
                "Ensured blob container exists. Container={Container}",
                containerName);

            if (_environment.IsDevelopment() &&
                string.Equals(containerName, _profileImagesContainerName, StringComparison.Ordinal))
            {
                await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);

                _logger.LogInformation(
                    "Set blob public access policy in Development. Container={Container}",
                    containerName);
            }

            return containerClient;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "Failed to initialize container. Status={Status}, ErrorCode={ErrorCode}, Container={Container}",
                ex.Status,
                ex.ErrorCode,
                containerName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while initializing container. Container={Container}",
                containerName);
            throw;
        }
    }

    private BlobServiceClient CreateBlobServiceClient()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogError("Blob storage connection string is missing.");
            throw new InvalidOperationException("Blob storage connection string is not configured.");
        }

        try
        {
            if (_environment.IsDevelopment() &&
                _connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
            {
                var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02);

                _logger.LogInformation("Creating BlobServiceClient for Azurite/development storage.");
                return new BlobServiceClient(_connectionString, options);
            }

            _logger.LogInformation("Creating BlobServiceClient for Azure Storage.");
            return new BlobServiceClient(_connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob storage configuration is invalid.");
            throw new InvalidOperationException("Blob storage configuration is invalid.", ex);
        }
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }
    }
}
