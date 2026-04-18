using LMS_Backend.Models.Entities;

namespace LMS_Backend.Services;

public interface IAzureCommunicationLiveSessionService
{
    Task<string> CreateChatThreadAsync(
        User actingUser,
        string actingDisplayName,
        string topic,
        CancellationToken cancellationToken);

    Task EnsureChatParticipantAsync(
        User actingUser,
        string actingDisplayName,
        string chatThreadId,
        User participantUser,
        string participantDisplayName,
        CancellationToken cancellationToken);

    Task<AcsLiveSessionRecordingResult> StartRecordingAsync(
        LiveSession session,
        CancellationToken cancellationToken);

    Task<AcsLiveSessionRecordingResult> StopRecordingAsync(
        LiveSession session,
        CancellationToken cancellationToken);
}
