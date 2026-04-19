using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/student")]
[Authorize(Policy = AppPolicies.StudentOnly)]
public class StudentMaterialsController : ApiControllerBase
{
    private readonly IMaterialService _materialService;

    public StudentMaterialsController(IMaterialService materialService)
    {
        _materialService = materialService;
    }

    [HttpGet("courses/{courseId:guid}/materials")]
    public async Task<IActionResult> GetMaterialsByCourse(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var materials = await _materialService.GetStudentMaterialsByCourseAsync(
                studentId,
                courseId,
                cancellationToken);

            return Success(materials, "Materials retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("materials/{materialId:int}")]
    public async Task<IActionResult> GetMaterialById(
        int materialId,
        CancellationToken cancellationToken = default)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var material = await _materialService.GetStudentMaterialByIdAsync(
                studentId,
                materialId,
                cancellationToken);

            return Success(material, "Material retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("materials/{materialId:int}/download")]
    public async Task<IActionResult> DownloadMaterial(
        int materialId,
        CancellationToken cancellationToken = default)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var material = await _materialService.DownloadStudentMaterialAsync(
                studentId,
                materialId,
                cancellationToken);

            return File(material.Stream, material.ContentType, material.FileName);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
