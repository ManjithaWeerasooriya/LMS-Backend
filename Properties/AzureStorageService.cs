using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LMS_Backend.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LMS_Backend.Services;

public class AzureStorageService
{
    private readonly string? _connectionString;
    private readonly string _defaultContainerName;
    private readonly string _profileImagesContainerName;
    private readonly IWebHostEnvironment _environment;

    public AzureStorageService(
        IOptions<AzureStorageOptions> options,
        IWebHostEnvironment environment)
    {
        _connectionString = options.Value.ConnectionString;
        _defaultContainerName = string.IsNullOrWhiteSpace(options.Value.ContainerName)
            ? AzureStorageOptions.DefaultContainerName
            : options.Value.ContainerName;
        _profileImagesContainerName = string.IsNullOrWhiteSpace(options.Value.ProfileImagesContainerName)
            ? AzureStorageOptions.DefaultProfileImagesContainerName
            : options.Value.ProfileImagesContainerName;
        _environment = environment;
    }

    public async Task<UploadFileResult> UploadFileAsync(IFormFile file)
    {
        var blobName = $"{DateTime.UtcNow.Ticks}-{file.FileName}";
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
        return UploadToContainerAsync(file, _profileImagesContainerName, blobName, contentType);
    }

    public async Task DeleteProfileImageIfExistsAsync(string blobName)
    {
        var containerClient = await GetContainerClientAsync(_profileImagesContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<(Stream Stream, string ContentType)> DownloadFileAsync(string blobName)
    {
        var blobServiceClient = CreateBlobServiceClient();
        var containerClient = blobServiceClient.GetBlobContainerClient(_defaultContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
            throw new FileNotFoundException("Blob not found.");

        var response = await blobClient.DownloadStreamingAsync();

        var contentType = response.Value.Details.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = "application/octet-stream";
        }

        return (response.Value.Content, contentType);
    }

    private async Task<UploadFileResult> UploadToContainerAsync(
        IFormFile file,
        string containerName,
        string blobName,
        string contentType)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        });

        return new UploadFileResult
        {
            FileUrl = blobClient.Uri.ToString(),
            BlobName = blobName
        };
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(string containerName)
    {
        var blobServiceClient = CreateBlobServiceClient();
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        if (_environment.IsDevelopment() &&
            string.Equals(containerName, _profileImagesContainerName, StringComparison.Ordinal))
        {
            await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);
        }

        return containerClient;
    }

    private BlobServiceClient CreateBlobServiceClient()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Blob storage connection string is not configured.");
        }

        try
        {
            if (_environment.IsDevelopment() &&
                _connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
            {
                var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02);
                return new BlobServiceClient(_connectionString, options);
            }

            return new BlobServiceClient(_connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Blob storage configuration is invalid.", ex);
        }
    }
}
