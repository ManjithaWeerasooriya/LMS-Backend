using LMS_Backend.Models.Entities;

namespace LMS_Backend.Services;

public interface IAzureCommunicationIdentityService
{
    Task<string> EnsureAcsIdentityAsync(
        User user,
        CancellationToken cancellationToken);

    Task<AcsIdentityTokenResult> CreateJoinTokenAsync(
        User user,
        string displayName,
        bool limitToJoinOnly,
        CancellationToken cancellationToken);

    Task<AcsIdentityTokenResult> CreateChatAccessTokenAsync(
        User user,
        string displayName,
        CancellationToken cancellationToken);
}
