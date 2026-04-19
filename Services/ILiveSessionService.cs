using LMS_Backend.Models.DTOs.LiveSessions;

namespace LMS_Backend.Services;

public interface ILiveSessionService
{
    Task<LiveSessionDto> CreateLiveSessionAsync(
        string teacherId,
        Guid courseId,
        CreateLiveSessionRequestDto dto,
        CancellationToken cancellationToken);

    Task<LiveSessionDto> UpdateLiveSessionAsync(
        string teacherId,
        Guid sessionId,
        UpdateLiveSessionRequestDto dto,
        CancellationToken cancellationToken);

    Task CancelLiveSessionAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<LiveSessionDto> StartLiveSessionAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<LiveSessionDto> EndLiveSessionAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<LiveSessionRecordingDto> StartRecordingAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<LiveSessionRecordingDto> StopRecordingAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LiveSessionDto>> GetTeacherLiveSessionsByCourseAsync(
        string teacherId,
        Guid courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LiveSessionDto>> GetStudentLiveSessionsByCourseAsync(
        string studentId,
        Guid courseId,
        CancellationToken cancellationToken);

    Task<LiveSessionDto> GetStudentLiveSessionByIdAsync(
        string studentId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<LiveSessionRecordingDto> GetStudentRecordingAsync(
        string studentId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<LiveSessionAttendanceDto> JoinAttendanceAsync(
        string studentId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<LiveSessionAttendanceDto> LeaveAttendanceAsync(
        string studentId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<LiveSessionAttendanceSummaryDto> GetLiveSessionAttendanceSummaryAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken);
}
