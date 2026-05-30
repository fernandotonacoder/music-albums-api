using Aspire.Hosting.ApplicationModel;
using MusicAlbums.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var isTestMode = bool.TryParse(builder.Configuration["TestMode"], out var testMode) && testMode;

IResourceBuilder<ParameterResource> jwtKey;
IResourceBuilder<ParameterResource> apiKey;
IResourceBuilder<PostgresDatabaseResource> postgres;

if (isTestMode)
{
    (postgres, jwtKey, apiKey) = TestAppHostEmulator.AddTestResources(builder);
}
else
{
    jwtKey = builder.AddParameterFromConfiguration("jwt-key", "jwt-key", secret: true);
    apiKey = builder.AddParameterFromConfiguration("api-key", "api-key", secret: true);
    var pgPassword = builder.AddParameterFromConfiguration("pg-password", "pg-password", secret: true);

    // Aspire-managed PostgreSQL: container survives AppHost stops (Persistent lifetime),
    // data survives container recreation (named volume). Reset = `docker volume rm musicalbums-postgres-data`.
    postgres = builder.AddPostgres("musicalbums-postgres", password: pgPassword, port: 5433)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("musicalbums-postgres-data")
        .AddDatabase("albums");
}

var musicAlbumsApi = builder.AddProject<Projects.MusicAlbums_Api>("musicalbums-api")
    .WithEnvironment("Database:ConnectionString", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("Jwt:Key", jwtKey)
    .WithEnvironment("Jwt:Issuer", "MusicAlbumsIdentity")
    .WithEnvironment("Jwt:Audience", "MusicAlbumsApi")
    .WithEnvironment("ApiKey", apiKey)
    .WaitFor(postgres);

var identityApi = builder.AddProject<Projects.Identity_Api>("identity-api")
    .WithEnvironment("Jwt:Key", jwtKey)
    .WithEnvironment("Jwt:Issuer", "MusicAlbumsIdentity")
    .WithEnvironment("Jwt:Audience", "MusicAlbumsApi");

if (isTestMode)
{
    // Integration tests run on CI agents that have no trusted ASP.NET Core dev
    // certificate, so probing or calling an HTTPS endpoint fails the TLS handshake and
    // the API never reports healthy. The API's launch profile is HTTPS-only, so expose a
    // plain-HTTP endpoint (Aspire allocates the port) for the health check and the test
    // clients to use. identity-api's launch profile already exposes HTTP, so it needs no
    // change here.
    musicAlbumsApi.WithHttpEndpoint(name: "http");
}
else
{
    musicAlbumsApi
        .WithHttpEndpoint(port: 5001, name: "http")
        .WithHttpsEndpoint(port: 5002, name: "https");

    identityApi
        .WithHttpEndpoint(port: 5003, name: "http")
        .WithHttpsEndpoint(port: 5004, name: "https");
}

// Probe over HTTP so the readiness gate doesn't depend on a trusted dev certificate
// (see the test-mode endpoint note above). Both modes expose an endpoint named "http".
musicAlbumsApi.WithHttpHealthCheck("/_health/ready", endpointName: "http");

await builder.Build().RunAsync();
