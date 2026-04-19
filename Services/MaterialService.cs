using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Materials;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class MaterialService : IMaterialService
{
    private readonly ApplicationDBContext _context;
    private readonly AzureStorageService _azureStorageService;

    public MaterialService(
        ApplicationDBContext context,
        AzureStorageService azureStorageService)
    {
        _context = context;
        _azureStorageService = azureStorageService;
    }

    public async Task<IReadOnlyList<MaterialDto>> GetTeacherMaterialsByCourseAsync(
        string teacherId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsCourseAsync(teacherId, courseId, cancellationToken);

        return await _context.Materials
            .AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MaterialDto
            {
                Id = m.Id,
                Title = m.Title,
                FileUrl = m.FileUrl,
                BlobName = m.BlobName,
                ContentType = m.ContentType,
                MaterialType = m.MaterialType,
                FileSize = m.FileSize,
                CourseId = m.CourseId,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialDto>> GetStudentMaterialsByCourseAsync(
        string studentId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        await EnsureStudentEnrolledInCourseAsync(studentId, courseId, cancellationToken);

        return await _context.Materials
            .AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MaterialDto
            {
                Id = m.Id,
                Title = m.Title,
                FileUrl = m.FileUrl,
                BlobName = m.BlobName,
                ContentType = m.ContentType,
                MaterialType = m.MaterialType,
                FileSize = m.FileSize,
                CourseId = m.CourseId,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<MaterialDto> GetTeacherMaterialByIdAsync(
        string teacherId,
        int materialId,
        CancellationToken cancellationToken)
    {
        var material = await GetTeacherAccessibleMaterialAsync(teacherId, materialId, cancellationToken);
        return ToMaterialDto(material);
    }

    public async Task<MaterialDto> GetStudentMaterialByIdAsync(
        string studentId,
        int materialId,
        CancellationToken cancellationToken)
    {
        var material = await GetStudentAccessibleMaterialAsync(studentId, materialId, cancellationToken);
        return ToMaterialDto(material);
    }

    public async Task<MaterialDownloadResult> DownloadTeacherMaterialAsync(
        string teacherId,
        int materialId,
        CancellationToken cancellationToken)
    {
        var material = await GetTeacherAccessibleMaterialAsync(teacherId, materialId, cancellationToken);
        return await DownloadMaterialInternalAsync(material);
    }

    public async Task<MaterialDownloadResult> DownloadStudentMaterialAsync(
        string studentId,
        int materialId,
        CancellationToken cancellationToken)
    {
        var material = await GetStudentAccessibleMaterialAsync(studentId, materialId, cancellationToken);
        return await DownloadMaterialInternalAsync(material);
    }

    private async Task EnsureTeacherOwnsCourseAsync(
        string teacherId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new
            {
                c.Id,
                c.TeacherId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            throw new NotFoundException("Course not found.");
        }

        if (!string.Equals(course.TeacherId, teacherId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You do not have access to manage materials for this course.");
        }
    }

    private async Task EnsureStudentEnrolledInCourseAsync(
        string studentId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new
            {
                c.Id,
                IsEnrolled = c.Enrollments.Any(e => e.StudentId == studentId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            throw new NotFoundException("Course not found.");
        }

        if (!course.IsEnrolled)
        {
            throw new ForbiddenException("You must be enrolled in the course to access its materials.");
        }
    }

    private async Task<Material> GetTeacherAccessibleMaterialAsync(
        string teacherId,
        int materialId,
        CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .AsNoTracking()
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == materialId, cancellationToken);

        if (material == null)
        {
            throw new NotFoundException("Material not found.");
        }

        if (!string.Equals(material.Course.TeacherId, teacherId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You do not have access to manage this material.");
        }

        return material;
    }

    private async Task<Material> GetStudentAccessibleMaterialAsync(
        string studentId,
        int materialId,
        CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == materialId, cancellationToken);

        if (material == null)
        {
            throw new NotFoundException("Material not found.");
        }

        var isEnrolled = await _context.CourseEnrollments
            .AsNoTracking()
            .AnyAsync(
                e => e.CourseId == material.CourseId && e.StudentId == studentId,
                cancellationToken);

        if (!isEnrolled)
        {
            throw new ForbiddenException("You must be enrolled in the course to access this material.");
        }

        return material;
    }

    private async Task<MaterialDownloadResult> DownloadMaterialInternalAsync(Material material)
    {
        try
        {
            var fileResult = await _azureStorageService.DownloadFileAsync(material.BlobName);

            return new MaterialDownloadResult
            {
                Stream = fileResult.Stream,
                ContentType = fileResult.ContentType,
                FileName = BuildDownloadFileName(material)
            };
        }
        catch (FileNotFoundException ex)
        {
            throw new NotFoundException("File not found in storage.", ex);
        }
    }

    private static string BuildDownloadFileName(Material material)
    {
        var fileName = material.Title;
        var extension = Path.GetExtension(material.BlobName);

        if (!string.IsNullOrWhiteSpace(extension) &&
            !fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            fileName += extension;
        }

        return fileName;
    }

    private static MaterialDto ToMaterialDto(Material material)
    {
        return new MaterialDto
        {
            Id = material.Id,
            Title = material.Title,
            FileUrl = material.FileUrl,
            BlobName = material.BlobName,
            ContentType = material.ContentType,
            MaterialType = material.MaterialType,
            FileSize = material.FileSize,
            CourseId = material.CourseId,
            CreatedAt = material.CreatedAt
        };
    }
}
