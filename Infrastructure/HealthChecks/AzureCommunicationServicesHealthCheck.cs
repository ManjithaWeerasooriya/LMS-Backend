using System.Net;
using LMS_Backend.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LMS_Backend.Infrastructure.HealthChecks;

public sealed class AzureCommunicationServicesHealthCheck : IHealthCheck
{
    private readonly AzureCommunicationOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureCommunicationServicesHealthCheck(
        IOptions<AzureCommunicationOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["tokenLifetimeHours"] = _options.AccessTokenLifetimeHours
        };

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return HealthCheckResult.Unhealthy(
                "Azure Communication Services connection string is not configured.",
                data: data);
        }

        if (!TryCreateUri(_options.Endpoint, out var configuredEndpoint))
        {
            return HealthCheckResult.Unhealthy(
                "Azure Communication Services endpoint is not configured or is invalid.",
                data: data);
        }

        data["endpoint"] = configuredEndpoint.ToString();

        if (_options.AccessTokenLifetimeHours is < 1 or > 24)
        {
            return HealthCheckResult.Unhealthy(
                "Azure Communication Services token lifetime must be between 1 and 24 hours.",
                data: data);
        }

        if (!TryParseConnectionString(_options.ConnectionString, out var connectionStringParts, out var parseError))
        {
            return HealthCheckResult.Unhealthy(parseError, data: data);
        }

        if (!connectionStringParts.TryGetValue("accesskey", out var accessKey) ||
            string.IsNullOrWhiteSpace(accessKey))
        {
            return HealthCheckResult.Unhealthy(
                "Azure Communication Services access key is missing from the connection string.",
                data: data);
        }

        if (!connectionStringParts.TryGetValue("endpoint", out var connectionStringEndpoint) ||
            !TryCreateUri(connectionStringEndpoint, out var parsedConnectionStringEndpoint))
        {
            return HealthCheckResult.Unhealthy(
                "Azure Communication Services endpoint is missing from the connection string.",
                data: data);
        }

        data["connectionStringEndpoint"] = parsedConnectionStringEndpoint.ToString();

        if (!EndpointsMatch(configuredEndpoint, parsedConnectionStringEndpoint))
        {
            return HealthCheckResult.Unhealthy(
                "Azure Communication Services endpoint does not match the connection string endpoint.",
                data: data);
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, configuredEndpoint);
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            data["httpStatusCode"] = (int)response.StatusCode;

            if ((int)response.StatusCode >= 500)
            {
                return HealthCheckResult.Unhealthy(
                    "Azure Communication Services endpoint returned a server error.",
                    data: data);
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                return HealthCheckResult.Degraded(
                    "Azure Communication Services endpoint is reachable but rate limited.",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "Azure Communication Services endpoint is reachable and configuration is valid.",
                data);
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy(
                "Azure Communication Services endpoint is unreachable.",
                ex,
                data);
        }
        catch (TaskCanceledException ex)
        {
            return HealthCheckResult.Unhealthy(
                "Azure Communication Services endpoint check timed out.",
                ex,
                data);
        }
    }

    private static bool TryParseConnectionString(
        string connectionString,
        out Dictionary<string, string> parts,
        out string error)
    {
        parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == segment.Length - 1)
            {
                error = "Azure Communication Services connection string format is invalid.";
                return false;
            }

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                error = "Azure Communication Services connection string format is invalid.";
                return false;
            }

            parts[key] = value;
        }

        if (parts.Count == 0)
        {
            error = "Azure Communication Services connection string format is invalid.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryCreateUri(string? value, out Uri uri)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out uri!);
    }

    private static bool EndpointsMatch(Uri left, Uri right)
    {
        return string.Equals(
            NormalizeEndpoint(left),
            NormalizeEndpoint(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEndpoint(Uri uri)
    {
        return uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped)
            .TrimEnd('/');
    }
}
