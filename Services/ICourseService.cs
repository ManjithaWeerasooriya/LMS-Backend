using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Models.Entities;

namespace LMS_Backend.Services;

public interface ICourseService
{
    Task<Course> CreateCourseAsync(
        string teacherId,
        CreateCourseRequestDto dto,
        CancellationToken cancellationToken);

    Task<List<CourseListItemDto>> GetCoursesForTeacherAsync(
        string teacherId,
        string? search,
        CancellationToken cancellationToken);

    Task<PagedResult<CourseListItemDto>> GetCoursesForManagementAsync(
        CourseQueryOptions options);

    Task<Course?> GetCourseAsync(
        Guid id,
        string teacherId,
        CancellationToken cancellationToken);

    Task<CourseDetailDto?> GetCourseDetailForTeacherAsync(
        Guid id,
        string teacherId,
        CancellationToken cancellationToken);

    Task<bool> UpdateCourseAsync(
        Guid id,
        string teacherId,
        CreateCourseRequestDto dto,
        CancellationToken cancellationToken);

    Task<bool> DeleteCourseAsync(
        Guid id,
        string teacherId,
        CancellationToken cancellationToken);

    Task<bool> ArchiveCourseAsync(Guid courseId);

    Task<bool> DeleteCourseForManagementAsync(Guid courseId);

    Task<CourseEnrollmentResult> EnrollStudentInCourseAsync(
        Guid courseId,
        string studentId,
        CancellationToken cancellationToken);

    Task<List<StudentCourseListItemDto>> GetCoursesForStudentAsync(
        string studentId,
        string? search,
        CancellationToken cancellationToken);

    Task<List<StudentCourseListItemDto>> GetEnrolledCoursesForStudentAsync(
        string studentId,
        CancellationToken cancellationToken);
}
