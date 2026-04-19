using LMS_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LMS_Backend.Infrastructure.HealthChecks;

public sealed class ApplicationDbContextHealthCheck : IHealthCheck
{
    private readonly ApplicationDBContext _dbContext;

    public ApplicationDbContextHealthCheck(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = CreateData();

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("EF Core database connection is available.", data)
                : HealthCheckResult.Unhealthy("EF Core database connection is unavailable.", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "EF Core database connection check failed.",
                ex,
                data);
        }
    }

    private Dictionary<string, object> CreateData()
    {
        var data = new Dictionary<string, object>
        {
            ["provider"] = _dbContext.Database.ProviderName ?? "unknown"
        };

        try
        {
            var connection = _dbContext.Database.GetDbConnection();

            if (!string.IsNullOrWhiteSpace(connection.DataSource))
            {
                data["dataSource"] = connection.DataSource;
            }

            if (!string.IsNullOrWhiteSpace(connection.Database))
            {
                data["database"] = connection.Database;
            }
        }
        catch
        {
            // Connection metadata is best-effort only.
        }

        return data;
    }
}
