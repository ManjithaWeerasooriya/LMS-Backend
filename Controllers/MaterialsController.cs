using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/materials")]
public class MaterialsController : ApiControllerBase
{
    private readonly IMaterialService _materialService;

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

    public MaterialsController(IMaterialService materialService)
    {
        _materialService = materialService;
    }

    [HttpPost("upload")]
    [Authorize(Policy = AppPolicies.TeacherOnly)]
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
            return UnauthorizedResponse();

        var finalTitle = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(file.FileName)
            : title;

        try
        {
            var material = await _materialService.UploadTeacherMaterialAsync(
                userId,
                courseId,
                file,
                finalTitle,
                contentType,
                GetMaterialType(contentType, file.FileName),
                HttpContext.RequestAborted);

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
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("course/{courseId:guid}")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> GetByCourse(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return UnauthorizedResponse();

        try
        {
            if (User.IsInRole(AppRoles.Teacher))
            {
                var materials = await _materialService.GetTeacherMaterialsByCourseAsync(
                    userId,
                    courseId,
                    cancellationToken);

                return Ok(materials);
            }

            if (User.IsInRole(AppRoles.Student))
            {
                var materials = await _materialService.GetStudentMaterialsByCourseAsync(
                    userId,
                    courseId,
                    cancellationToken);

                return Ok(materials);
            }

            return Forbid();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return UnauthorizedResponse();

        try
        {
            if (User.IsInRole(AppRoles.Teacher))
            {
                var material = await _materialService.GetTeacherMaterialByIdAsync(
                    userId,
                    id,
                    cancellationToken);

                return Ok(material);
            }

            if (User.IsInRole(AppRoles.Student))
            {
                var material = await _materialService.GetStudentMaterialByIdAsync(
                    userId,
                    id,
                    cancellationToken);

                return Ok(material);
            }

            return Forbid();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:int}/download")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> Download(
        int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return UnauthorizedResponse();

        try
        {
            if (User.IsInRole(AppRoles.Teacher))
            {
                var material = await _materialService.DownloadTeacherMaterialAsync(
                    userId,
                    id,
                    cancellationToken);

                return File(material.Stream, material.ContentType, material.FileName);
            }

            if (User.IsInRole(AppRoles.Student))
            {
                var material = await _materialService.DownloadStudentMaterialAsync(
                    userId,
                    id,
                    cancellationToken);

                return File(material.Stream, material.ContentType, material.FileName);
            }

            return Forbid();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
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
