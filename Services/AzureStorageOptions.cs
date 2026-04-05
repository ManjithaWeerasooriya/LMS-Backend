namespace LMS_Backend.Services;

public sealed class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";
    public const string DefaultContainerName = "course-materials";

    public string? ConnectionString { get; set; }

    public string ContainerName { get; set; } = DefaultContainerName;
}
