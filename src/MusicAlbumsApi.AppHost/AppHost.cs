var builder = DistributedApplication.CreateBuilder(args);

// Secrets and API keys (sourced from AppHost user-secrets via configuration)
var jwtKey = builder.AddParameterFromConfiguration("jwt-key", "jwt-key", secret: true);
var apiKey = builder.AddParameterFromConfiguration("api-key", "api-key", secret: true);
var pgPassword = builder.AddParameterFromConfiguration("pg-password", "pg-password", secret: true);

// Aspire-managed PostgreSQL: container survives AppHost stops (Persistent lifetime),
// data survives container recreation (named volume). Reset = `docker volume rm musicalbums-postgres-data`.
var db = builder.AddPostgres("musicalbums-postgres", password: pgPassword, port: 5433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("musicalbums-postgres-data")
    .AddDatabase("albums");

// Music Albums API: main service
_ = builder.AddProject<Projects.MusicAlbums_Api>("musicalbums-api")
    .WithEnvironment("Database:ConnectionString", db.Resource.ConnectionStringExpression)
    .WithEnvironment("Jwt:Key", jwtKey)
    .WithEnvironment("Jwt:Issuer", "MusicAlbumsIdentity")
    .WithEnvironment("Jwt:Audience", "MusicAlbumsApi")
    .WithEnvironment("ApiKey", apiKey)
    .WithHttpEndpoint(port: 5001, name: "http")
    .WithHttpsEndpoint(port: 5002, name: "https")
    .WaitFor(db);

// Identity API: helper service for auth (runs in same local environment)
_ = builder.AddProject<Projects.Identity_Api>("identity-api")
    .WithEnvironment("Jwt:Key", jwtKey)
    .WithEnvironment("Jwt:Issuer", "MusicAlbumsIdentity")
    .WithEnvironment("Jwt:Audience", "MusicAlbumsApi")
    .WithHttpEndpoint(port: 5003, name: "http")
    .WithHttpsEndpoint(port: 5004, name: "https");

await builder.Build().RunAsync();
