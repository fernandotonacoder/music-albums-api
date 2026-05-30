using System.Data.Common;
using Aspire.Hosting;
using Npgsql;
using Projects;
using Respawn;

namespace MusicAlbums.Tests.Integration;

public class DistributedAppFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(180);

    private DistributedApplication _app = null!;
    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    public HttpClient MusicAlbumsApiClient { get; private set; } = null!;
    public HttpClient IdentityApiClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<MusicAlbums_AppHost>(
                ["--TestMode=true"],
                TestContext.Current.CancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        _app = await appHost.BuildAsync(TestContext.Current.CancellationToken);
        await _app.StartAsync(TestContext.Current.CancellationToken);

        await WaitForResourceHealthyAsync("musicalbums-api");
        await WaitForResourceHealthyAsync("identity-api");

        // Use the HTTP endpoints: CI agents have no trusted dev certificate, so HTTPS
        // calls would fail the TLS handshake (see the test-mode endpoints in AppHost.cs).
        MusicAlbumsApiClient = _app.CreateHttpClient("musicalbums-api", "http");
        IdentityApiClient = _app.CreateHttpClient("identity-api", "http");

        await InitializeDatabaseConnectionAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    // Waits for a resource to report healthy, but on timeout surfaces the resource's last
    // known state and health-check failures instead of a bare "operation timed out" — so a
    // CI failure (e.g. an unhealthy HTTPS probe) is diagnosable straight from the message.
    private async Task WaitForResourceHealthyAsync(string resourceName)
    {
        try
        {
            await _app.ResourceNotifications
                .WaitForResourceHealthyAsync(resourceName, TestContext.Current.CancellationToken)
                .WaitAsync(StartupTimeout, TestContext.Current.CancellationToken);
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException(
                $"Resource '{resourceName}' did not become healthy within " +
                $"{StartupTimeout.TotalSeconds:0}s. Last known state:{Environment.NewLine}" +
                await DescribeResourceAsync(resourceName),
                ex);
        }
    }

    private async Task<string> DescribeResourceAsync(string resourceName)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            // WatchAsync replays the latest snapshot on subscribe, so a match-anything
            // predicate returns the current state almost immediately.
            var resourceEvent = await _app.ResourceNotifications
                .WaitForResourceAsync(resourceName, _ => true, cts.Token);

            var snapshot = resourceEvent.Snapshot;
            var lines = new List<string>
            {
                $"  state:  {snapshot.State?.Text ?? "unknown"}",
                $"  health: {snapshot.HealthStatus?.ToString() ?? "unknown"}",
            };

            foreach (var report in snapshot.HealthReports)
            {
                var reason = report.ExceptionText ?? report.Description;
                lines.Add($"  health check '{report.Name}': {report.Status}" +
                    (string.IsNullOrWhiteSpace(reason) ? "" : $" — {reason.Trim()}"));
            }

            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            return $"  (could not read resource state: {ex.Message})";
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Null-guarded so that if InitializeAsync throws partway through, cleanup doesn't
        // raise a NullReferenceException that masks the original failure.
        MusicAlbumsApiClient?.Dispose();
        IdentityApiClient?.Dispose();

        if (_dbConnection is not null)
        {
            await _dbConnection.DisposeAsync();
        }

        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    private async Task InitializeDatabaseConnectionAsync()
    {
        var connectionString = await _app.GetConnectionStringAsync(
            "albums", TestContext.Current.CancellationToken);

        _dbConnection = new NpgsqlConnection(connectionString);
        await _dbConnection.OpenAsync(TestContext.Current.CancellationToken);

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            SchemasToInclude = ["public"],
            DbAdapter = DbAdapter.Postgres
        });
    }
}
