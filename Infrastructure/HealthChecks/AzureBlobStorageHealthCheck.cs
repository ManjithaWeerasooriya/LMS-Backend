using Azure;
using Azure.Storage.Blobs;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LMS_Backend.Infrastructure.HealthChecks;

public sealed class AzureBlobStorageHealthCheck : IHealthCheck
{
    private readonly AzureStorageOptions _options;
    private readonly IWebHostEnvironment _environment;

    public AzureBlobStorageHealthCheck(
        IOptions<AzureStorageOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = CreateData();

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return HealthCheckResult.Unhealthy(
                "Azure Blob Storage connection string is not configured.",
                data: data);
        }

        try
        {
            var client = CreateBlobServiceClient(_options.ConnectionString);
            data["serviceUri"] = client.Uri.ToString();

            var properties = await client.GetPropertiesAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(properties.Value.DefaultServiceVersion))
            {
                data["defaultServiceVersion"] = properties.Value.DefaultServiceVersion;
            }

            return HealthCheckResult.Healthy("Azure Blob Storage is reachable.", data);
        }
        catch (RequestFailedException ex)
        {
            return HealthCheckResult.Unhealthy("Azure Blob Storage check failed.", ex, data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Azure Blob Storage configuration is invalid.",
                ex,
                data);
        }
    }

    private Dictionary<string, object> CreateData()
    {
        var defaultContainerName = string.IsNullOrWhiteSpace(_options.ContainerName)
            ? AzureStorageOptions.DefaultContainerName
            : _options.ContainerName;
        var profileImagesContainerName = string.IsNullOrWhiteSpace(_options.ProfileImagesContainerName)
            ? AzureStorageOptions.DefaultProfileImagesContainerName
            : _options.ProfileImagesContainerName;

        return new Dictionary<string, object>
        {
            ["environment"] = _environment.EnvironmentName,
            ["defaultContainer"] = defaultContainerName,
            ["profileImagesContainer"] = profileImagesContainerName
        };
    }

    private BlobServiceClient CreateBlobServiceClient(string connectionString)
    {
        if (_environment.IsDevelopment() &&
            connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02);
            return new BlobServiceClient(connectionString, options);
        }

        return new BlobServiceClient(connectionString);
    }
}
