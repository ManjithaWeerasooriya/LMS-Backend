namespace LMS_Backend.Services;

public sealed class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";
    public const string DefaultContainerName = "course-materials";
    public const string DefaultProfileImagesContainerName = "profile-images";

    public string? ConnectionString { get; set; }

    public string ContainerName { get; set; } = DefaultContainerName;

    public string ProfileImagesContainerName { get; set; } = DefaultProfileImagesContainerName;
}
