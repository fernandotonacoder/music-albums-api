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

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("musicalbums-api")
            .WaitAsync(StartupTimeout);

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("identity-api")
            .WaitAsync(StartupTimeout);

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
