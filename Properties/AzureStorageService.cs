using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LMS_Backend.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace LMS_Backend.Services;

public class AzureStorageService
{
    private readonly string _connectionString;
    private readonly string _containerName = "course-materials";

    public AzureStorageService(IConfiguration config)
    {
        _connectionString = config["AZURE_CONN"]
            ?? throw new Exception("AZURE_CONN missing");
    }

    public async Task<UploadFileResult> UploadFileAsync(IFormFile file)
    {
        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

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
}
