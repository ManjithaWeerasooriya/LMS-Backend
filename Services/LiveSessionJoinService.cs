using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.LiveSessions;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class LiveSessionJoinService : ILiveSessionJoinService
{
    private readonly ApplicationDBContext _context;
    private readonly IAzureCommunicationIdentityService _azureCommunicationIdentityService;
    private readonly IAzureCommunicationLiveSessionService _azureCommunicationLiveSessionService;

    public LiveSessionJoinService(
        ApplicationDBContext context,
        IAzureCommunicationIdentityService azureCommunicationIdentityService,
        IAzureCommunicationLiveSessionService azureCommunicationLiveSessionService)
    {
        _context = context;
        _azureCommunicationIdentityService = azureCommunicationIdentityService;
        _azureCommunicationLiveSessionService = azureCommunicationLiveSessionService;
    }

    public async Task<LiveSessionJoinTokenResponseDto> CreateJoinTokenAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _context.LiveSessions
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.CreatedByTeacher)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("Live session not found.");
        }

        EnsureSessionCanBeJoined(session);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var isCourseTeacher = string.Equals(session.Course.TeacherId, userId, StringComparison.Ordinal);
        var isEnrolledStudent = !isCourseTeacher && await _context.CourseEnrollments
            .AsNoTracking()
            .AnyAsync(e => e.CourseId == session.CourseId && e.StudentId == userId, cancellationToken);

        if (!isCourseTeacher && !isEnrolledStudent)
        {
            throw new ForbiddenException("You do not have access to join this live session.");
        }

        var tokenResult = await _azureCommunicationIdentityService.CreateJoinTokenAsync(
            user,
            BuildDisplayName(user),
            limitToJoinOnly: ShouldLimitToJoinOnly(session),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(session.ChatThreadId))
        {
            await _azureCommunicationLiveSessionService.EnsureChatParticipantAsync(
                session.CreatedByTeacher,
                BuildDisplayName(session.CreatedByTeacher),
                session.ChatThreadId,
                user,
                tokenResult.DisplayName,
                cancellationToken);
        }

        return new LiveSessionJoinTokenResponseDto
        {
            AcsUserId = tokenResult.AcsUserId,
            Token = tokenResult.Token,
            DisplayName = tokenResult.DisplayName,
            AcsEndpoint = tokenResult.Endpoint,
            MeetingType = MeetingType.Room,
            RoomId = session.RoomId,
            ChatThreadId = session.ChatThreadId,
            Session = new LiveSessionJoinMetadataDto
            {
                Id = session.Id,
                CourseId = session.CourseId,
                CourseTitle = session.Course.Title,
                Title = session.Title,
                StartTime = session.StartTime,
                DurationMinutes = session.DurationMinutes,
                Status = session.Status
            }
        };
    }

    private static void EnsureSessionCanBeJoined(LiveSession session)
    {
        if (session.Status == LiveSessionStatus.Cancelled)
        {
            throw new ConflictException("This live session has been cancelled.");
        }

        if (session.Status == LiveSessionStatus.Ended)
        {
            throw new ConflictException("This live session has already ended.");
        }

        if (session.MeetingType != MeetingType.Room || string.IsNullOrWhiteSpace(session.RoomId))
        {
            throw new ConflictException("This live session is not configured for ACS room joining.");
        }
    }

    private static bool ShouldLimitToJoinOnly(LiveSession session)
    {
        return true;
    }

    private static string BuildDisplayName(User user)
    {
        var preferred = string.Join(
            " ",
            new[] { user.FirstName, user.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));

        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            return user.UserName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            return user.Email.Trim();
        }

        return $"User {user.Id}";
    }
}
