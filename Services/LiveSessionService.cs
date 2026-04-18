using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.LiveSessions;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class LiveSessionService : ILiveSessionService
{
    private readonly ApplicationDBContext _context;

    public LiveSessionService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<LiveSessionDto> CreateLiveSessionAsync(
        string teacherId,
        Guid courseId,
        CreateLiveSessionRequestDto dto,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsCourseAsync(teacherId, courseId, cancellationToken);

        var session = new LiveSession
        {
            CourseId = courseId,
            Title = NormalizeRequired(dto.Title, "Title"),
            Description = NormalizeOptional(dto.Description),
            StartTime = dto.StartTime,
            DurationMinutes = dto.DurationMinutes,
            Status = dto.Status,
            RecordingEnabled = dto.RecordingEnabled,
            PlaybackEnabled = dto.PlaybackEnabled,
            AcsRoomId = NormalizeOptional(dto.AcsRoomId),
            AcsCallLocator = NormalizeOptional(dto.AcsCallLocator),
            ChatThreadId = NormalizeOptional(dto.ChatThreadId),
            CreatedByTeacherId = teacherId,
            CreatedAt = DateTime.UtcNow
        };

        _context.LiveSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetLiveSessionForResponseAsync(session.Id, cancellationToken);
    }

    public async Task<LiveSessionDto> UpdateLiveSessionAsync(
        string teacherId,
        Guid sessionId,
        UpdateLiveSessionRequestDto dto,
        CancellationToken cancellationToken)
    {
        var session = await GetManagedSessionAsync(teacherId, sessionId, cancellationToken);

        session.Title = NormalizeRequired(dto.Title, "Title");
        session.Description = NormalizeOptional(dto.Description);
        session.StartTime = dto.StartTime;
        session.DurationMinutes = dto.DurationMinutes;
        session.Status = dto.Status;
        session.RecordingEnabled = dto.RecordingEnabled;
        session.PlaybackEnabled = dto.PlaybackEnabled;
        session.AcsRoomId = NormalizeOptional(dto.AcsRoomId);
        session.AcsCallLocator = NormalizeOptional(dto.AcsCallLocator);
        session.ChatThreadId = NormalizeOptional(dto.ChatThreadId);
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetLiveSessionForResponseAsync(session.Id, cancellationToken);
    }

    public async Task CancelLiveSessionAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetManagedSessionAsync(teacherId, sessionId, cancellationToken);

        if (session.Status == LiveSessionStatus.Cancelled)
        {
            return;
        }

        session.Status = LiveSessionStatus.Cancelled;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LiveSessionDto>> GetTeacherLiveSessionsByCourseAsync(
        string teacherId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        await EnsureTeacherOwnsCourseAsync(teacherId, courseId, cancellationToken);

        var sessions = await BuildResponseQuery()
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return sessions.Select(ToLiveSessionDto).ToList();
    }

    public async Task<IReadOnlyList<LiveSessionDto>> GetStudentLiveSessionsByCourseAsync(
        string studentId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        await EnsureStudentEnrolledInCourseAsync(studentId, courseId, cancellationToken);

        var sessions = await BuildResponseQuery()
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return sessions.Select(ToLiveSessionDto).ToList();
    }

    public async Task<LiveSessionDto> GetStudentLiveSessionByIdAsync(
        string studentId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        await EnsureStudentCanAccessSessionAsync(studentId, sessionId, cancellationToken);

        return await GetLiveSessionForResponseAsync(sessionId, cancellationToken);
    }

    private IQueryable<LiveSession> BuildResponseQuery()
    {
        return _context.LiveSessions
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.CreatedByTeacher);
    }

    private async Task<LiveSessionDto> GetLiveSessionForResponseAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await BuildResponseQuery()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("Live session not found.");
        }

        return ToLiveSessionDto(session);
    }

    private async Task EnsureTeacherOwnsCourseAsync(
        string teacherId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Id, c.TeacherId })
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            throw new NotFoundException("Course not found.");
        }

        if (!string.Equals(course.TeacherId, teacherId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You do not have access to manage live sessions for this course.");
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
            throw new ForbiddenException("You must be enrolled in the course to access its live sessions.");
        }
    }

    private async Task<LiveSession> GetManagedSessionAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _context.LiveSessions
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("Live session not found.");
        }

        if (!string.Equals(session.Course.TeacherId, teacherId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You do not have access to manage this live session.");
        }

        return session;
    }

    private async Task EnsureStudentCanAccessSessionAsync(
        string studentId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var access = await _context.LiveSessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId)
            .Select(s => new
            {
                s.Id,
                IsEnrolled = s.Course.Enrollments.Any(e => e.StudentId == studentId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (access == null)
        {
            throw new NotFoundException("Live session not found.");
        }

        if (!access.IsEnrolled)
        {
            throw new ForbiddenException("You must be enrolled in the course to access this live session.");
        }
    }

    private static LiveSessionDto ToLiveSessionDto(LiveSession session)
    {
        var teacherName = string.Join(
            " ",
            new[] { session.CreatedByTeacher.FirstName, session.CreatedByTeacher.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return new LiveSessionDto
        {
            Id = session.Id,
            CourseId = session.CourseId,
            CourseTitle = session.Course.Title,
            Title = session.Title,
            Description = session.Description,
            StartTime = session.StartTime,
            DurationMinutes = session.DurationMinutes,
            Status = session.Status,
            RecordingEnabled = session.RecordingEnabled,
            PlaybackEnabled = session.PlaybackEnabled,
            AcsRoomId = session.AcsRoomId,
            AcsCallLocator = session.AcsCallLocator,
            ChatThreadId = session.ChatThreadId,
            CreatedByTeacherId = session.CreatedByTeacherId,
            TeacherName = string.IsNullOrWhiteSpace(teacherName) ? null : teacherName,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = NormalizeOptional(value);
        if (normalized == null)
        {
            throw new ArgumentException($"{fieldName} is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
