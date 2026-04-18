using LMS_Backend.Models.Entities;

namespace LMS_Backend.Services;

public interface IAzureCommunicationIdentityService
{
    Task<AcsIdentityTokenResult> CreateJoinTokenAsync(
        User user,
        string displayName,
        bool limitToJoinOnly,
        CancellationToken cancellationToken);
}
