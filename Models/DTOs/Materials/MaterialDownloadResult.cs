using System.IO;

namespace LMS_Backend.Models.DTOs.Materials;

public class MaterialDownloadResult
{
    public Stream Stream { get; set; } = Stream.Null;
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = string.Empty;
}
