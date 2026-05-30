using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MusicAlbums.Api.Auth;
using MusicAlbums.Api.Health;
using MusicAlbums.Api.Mapping;
using MusicAlbums.Api.OpenApi;
using MusicAlbums.Application;
using MusicAlbums.Application.Database;
using MusicAlbums.ServiceDefaults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

builder.AddServiceDefaults();

var jwtKey = config["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("JWT_KEY must be configured (user-secrets or configuration) and be at least 32 characters long.");
var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);

var apiKey = config["ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
    throw new InvalidOperationException("API_KEY must be configured (user-secrets or configuration).");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = config["Jwt:Issuer"],
        ValidAudience = config["Jwt:Audience"],
        ValidateIssuer = true,
        ValidateAudience = true
    };
});

builder.Services.AddScoped<IAuthorizationHandler, AdminAuthHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthConstants.AdminUserPolicyName, p => p.AddRequirements(new AdminAuthRequirement()))
    .AddPolicy(AuthConstants.TrustedMemberPolicyName, p => p.RequireAssertion(c =>
            c.User.HasClaim(m => m is { Type: AuthConstants.AdminUserClaimName, Value: "true" }) ||
            c.User.HasClaim(m => m is { Type: AuthConstants.TrustedMemberClaimName, Value: "true" })));

builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddSingleton<IConfiguration>(config);

builder.Services.AddApiVersioning(x =>
{
    x.DefaultApiVersion = new ApiVersion(1.0);
    x.AssumeDefaultVersionWhenUnspecified = true;
    x.ReportApiVersions = true;
    x.ApiVersionReader = new MediaTypeApiVersionReader("api-version");
}).AddMvc().AddApiExplorer();

builder.Services.AddOutputCache(x =>
{
    x.AddBasePolicy(c => c.Cache());
    x.AddPolicy("AlbumCache", c =>
        c.Cache()
        .Expire(TimeSpan.FromMinutes(1))
        .SetVaryByQuery(new[] { "title", "year", "sortBy", "page", "pageSize" })
        .Tag("albums"));
    // Collapse repeated public hits to /_health/ready into one DB call per window.
    // Output cache only stores 200s, so an unhealthy result is never served stale.
    x.AddPolicy("HealthReadiness", c =>
        c.Cache().Expire(TimeSpan.FromSeconds(5)));
});

builder.Services.AddControllers();

// builder.Services.AddCors(options =>
// {
//     options.AddDefaultPolicy(builder =>
//     {
//         builder.AllowAnyOrigin()
//             .AllowAnyHeader()
//             .AllowAnyMethod();
//     });
// });

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecurityTransformer>();
    options.ShouldInclude = _ => true;
});

builder.Services.AddApplication();
builder.Services.AddDatabase(config["Database:ConnectionString"]!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Music Albums API")
            .WithClassicLayout()
            .ForceDarkMode()
            .AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme)
            .WithProxy(null!);
    });
}

// Liveness: process is up and responding. No dependency checks — a failing liveness
// probe restarts the container, which must not happen for a transient DB outage.
app.MapHealthChecks("_health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: dependencies (the database) are reachable, so the instance can serve
// traffic. A failing readiness probe pulls the instance from rotation without a restart.
app.MapHealthChecks("_health/ready", new HealthCheckOptions
{
    Predicate = check => check.Name == DatabaseHealthCheck.Name
}).CacheOutput("HealthReadiness");

// Skip HTTPS redirect in test mode: CI has no trusted dev cert, and test clients
// connect over plain HTTP. In production the container is HTTP-only, so this
// middleware is also a no-op there (no HTTPS port → UseHttpsRedirection skips itself).
var isTestMode = bool.TryParse(config["TestMode"], out var tm) && tm;
if (!isTestMode)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

//app.UseCors();
app.UseOutputCache();

app.UseMiddleware<ValidationMappingMiddleware>();
app.MapControllers();

var dbInitializer = app.Services.GetRequiredService<DbInitializer>();
await dbInitializer.InitializeAsync(CancellationToken.None);

await app.RunAsync();
