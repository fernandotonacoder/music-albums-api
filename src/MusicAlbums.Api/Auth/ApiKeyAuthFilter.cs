using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MusicAlbums.Api.Auth;

public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private readonly string _apiKey;

    public ApiKeyAuthFilter()
    {
        _apiKey = Environment.GetEnvironmentVariable("API_KEY")
            ?? throw new InvalidOperationException("API_KEY environment variable is not configured.");
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeaderName,
                out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("API Key missing");
            return;
        }

        if (_apiKey != extractedApiKey)
        {
            context.Result = new UnauthorizedObjectResult("Invalid API Key");
        }
    }
}


