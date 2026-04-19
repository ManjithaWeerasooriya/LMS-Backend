using System;

namespace LMS_Backend.Models.DTOs.Materials;

public class MaterialDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid CourseId { get; set; }
    public DateTime CreatedAt { get; set; }
}
