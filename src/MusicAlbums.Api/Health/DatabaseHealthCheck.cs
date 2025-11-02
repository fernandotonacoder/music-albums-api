using Microsoft.Extensions.Diagnostics.HealthChecks;
using MusicAlbums.Application.Database;

namespace MusicAlbums.Api.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    public const string Name = "Database";
    
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IDbConnectionFactory dbConnectionFactory, ILogger<DatabaseHealthCheck> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        try
        {
            _ = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Database is unhealthy: {ErrorMessage}", e.Message);
            return HealthCheckResult.Unhealthy("Database is unhealthy", e);
        }
    }
}
