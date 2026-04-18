using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Chat;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.Extensions.Options;

namespace LMS_Backend.Services;

public class AzureCommunicationLiveSessionService : IAzureCommunicationLiveSessionService
{
    private static readonly object CallAutomationClientLock = new();
    private static CallAutomationClient? SharedCallAutomationClient;

    private readonly IAzureCommunicationIdentityService _azureCommunicationIdentityService;
    private readonly AzureCommunicationOptions _options;

    public AzureCommunicationLiveSessionService(
        IAzureCommunicationIdentityService azureCommunicationIdentityService,
        IOptions<AzureCommunicationOptions> options)
    {
        _azureCommunicationIdentityService = azureCommunicationIdentityService;
        _options = options.Value;
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
        if (!string.IsNullOrWhiteSpace(session.AcsCallLocator))
        {
            var normalized = session.AcsCallLocator.Trim();

            if (normalized.StartsWith("connection:", StringComparison.OrdinalIgnoreCase))
            {
                return new StartRecordingOptions(normalized["connection:".Length..].Trim());
            }

            if (normalized.StartsWith("server:", StringComparison.OrdinalIgnoreCase))
            {
                return new StartRecordingOptions(
                    new ServerCallLocator(normalized["server:".Length..].Trim()));
            }

            if (normalized.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
            {
                return new StartRecordingOptions(
                    new GroupCallLocator(normalized["group:".Length..].Trim()));
            }

            if (normalized.StartsWith("room:", StringComparison.OrdinalIgnoreCase))
            {
                return new StartRecordingOptions(
                    new RoomCallLocator(normalized["room:".Length..].Trim()));
            }

            return new StartRecordingOptions(new ServerCallLocator(normalized));
        }

        if (!string.IsNullOrWhiteSpace(session.AcsRoomId))
        {
            return new StartRecordingOptions(new RoomCallLocator(session.AcsRoomId.Trim()));
        }

        throw new ConflictException("This live session is not configured for ACS recording.");
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
