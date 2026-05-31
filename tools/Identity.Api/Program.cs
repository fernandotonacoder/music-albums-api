using MusicAlbums.ServiceDefaults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var jwtSecret = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_KEY must be configured (user-secrets or configuration) and at least 32 characters long.");
}

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

// Must run before anything reads the request scheme/host (OpenAPI servers, redirects, links):
// rewrites them from the reverse-proxy forwarded headers configured in AddServiceDefaults.
app.UseForwardedHeaders();

// Emit HSTS when running behind the Container Apps ingress (see UseHttpsHardening).
app.UseHttpsHardening();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Identity API")
            .WithClassicLayout()
            .ForceDarkMode()
            .WithProxy(null!);
    });
}

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
