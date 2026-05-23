var builder = DistributedApplication.CreateBuilder(args);
var useManagedPostgres = string.Equals(builder.Configuration["UseManagedPostgres"], "true", StringComparison.OrdinalIgnoreCase);

// Secrets and API keys (sourced from AppHost user-secrets via configuration)
var jwtKey = builder.AddParameterFromConfiguration("jwt-key", "jwt-key", secret: true);
var apiKey = builder.AddParameterFromConfiguration("api-key", "api-key", secret: true);

// Dashboard-visible label for the currently selected local DB mode.
_ = builder.AddParameter(
        "db-mode",
        useManagedPostgres ? "Aspire-managed ephemeral Postgres" : "Persisted docker-compose Postgres",
        publishValueAsDefault: true)
    .WithDescription("Current local database mode. Use `UseManagedPostgres=true` to switch to the disposable Aspire-managed database.", enableMarkdown: true)
    .WithIconName("Database");

// Music Albums API: main service
if (useManagedPostgres)
{
    var db = builder.AddPostgres("postgres")
        .AddDatabase("albums");

    _ = builder.AddProject<Projects.MusicAlbums_Api>("musicalbums-api")
        .WithEnvironment("Database:ConnectionString", db.Resource.ConnectionStringExpression)
        .WithEnvironment("Jwt:Key", jwtKey)
        .WithEnvironment("Jwt:Issuer", "MusicAlbumsIdentity")
        .WithEnvironment("Jwt:Audience", "MusicAlbumsApi")
        .WithEnvironment("ApiKey", apiKey)
        .WithHttpEndpoint(port: 5001, name: "http")
        .WithHttpsEndpoint(port: 5002, name: "https")
        .WaitFor(db);
}
else
{
    // Default to the persisted docker-compose database.
    var externalDatabase = builder.AddConnectionString("albums");

    _ = builder.AddProject<Projects.MusicAlbums_Api>("musicalbums-api")
        .WithEnvironment("Database:ConnectionString", externalDatabase.Resource.ConnectionStringExpression)
        .WithEnvironment("Jwt:Key", jwtKey)
        .WithEnvironment("Jwt:Issuer", "MusicAlbumsIdentity")
        .WithEnvironment("Jwt:Audience", "MusicAlbumsApi")
        .WithEnvironment("ApiKey", apiKey)
        .WithHttpEndpoint(port: 5001, name: "http")
        .WithHttpsEndpoint(port: 5002, name: "https");
}

// Identity API: helper service for auth (runs in same local environment)
_ = builder.AddProject<Projects.Identity_Api>("identity-api")
    .WithEnvironment("Jwt:Key", jwtKey)
    .WithEnvironment("Jwt:Issuer", "MusicAlbumsIdentity")
    .WithEnvironment("Jwt:Audience", "MusicAlbumsApi")
    .WithHttpEndpoint(port: 5003, name: "http")
    .WithHttpsEndpoint(port: 5004, name: "https");

await builder.Build().RunAsync();
