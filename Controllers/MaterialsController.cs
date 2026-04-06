using System.Security.Claims;
using LMS_Backend.Data;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/materials")]
public class MaterialsController : ControllerBase
{
    private readonly AzureStorageService _azureStorageService;
    private readonly ApplicationDBContext _dbContext;

    private static readonly string[] AllowedExtensions =
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip",
        ".mp4", ".avi", ".mov", ".mkv"
    };

    private static readonly string[] AllowedContentTypes =
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/zip",
        "video/mp4",
        "video/x-msvideo",
        "video/quicktime",
        "video/x-matroska"
    };

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public MaterialsController(
        AzureStorageService azureStorageService,
        ApplicationDBContext dbContext)
    {
        _azureStorageService = azureStorageService;
        _dbContext = dbContext;
    }

    [HttpPost("upload")]
    [Authorize(Roles = "Teacher")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm(Name = "file")] IFormFile file,
        [FromForm] string? title,
        [FromForm] Guid courseId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (courseId == Guid.Empty)
            return BadRequest("Invalid courseId.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("File too large. Maximum allowed size is 50 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return BadRequest("Invalid file type.");

        var contentType = NormalizeContentType(file.ContentType);
        if (!IsAllowedContentType(contentType, extension))
            return BadRequest("Invalid content type.");

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var ownsCourse = await IsTeacherOwnerOfCourse(userId, courseId);
        if (!ownsCourse)
            return Forbid();

        var uploadResult = await _azureStorageService.UploadFileAsync(file);

        var finalTitle = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(file.FileName)
            : title;

        var material = new Material
        {
            Title = finalTitle,
            FileUrl = uploadResult.FileUrl,
            BlobName = uploadResult.BlobName,
            ContentType = contentType,
            MaterialType = GetMaterialType(contentType, file.FileName),
            FileSize = file.Length,
            CourseId = courseId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Materials.Add(material);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "File uploaded successfully",
            material.Id,
            material.Title,
            material.FileUrl,
            material.MaterialType,
            material.CourseId
        });
    }

    [HttpGet("course/{courseId:guid}")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> GetByCourse(Guid courseId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var allowed =
            (User.IsInRole("Teacher") && await IsTeacherOwnerOfCourse(userId, courseId)) ||
            (User.IsInRole("Student") && await IsStudentEnrolledInCourse(userId, courseId));

        if (!allowed)
            return Forbid();

        var materials = await _dbContext.Materials
            .Where(m => m.CourseId == courseId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return Ok(materials);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var material = await _dbContext.Materials.FirstOrDefaultAsync(m => m.Id == id);
        if (material == null)
            return NotFound("Material not found.");

        var allowed =
            (User.IsInRole("Teacher") && await IsTeacherOwnerOfCourse(userId, material.CourseId)) ||
            (User.IsInRole("Student") && await IsStudentEnrolledInCourse(userId, material.CourseId));

        if (!allowed)
            return Forbid();

        return Ok(material);
    }

    [HttpGet("{id:int}/download")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> Download(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var material = await _dbContext.Materials.FirstOrDefaultAsync(m => m.Id == id);
        if (material == null)
            return NotFound("Material not found.");

        var allowed =
            (User.IsInRole("Teacher") && await IsTeacherOwnerOfCourse(userId, material.CourseId)) ||
            (User.IsInRole("Student") && await IsStudentEnrolledInCourse(userId, material.CourseId));

        if (!allowed)
            return Forbid();

        try
        {
            var fileResult = await _azureStorageService.DownloadFileAsync(material.BlobName);

            var fileName = material.Title;
            var extension = Path.GetExtension(material.BlobName);

            if (!string.IsNullOrWhiteSpace(extension) &&
                !fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                fileName += extension;
            }

            return File(fileResult.Stream, fileResult.ContentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound("File not found in storage.");
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task<bool> IsStudentEnrolledInCourse(string userId, Guid courseId)
    {
        return await _dbContext.CourseEnrollments
            .AnyAsync(e => e.StudentId == userId && e.CourseId == courseId);
    }

    private async Task<bool> IsTeacherOwnerOfCourse(string userId, Guid courseId)
    {
        return await _dbContext.Courses
            .AnyAsync(c => c.Id == courseId && c.TeacherId == userId);
    }

    private static string GetMaterialType(string? contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (contentType?.Contains("pdf") == true || extension == ".pdf")
            return "pdf";

        if (contentType?.StartsWith("video/") == true ||
            extension is ".mp4" or ".avi" or ".mov" or ".mkv")
            return "video";

        if (extension is ".doc" or ".docx" or ".ppt" or ".pptx" or ".zip")
            return "assignment";

        return "other";
    }

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "application/octet-stream";
        }

        return contentType
            .Split(';', 2)[0]
            .Trim()
            .ToLowerInvariant();
    }

    private static bool IsAllowedContentType(string contentType, string extension)
    {
        if (AllowedContentTypes.Contains(contentType))
        {
            return true;
        }

        // Some clients, including Postman, frequently send generic octet-stream
        // for file parts even when the extension is valid.
        return string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase)
            && AllowedExtensions.Contains(extension);
    }
}