using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace MusicAlbums.Api.Auth;

public class AdminAuthHandler(IConfiguration configuration, ILogger<AdminAuthHandler> logger)
    : AuthorizationHandler<AdminAuthRequirement>
{
    private readonly string _apiKey = configuration["ApiKey"]
                                      ?? throw new InvalidOperationException(
                                          "API_KEY must be configured (user-secrets or configuration).");

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminAuthRequirement requirement)
    {
        if (context.User.HasClaim(AuthConstants.AdminUserClaimName, "true"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var httpContext = context.Resource as HttpContext;
        if (httpContext is null)
            return Task.CompletedTask;

        if (!httpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeaderName, out var extractedApiKey))
        {
            logger.LogWarning("Admin access denied: missing API key header on {Path}.", httpContext.Request.Path);
            context.Fail();
            return Task.CompletedTask;
        }

        if (_apiKey != extractedApiKey)
        {
            logger.LogWarning("Admin access denied: invalid API key on {Path}.", httpContext.Request.Path);
            context.Fail();
            return Task.CompletedTask;
        }

        var identity = (ClaimsIdentity)httpContext.User.Identity!;
        identity.AddClaim(new Claim("userid", Guid.Parse("74e20de1-8dd0-4bc2-a9f5-8aa3203ad209").ToString()));
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}