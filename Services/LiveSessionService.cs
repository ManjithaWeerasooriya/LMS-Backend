using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.LiveSessions;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class LiveSessionService : ILiveSessionService
{
    private static readonly TimeSpan LateThreshold = TimeSpan.FromMinutes(5);

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
            Status = LiveSessionStatus.Scheduled,
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

    public async Task<LiveSessionDto> StartLiveSessionAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetManagedSessionAsync(teacherId, sessionId, cancellationToken);

        if (session.Status == LiveSessionStatus.Live)
        {
            return await GetLiveSessionForResponseAsync(session.Id, cancellationToken);
        }

        if (session.Status != LiveSessionStatus.Scheduled)
        {
            throw new ConflictException("Only scheduled live sessions can be started.");
        }

        session.Status = LiveSessionStatus.Live;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetLiveSessionForResponseAsync(session.Id, cancellationToken);
    }

    public async Task<LiveSessionDto> EndLiveSessionAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetManagedSessionAsync(teacherId, sessionId, cancellationToken);

        if (session.Status == LiveSessionStatus.Ended)
        {
            return await GetLiveSessionForResponseAsync(session.Id, cancellationToken);
        }

        if (session.Status != LiveSessionStatus.Live)
        {
            throw new ConflictException("Only live sessions can be ended.");
        }

        var endedAt = DateTime.UtcNow;

        session.Status = LiveSessionStatus.Ended;
        session.UpdatedAt = endedAt;

        var activeAttendances = await _context.LiveSessionAttendances
            .Where(a => a.SessionId == session.Id && a.JoinTime != null && a.LeaveTime == null)
            .ToListAsync(cancellationToken);

        foreach (var attendance in activeAttendances)
        {
            FinalizeAttendance(session, attendance, endedAt);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetLiveSessionForResponseAsync(session.Id, cancellationToken);
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

    public async Task<LiveSessionAttendanceDto> JoinAttendanceAsync(
        string studentId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetStudentAccessibleSessionAsync(studentId, sessionId, cancellationToken);
        EnsureSessionCanBeJoinedForAttendance(session);

        var joinedAt = DateTime.UtcNow;

        var attendance = await _context.LiveSessionAttendances
            .FirstOrDefaultAsync(
                a => a.SessionId == sessionId && a.StudentId == studentId,
                cancellationToken);

        if (attendance == null)
        {
            attendance = new LiveSessionAttendance
            {
                SessionId = sessionId,
                StudentId = studentId,
                JoinTime = joinedAt,
                DurationSeconds = 0,
                AttendanceStatus = DetermineAttendanceStatus(session, joinedAt),
                LastSeenAt = joinedAt
            };

            _context.LiveSessionAttendances.Add(attendance);
        }
        else
        {
            if (attendance.LeaveTime != null)
            {
                throw new ConflictException("Attendance for this live session has already been completed.");
            }

            attendance.JoinTime ??= joinedAt;
            attendance.LastSeenAt = joinedAt;
            attendance.AttendanceStatus = DetermineAttendanceStatus(session, attendance.JoinTime.Value);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ToLiveSessionAttendanceDto(attendance);
    }

    public async Task<LiveSessionAttendanceDto> LeaveAttendanceAsync(
        string studentId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetStudentAccessibleSessionAsync(studentId, sessionId, cancellationToken);

        var attendance = await _context.LiveSessionAttendances
            .FirstOrDefaultAsync(
                a => a.SessionId == sessionId && a.StudentId == studentId,
                cancellationToken);

        if (attendance == null || attendance.JoinTime == null)
        {
            throw new ConflictException("You have not joined this live session.");
        }

        if (attendance.LeaveTime != null)
        {
            return ToLiveSessionAttendanceDto(attendance);
        }

        if (session.Status == LiveSessionStatus.Scheduled)
        {
            throw new ConflictException("This live session has not started yet.");
        }

        FinalizeAttendance(session, attendance, DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        return ToLiveSessionAttendanceDto(attendance);
    }

    public async Task<LiveSessionAttendanceSummaryDto> GetLiveSessionAttendanceSummaryAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetManagedSessionAsync(teacherId, sessionId, cancellationToken);

        var enrolledStudents = await _context.CourseEnrollments
            .AsNoTracking()
            .Where(e => e.CourseId == session.CourseId)
            .Select(e => new
            {
                e.StudentId,
                e.Student.FirstName,
                e.Student.LastName,
                e.Student.Email
            })
            .ToListAsync(cancellationToken);

        var attendanceRecords = await _context.LiveSessionAttendances
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId)
            .ToDictionaryAsync(a => a.StudentId, cancellationToken);

        var now = DateTime.UtcNow;

        var students = enrolledStudents
            .Select(student =>
            {
                attendanceRecords.TryGetValue(student.StudentId, out var attendance);

                var durationSeconds = attendance?.DurationSeconds ?? 0;
                if (attendance?.JoinTime != null &&
                    attendance.LeaveTime == null &&
                    session.Status == LiveSessionStatus.Live)
                {
                    durationSeconds = CalculateDurationSeconds(attendance.JoinTime.Value, now);
                }

                return new LiveSessionAttendanceStudentDto
                {
                    StudentId = student.StudentId,
                    StudentName = BuildDisplayName(
                        student.FirstName,
                        student.LastName,
                        student.Email),
                    StudentEmail = student.Email,
                    JoinTime = attendance?.JoinTime,
                    LeaveTime = attendance?.LeaveTime,
                    DurationSeconds = durationSeconds,
                    AttendanceStatus = attendance?.AttendanceStatus ?? AttendanceStatus.Absent,
                    LastSeenAt = attendance?.LastSeenAt
                };
            })
            .OrderBy(student => student.StudentName ?? student.StudentEmail ?? student.StudentId)
            .ToList();

        return new LiveSessionAttendanceSummaryDto
        {
            SessionId = session.Id,
            CourseId = session.CourseId,
            CourseTitle = session.Course.Title,
            SessionTitle = session.Title,
            StartTime = session.StartTime,
            DurationMinutes = session.DurationMinutes,
            Status = session.Status,
            TotalEnrolledStudents = students.Count,
            TotalJoinedStudents = students.Count(s => s.JoinTime.HasValue),
            TotalCompletedAttendances = students.Count(s => s.LeaveTime.HasValue),
            Students = students
        };
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

    private async Task<LiveSession> GetStudentAccessibleSessionAsync(
        string studentId,
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

        var isEnrolled = await _context.CourseEnrollments
            .AsNoTracking()
            .AnyAsync(
                e => e.CourseId == session.CourseId && e.StudentId == studentId,
                cancellationToken);

        if (!isEnrolled)
        {
            throw new ForbiddenException("You must be enrolled in the course to access this live session.");
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

    private static void EnsureSessionCanBeJoinedForAttendance(LiveSession session)
    {
        if (session.Status == LiveSessionStatus.Scheduled)
        {
            throw new ConflictException("This live session has not started yet.");
        }

        if (session.Status == LiveSessionStatus.Ended)
        {
            throw new ConflictException("This live session has already ended.");
        }

        if (session.Status == LiveSessionStatus.Cancelled)
        {
            throw new ConflictException("This live session has been cancelled.");
        }
    }

    private static void FinalizeAttendance(
        LiveSession session,
        LiveSessionAttendance attendance,
        DateTime leftAt)
    {
        var joinTime = attendance.JoinTime ?? leftAt;
        var effectiveLeaveTime = leftAt < joinTime ? joinTime : leftAt;

        attendance.JoinTime ??= joinTime;
        attendance.LeaveTime = effectiveLeaveTime;
        attendance.DurationSeconds = CalculateDurationSeconds(attendance.JoinTime.Value, effectiveLeaveTime);
        attendance.AttendanceStatus = DetermineAttendanceStatus(session, attendance.JoinTime.Value);
        attendance.LastSeenAt = effectiveLeaveTime;
    }

    private static LiveSessionDto ToLiveSessionDto(LiveSession session)
    {
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
            TeacherName = BuildDisplayName(
                session.CreatedByTeacher.FirstName,
                session.CreatedByTeacher.LastName,
                session.CreatedByTeacher.Email),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    private static LiveSessionAttendanceDto ToLiveSessionAttendanceDto(LiveSessionAttendance attendance)
    {
        return new LiveSessionAttendanceDto
        {
            Id = attendance.Id,
            SessionId = attendance.SessionId,
            StudentId = attendance.StudentId,
            JoinTime = attendance.JoinTime,
            LeaveTime = attendance.LeaveTime,
            DurationSeconds = attendance.DurationSeconds,
            AttendanceStatus = attendance.AttendanceStatus,
            LastSeenAt = attendance.LastSeenAt
        };
    }

    private static int CalculateDurationSeconds(DateTime start, DateTime end)
    {
        if (end <= start)
        {
            return 0;
        }

        return (int)Math.Round((end - start).TotalSeconds, MidpointRounding.AwayFromZero);
    }

    private static AttendanceStatus DetermineAttendanceStatus(
        LiveSession session,
        DateTime joinTime)
    {
        return joinTime > session.StartTime.Add(LateThreshold)
            ? AttendanceStatus.Late
            : AttendanceStatus.Present;
    }

    private static string? BuildDisplayName(
        string? firstName,
        string? lastName,
        string? fallback)
    {
        var fullName = string.Join(
            " ",
            new[] { firstName, lastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return NormalizeOptional(fallback);
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
