using System.Security.Claims;
using LMS_Backend.Data;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaterialsController : ControllerBase
{
    private readonly AzureStorageService _azureStorageService;
    private readonly ApplicationDBContext _dbContext;

    public MaterialsController(
        AzureStorageService azureStorageService,
        ApplicationDBContext dbContext)
    {
        _azureStorageService = azureStorageService;
        _dbContext = dbContext;
    }

    // ✅ UPLOAD
    [HttpPost("upload")]
    [Authorize(Roles = "Teacher")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm(Name = "file")] IFormFile file,
        [FromForm] string? title,
        [FromForm] Guid courseId)
    {
        // 🔴 Validation
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (courseId == Guid.Empty)
            return BadRequest("Invalid courseId.");

        // 🔐 Get user
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // 🔐 Check ownership
        var ownsCourse = await IsTeacherOwnerOfCourse(userId, courseId);
        if (!ownsCourse)
            return Forbid();

        // 📤 Upload file
        var uploadResult = await _azureStorageService.UploadFileAsync(file);

        // 🧠 Auto title fallback
        var finalTitle = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(file.FileName)
            : title;

        // 💾 Save to DB
        var material = new Material
        {
            Title = finalTitle,
            FileUrl = uploadResult.FileUrl,
            BlobName = uploadResult.BlobName,
            ContentType = file.ContentType ?? "application/octet-stream",
            MaterialType = GetMaterialType(file.ContentType, file.FileName),
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

    // ✅ GET BY COURSE
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

    // ✅ GET BY ID
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

    // ✅ DOWNLOAD
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

        return Redirect(material.FileUrl);
    }

    // 🔧 HELPERS
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
}