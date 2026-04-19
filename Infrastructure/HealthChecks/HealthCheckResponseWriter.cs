using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace LMS_Backend.Infrastructure.HealthChecks;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static Task WriteFullResponseAsync(HttpContext context, HealthReport report)
    {
        var environment = context.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName ?? "Unknown";

        var payload = new
        {
            status = report.Status.ToString(),
            environment,
            machineName = Environment.MachineName,
            generatedAtUtc = DateTimeOffset.UtcNow,
            totalDuration = report.TotalDuration,
            traceId = context.TraceIdentifier,
            checks = report.Entries
                .OrderBy(entry => entry.Key)
                .ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration,
                        error = entry.Value.Exception?.Message,
                        data = entry.Value.Data
                    })
        };

        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }
}
