using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MusicAlbums.Api.Auth;
using MusicAlbums.Api.Health;
using MusicAlbums.Api.Mapping;
using MusicAlbums.Api.Swagger;
using MusicAlbums.Application;
using MusicAlbums.Application.Database;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// User Secrets (local dev) and Environment Variables (production) are loaded automatically
var config = builder.Configuration;

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new InvalidOperationException("JWT_KEY environment variable is not configured.");
if (jwtKey.Length < 32)
    throw new InvalidOperationException("JWT_KEY must be at least 32 characters long.");
var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);

var apiKey = Environment.GetEnvironmentVariable("API_KEY")
    ?? throw new InvalidOperationException("API_KEY environment variable is not configured.");

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

CryptographicOperations.ZeroMemory(jwtKeyBytes);

builder.Services.AddAuthorization(x =>
{
    x.AddPolicy(AuthConstants.AdminUserPolicyName,
        p => p.AddRequirements(new AdminAuthRequirement(apiKey)));

    x.AddPolicy(AuthConstants.TrustedMemberPolicyName,
        p => p.RequireAssertion(c =>
            c.User.HasClaim(m => m is { Type: AuthConstants.AdminUserClaimName, Value: "true" }) ||
            c.User.HasClaim(m => m is { Type: AuthConstants.TrustedMemberClaimName, Value: "true" })));
});

builder.Services.AddScoped<ApiKeyAuthFilter>();

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

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(x => x.OperationFilter<SwaggerDefaultValues>());

builder.Services.AddApplication();
builder.Services.AddDatabase(config["Database:ConnectionString"]!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(x =>
    {
        foreach (var groupName in app.DescribeApiVersions().Select(description => description.GroupName))
        {
            x.SwaggerEndpoint($"/swagger/{groupName}/swagger.json", groupName);
        }
    });
}

app.MapHealthChecks("_health");

// Liveness probe - basic check that the app is running (no DB check)
app.MapHealthChecks("_health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // No checks - just confirms the app responds
});

// Readiness probe - confirms app can handle requests (includes DB check)
app.MapHealthChecks("_health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == DatabaseHealthCheck.Name
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

//app.UseCors();
app.UseOutputCache();

app.UseMiddleware<ValidationMappingMiddleware>();
app.MapControllers();

var dbInitializer = app.Services.GetRequiredService<DbInitializer>();
await dbInitializer.InitializeAsync(CancellationToken.None);

await app.RunAsync();
