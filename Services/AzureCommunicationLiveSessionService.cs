using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Chat;
using Azure.Communication.Rooms;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.Extensions.Options;

namespace LMS_Backend.Services;

public class AzureCommunicationLiveSessionService : IAzureCommunicationLiveSessionService
{
    private static readonly object CallAutomationClientLock = new();
    private static readonly object RoomsClientLock = new();
    private static CallAutomationClient? SharedCallAutomationClient;
    private static RoomsClient? SharedRoomsClient;

    private readonly IAzureCommunicationIdentityService _azureCommunicationIdentityService;
    private readonly AzureCommunicationOptions _options;

    public AzureCommunicationLiveSessionService(
        IAzureCommunicationIdentityService azureCommunicationIdentityService,
        IOptions<AzureCommunicationOptions> options)
    {
        _azureCommunicationIdentityService = azureCommunicationIdentityService;
        _options = options.Value;
    }

    public async Task<string> CreateRoomAsync(
        DateTime startTime,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        try
        {
            var response = await GetRoomsClient().CreateRoomAsync(
                BuildRoomCreateOptions(startTime, durationMinutes),
                cancellationToken);

            var roomId = response.Value.Id?.Trim();
            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new ServiceUnavailableException("Azure Communication Services did not return a room identifier.");
            }

            return roomId;
        }
        catch (RequestFailedException ex)
        {
            throw new ServiceUnavailableException("Azure Communication Services rooms are unavailable.", ex);
        }
    }

    public async Task UpdateRoomAsync(
        string roomId,
        DateTime startTime,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        if (string.IsNullOrWhiteSpace(roomId))
        {
            throw new ArgumentException("Room id is required.");
        }

        try
        {
            await GetRoomsClient().UpdateRoomAsync(
                roomId.Trim(),
                BuildRoomUpdateOptions(startTime, durationMinutes),
                cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            throw new ServiceUnavailableException("Azure Communication Services rooms are unavailable.", ex);
        }
    }

    public async Task<string> CreateChatThreadAsync(
        User actingUser,
        string actingDisplayName,
        string topic,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        try
        {
            var chatClient = await CreateChatClientAsync(
                actingUser,
                actingDisplayName,
                cancellationToken);

            var teacherAcsIdentityId = await _azureCommunicationIdentityService.EnsureAcsIdentityAsync(
                actingUser,
                cancellationToken);

            var participants = new[]
            {
                new ChatParticipant(new CommunicationUserIdentifier(teacherAcsIdentityId))
                {
                    DisplayName = actingDisplayName,
                    ShareHistoryTime = DateTimeOffset.UtcNow
                }
            };

            var response = await chatClient.CreateChatThreadAsync(
                topic,
                participants,
                cancellationToken: cancellationToken);

            var threadId = response.Value.ChatThread?.Id;
            if (string.IsNullOrWhiteSpace(threadId))
            {
                throw new ServiceUnavailableException("Azure Communication Services did not return a chat thread identifier.");
            }

            return threadId;
        }
        catch (RequestFailedException ex)
        {
            throw new ServiceUnavailableException("Azure Communication Services chat is unavailable.", ex);
        }
    }

    public async Task EnsureChatParticipantAsync(
        User actingUser,
        string actingDisplayName,
        string chatThreadId,
        User participantUser,
        string participantDisplayName,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        if (string.IsNullOrWhiteSpace(chatThreadId))
        {
            throw new ArgumentException("Chat thread id is required.");
        }

        try
        {
            var chatClient = await CreateChatClientAsync(
                actingUser,
                actingDisplayName,
                cancellationToken);

            var participantAcsIdentityId = await _azureCommunicationIdentityService.EnsureAcsIdentityAsync(
                participantUser,
                cancellationToken);

            var threadClient = chatClient.GetChatThreadClient(chatThreadId);
            await threadClient.AddParticipantAsync(
                new ChatParticipant(new CommunicationUserIdentifier(participantAcsIdentityId))
                {
                    DisplayName = participantDisplayName,
                    ShareHistoryTime = DateTimeOffset.UtcNow
                },
                cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            throw new ServiceUnavailableException("Azure Communication Services chat is unavailable.", ex);
        }
    }

    public async Task<AcsLiveSessionRecordingResult> StartRecordingAsync(
        LiveSession session,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        try
        {
            var response = await GetCallRecordingClient()
                .StartAsync(BuildStartRecordingOptions(session), cancellationToken);

            return new AcsLiveSessionRecordingResult
            {
                AcsRecordingId = response.Value.RecordingId,
                RecordingState = response.Value.RecordingState?.ToString(),
                RecordedAt = DateTime.UtcNow
            };
        }
        catch (RequestFailedException ex)
        {
            throw new ServiceUnavailableException("Azure Communication Services recording is unavailable.", ex);
        }
    }

    public async Task<AcsLiveSessionRecordingResult> StopRecordingAsync(
        LiveSession session,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        if (string.IsNullOrWhiteSpace(session.AcsRecordingId))
        {
            throw new ConflictException("No active recording was found for this live session.");
        }

        try
        {
            var recordingClient = GetCallRecordingClient();
            await recordingClient.StopAsync(session.AcsRecordingId, cancellationToken);

            var state = await recordingClient.GetStateAsync(
                session.AcsRecordingId,
                cancellationToken);

            return new AcsLiveSessionRecordingResult
            {
                AcsRecordingId = session.AcsRecordingId,
                RecordingState = state.Value.RecordingState?.ToString(),
                RecordedAt = DateTime.UtcNow
            };
        }
        catch (RequestFailedException ex)
        {
            throw new ServiceUnavailableException("Azure Communication Services recording is unavailable.", ex);
        }
    }

    private async Task<ChatClient> CreateChatClientAsync(
        User actingUser,
        string actingDisplayName,
        CancellationToken cancellationToken)
    {
        var chatAccess = await _azureCommunicationIdentityService.CreateChatAccessTokenAsync(
            actingUser,
            actingDisplayName,
            cancellationToken);

        return new ChatClient(
            new Uri(_options.Endpoint!),
            new CommunicationTokenCredential(chatAccess.Token));
    }

    private StartRecordingOptions BuildStartRecordingOptions(LiveSession session)
    {
        var options = ResolveRecordingOptions(session);
        options.RecordingFormat = RecordingFormat.Mp4;

        return options;
    }

    private static StartRecordingOptions ResolveRecordingOptions(LiveSession session)
    {
        if (session.MeetingType != MeetingType.Room || string.IsNullOrWhiteSpace(session.RoomId))
        {
            throw new ConflictException("This live session is not configured for ACS room recording.");
        }

        return new StartRecordingOptions(new RoomCallLocator(session.RoomId.Trim()));
    }

    private CallRecording GetCallRecordingClient()
    {
        ValidateConfiguration();

        if (SharedCallAutomationClient != null)
        {
            return SharedCallAutomationClient.GetCallRecording();
        }

        lock (CallAutomationClientLock)
        {
            SharedCallAutomationClient ??= new CallAutomationClient(_options.ConnectionString!);
        }

        return SharedCallAutomationClient.GetCallRecording();
    }

    private RoomsClient GetRoomsClient()
    {
        ValidateConfiguration();

        if (SharedRoomsClient != null)
        {
            return SharedRoomsClient;
        }

        lock (RoomsClientLock)
        {
            SharedRoomsClient ??= new RoomsClient(_options.ConnectionString!);
        }

        return SharedRoomsClient;
    }

    private static CreateRoomOptions BuildRoomCreateOptions(DateTime startTime, int durationMinutes)
    {
        var (validFrom, validUntil) = BuildRoomWindow(startTime, durationMinutes);
        return new CreateRoomOptions
        {
            ValidFrom = validFrom,
            ValidUntil = validUntil
        };
    }

    private static UpdateRoomOptions BuildRoomUpdateOptions(DateTime startTime, int durationMinutes)
    {
        var (validFrom, validUntil) = BuildRoomWindow(startTime, durationMinutes);
        return new UpdateRoomOptions
        {
            ValidFrom = validFrom,
            ValidUntil = validUntil
        };
    }

    private static (DateTimeOffset validFrom, DateTimeOffset validUntil) BuildRoomWindow(
        DateTime startTime,
        int durationMinutes)
    {
        var scheduledStart = startTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(startTime, DateTimeKind.Utc)
            : startTime.ToUniversalTime();

        var validFrom = new DateTimeOffset(scheduledStart.AddMinutes(-15));
        var validUntil = new DateTimeOffset(scheduledStart.AddMinutes(durationMinutes + 60));

        return (validFrom, validUntil);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new ServiceUnavailableException("Azure Communication Services is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new ServiceUnavailableException("Azure Communication Services endpoint is not configured.");
        }
    }
}

public class AcsLiveSessionRecordingResult
{
    public string AcsRecordingId { get; set; } = string.Empty;
    public string? RecordingState { get; set; }
    public DateTime RecordedAt { get; set; }
}
