namespace LMS_Backend.Services;

public sealed class AzureCommunicationOptions
{
    public const string SectionName = "AzureCommunication";

    public string? ConnectionString { get; set; }

    public string? Endpoint { get; set; }

    public int AccessTokenLifetimeHours { get; set; } = 8;
}
