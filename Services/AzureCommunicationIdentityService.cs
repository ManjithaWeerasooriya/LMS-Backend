using System.Collections.Concurrent;
using Azure;
using Azure.Communication;
using Azure.Communication.Identity;
using LMS_Backend.Data;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.Extensions.Options;

namespace LMS_Backend.Services;

public class AzureCommunicationIdentityService : IAzureCommunicationIdentityService
{
    private static readonly CommunicationTokenScope[] DefaultJoinScopes =
    {
        CommunicationTokenScope.ChatJoinLimited,
        CommunicationTokenScope.VoIP
    };

    private static readonly CommunicationTokenScope[] RestrictedJoinScopes =
    {
        CommunicationTokenScope.ChatJoinLimited,
        CommunicationTokenScope.VoIPJoin
    };

    private static readonly CommunicationTokenScope[] ChatAccessScopes =
    {
        CommunicationTokenScope.Chat
    };

    private static readonly object ClientLock = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> UserIdentityLocks = new();
    private static CommunicationIdentityClient? SharedClient;
    private readonly AzureCommunicationOptions _options;
    private readonly ApplicationDBContext _context;

    public AzureCommunicationIdentityService(
        IOptions<AzureCommunicationOptions> options,
        ApplicationDBContext context)
    {
        _options = options.Value;
        _context = context;
    }

    public async Task<AcsIdentityTokenResult> CreateJoinTokenAsync(
        User user,
        string displayName,
        bool limitToJoinOnly,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        try
        {
            var client = GetRequiredClient();
            var acsUser = await GetOrCreateAcsUserAsync(client, user, cancellationToken);
            var tokenLifetime = TimeSpan.FromHours(_options.AccessTokenLifetimeHours);
            var tokenResponse = await client.GetTokenAsync(
                acsUser,
                limitToJoinOnly ? RestrictedJoinScopes : DefaultJoinScopes,
                tokenLifetime,
                cancellationToken);

            return new AcsIdentityTokenResult
            {
                AcsUserId = acsUser.Id,
                Token = tokenResponse.Value.Token,
                DisplayName = displayName,
                Endpoint = _options.Endpoint
            };
        }
        catch (RequestFailedException ex)
        {
            throw new ServiceUnavailableException("Azure Communication Services is unavailable.", ex);
        }
    }

    public async Task<string> EnsureAcsIdentityAsync(
        User user,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        try
        {
            var acsUser = await GetOrCreateAcsUserAsync(
                GetRequiredClient(),
                user,
                cancellationToken);

            return acsUser.Id;
        }
        catch (RequestFailedException ex)
        {
            throw new ServiceUnavailableException("Azure Communication Services is unavailable.", ex);
        }
    }

    public async Task<AcsIdentityTokenResult> CreateChatAccessTokenAsync(
        User user,
        string displayName,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        try
        {
            var client = GetRequiredClient();
            var acsUser = await GetOrCreateAcsUserAsync(client, user, cancellationToken);
            var tokenLifetime = TimeSpan.FromHours(_options.AccessTokenLifetimeHours);
            var tokenResponse = await client.GetTokenAsync(
                acsUser,
                ChatAccessScopes,
                tokenLifetime,
                cancellationToken);

            return new AcsIdentityTokenResult
            {
                AcsUserId = acsUser.Id,
                Token = tokenResponse.Value.Token,
                DisplayName = displayName,
                Endpoint = _options.Endpoint
            };
        }
        catch (RequestFailedException ex)
        {
            throw new ServiceUnavailableException("Azure Communication Services is unavailable.", ex);
        }
    }

    private async Task<CommunicationUserIdentifier> GetOrCreateAcsUserAsync(
        CommunicationIdentityClient client,
        User user,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(user.AcsIdentityId))
        {
            return new CommunicationUserIdentifier(user.AcsIdentityId);
        }

        var userLock = UserIdentityLocks.GetOrAdd(user.Id, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);

        try
        {
            await _context.Entry(user).ReloadAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(user.AcsIdentityId))
            {
                return new CommunicationUserIdentifier(user.AcsIdentityId);
            }

            var userResponse = await client.CreateUserAsync(cancellationToken);
            user.AcsIdentityId = userResponse.Value.Id;
            await _context.SaveChangesAsync(cancellationToken);

            return userResponse.Value;
        }
        finally
        {
            userLock.Release();
        }
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

        if (_options.AccessTokenLifetimeHours is < 1 or > 24)
        {
            throw new ServiceUnavailableException("Azure Communication Services token lifetime must be between 1 and 24 hours.");
        }
    }

    private CommunicationIdentityClient GetRequiredClient()
    {
        ValidateConfiguration();

        if (SharedClient != null)
        {
            return SharedClient;
        }

        lock (ClientLock)
        {
            SharedClient ??= new CommunicationIdentityClient(_options.ConnectionString!);
        }

        return SharedClient;
    }
}

public class AcsIdentityTokenResult
{
    public string AcsUserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
}
