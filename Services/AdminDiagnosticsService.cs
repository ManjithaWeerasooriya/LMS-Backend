using LMS_Backend.Models.DTOs.Admin;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LMS_Backend.Services;

public sealed class AdminDiagnosticsService
{
    private const string DatabaseHealthCheckName = "database";
    private const string BlobStorageHealthCheckName = "azure_blob_storage";
    private const string CommunicationHealthCheckName = "azure_communication_services";

    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<AdminDiagnosticsService> _logger;

    public AdminDiagnosticsService(
        HealthCheckService healthCheckService,
        ILogger<AdminDiagnosticsService> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    public async Task<AzureConnectionDiagnosticsResponseDto> TestAzureConnectionsAsync(
        CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Name is DatabaseHealthCheckName
                or BlobStorageHealthCheckName
                or CommunicationHealthCheckName,
            cancellationToken);

        return new AzureConnectionDiagnosticsResponseDto
        {
            MySql = CreateDatabaseResult(report),
            AzureBlobStorage = CreateResult(
                report,
                BlobStorageHealthCheckName,
                "Azure Blob Storage"),
            AzureCommunicationServices = CreateResult(
                report,
                CommunicationHealthCheckName,
                "Azure Communication Services")
        };
    }

    private AzureConnectionDiagnosticResultDto CreateDatabaseResult(HealthReport report)
    {
        if (!report.Entries.TryGetValue(DatabaseHealthCheckName, out var entry))
        {
            return LogAndReturnMissingResult("MySQL", DatabaseHealthCheckName);
        }

        var provider = entry.Data.TryGetValue("provider", out var providerValue)
            ? providerValue?.ToString()
            : null;
        var providerSuffix = string.IsNullOrWhiteSpace(provider)
            ? string.Empty
            : $" using configured EF Core provider '{provider}'";
        var successMessage = $"Database connectivity succeeded{providerSuffix}.";
        var failureMessage = BuildFailureMessage(entry, $"Database connectivity failed{providerSuffix}.");

        return CreateResult("MySQL", entry, successMessage, failureMessage);
    }

    private AzureConnectionDiagnosticResultDto CreateResult(
        HealthReport report,
        string healthCheckName,
        string serviceName)
    {
        if (!report.Entries.TryGetValue(healthCheckName, out var entry))
        {
            return LogAndReturnMissingResult(serviceName, healthCheckName);
        }

        var successMessage = $"{serviceName} connectivity succeeded.";
        var failureMessage = BuildFailureMessage(entry, $"{serviceName} connectivity failed.");

        return CreateResult(serviceName, entry, successMessage, failureMessage);
    }

    private AzureConnectionDiagnosticResultDto CreateResult(
        string serviceName,
        HealthReportEntry entry,
        string successMessage,
        string failureMessage)
    {
        var isHealthy = entry.Status == HealthStatus.Healthy;
        var result = new AzureConnectionDiagnosticResultDto
        {
            Success = isHealthy,
            Message = isHealthy ? successMessage : failureMessage,
            CheckedAt = DateTimeOffset.UtcNow
        };

        if (!isHealthy)
        {
            _logger.LogWarning(
                entry.Exception,
                "Admin diagnostics check failed for {ServiceName}. Status: {Status}. Message: {Message}",
                serviceName,
                entry.Status,
                result.Message);
        }

        return result;
    }

    private AzureConnectionDiagnosticResultDto LogAndReturnMissingResult(
        string serviceName,
        string healthCheckName)
    {
        const string message = "Connectivity check is not registered.";

        _logger.LogError(
            "Admin diagnostics check is missing for {ServiceName}. HealthCheckName: {HealthCheckName}",
            serviceName,
            healthCheckName);

        return new AzureConnectionDiagnosticResultDto
        {
            Success = false,
            Message = message,
            CheckedAt = DateTimeOffset.UtcNow
        };
    }

    private static string BuildFailureMessage(HealthReportEntry entry, string fallbackMessage)
    {
        if (!string.IsNullOrWhiteSpace(entry.Description))
        {
            return entry.Exception is null
                ? entry.Description
                : $"{entry.Description} Error: {entry.Exception.Message}";
        }

        return entry.Exception is null
            ? fallbackMessage
            : $"{fallbackMessage} Error: {entry.Exception.Message}";
    }
}
