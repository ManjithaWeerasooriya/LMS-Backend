namespace LMS_Backend.Models.DTOs.Admin;

public sealed class AzureConnectionDiagnosticsResponseDto
{
    public AzureConnectionDiagnosticResultDto MySql { get; init; } = new();
    public AzureConnectionDiagnosticResultDto AzureBlobStorage { get; init; } = new();
    public AzureConnectionDiagnosticResultDto AzureCommunicationServices { get; init; } = new();
}

public sealed class AzureConnectionDiagnosticResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset CheckedAt { get; init; }
}
