using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LMS_Backend.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LMS_Backend.Services;

public class AzureStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public AzureStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = string.IsNullOrWhiteSpace(options.Value.ContainerName)
            ? AzureStorageOptions.DefaultContainerName
            : options.Value.ContainerName;
    }

    public async Task<UploadFileResult> UploadFileAsync(IFormFile file)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"{DateTime.UtcNow.Ticks}-{file.FileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders
        {
            ContentType = file.ContentType ?? "application/octet-stream"
        });

        return new UploadFileResult
        {
            FileUrl = blobClient.Uri.ToString(),
            BlobName = blobName
        };
    }

    public async Task<(Stream Stream, string ContentType)> DownloadFileAsync(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
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
}
