using LMS_Backend.Models.DTOs.Materials;

namespace LMS_Backend.Services;

public interface IMaterialService
{
    Task<IReadOnlyList<MaterialDto>> GetTeacherMaterialsByCourseAsync(
        string teacherId,
        Guid courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaterialDto>> GetStudentMaterialsByCourseAsync(
        string studentId,
        Guid courseId,
        CancellationToken cancellationToken);

    Task<MaterialDto> GetTeacherMaterialByIdAsync(
        string teacherId,
        int materialId,
        CancellationToken cancellationToken);

    Task<MaterialDto> GetStudentMaterialByIdAsync(
        string studentId,
        int materialId,
        CancellationToken cancellationToken);

    Task<MaterialDownloadResult> DownloadTeacherMaterialAsync(
        string teacherId,
        int materialId,
        CancellationToken cancellationToken);

    Task<MaterialDownloadResult> DownloadStudentMaterialAsync(
        string studentId,
        int materialId,
        CancellationToken cancellationToken);
}
