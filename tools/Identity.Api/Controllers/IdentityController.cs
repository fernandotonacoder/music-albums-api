using Identity.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Identity.Api.Controllers;

/// <summary>
///     Exposes endpoints for generating JWT tokens used by local development and API testing workflows.
/// </summary>
[ApiController]
[Route("")]
public class IdentityController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    /// <summary>
    ///     Initializes a new instance of the <see cref="IdentityController"/>.
    /// </summary>
    /// <param name="configuration">Application configuration used for JWT issuer and audience values.</param>
    public IdentityController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    ///     Generates a signed JWT for API testing and local development scenarios.
    /// </summary>
    /// <param name="request">Token request containing user information and custom claims.</param>
    /// <returns>
    ///     A 200 OK response with the generated token; or 400 Bad Request when the supplied user ID is empty.
    /// </returns>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GenerateToken(
        [FromBody] TokenGenerationRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("Valid userId is required");

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenSecret = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key must be configured (user-secrets or configuration).");
        if (tokenSecret.Length < 32)
            throw new InvalidOperationException(
                "JWT_KEY must be at least 32 characters long.");
        var key = Encoding.UTF8.GetBytes(tokenSecret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, request.Email ?? "unknown@test.com"),
            new(JwtRegisteredClaimNames.Email, request.Email ?? "unknown@test.com"),
            new("userid", request.UserId.ToString())
        };

        foreach (var claimPair in request.CustomClaims)
        {
            var jsonElement = (JsonElement)claimPair.Value;
            var valueType = jsonElement.ValueKind switch
            {
                JsonValueKind.True => ClaimValueTypes.Boolean,
                JsonValueKind.False => ClaimValueTypes.Boolean,
                JsonValueKind.Number => ClaimValueTypes.Double,
                _ => ClaimValueTypes.String
            };

            var claimValue = claimPair.Value.ToString() ?? string.Empty;
            var claim = new Claim(claimPair.Key, claimValue, valueType);
            claims.Add(claim);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TokenLifetime),
            Issuer = _configuration["Jwt:Issuer"] ?? "MusicAlbumsIdentity",
            Audience = _configuration["Jwt:Audience"] ?? "MusicAlbumsApi",
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature) //NOSONAR
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        CryptographicOperations.ZeroMemory(key);

        var jwt = tokenHandler.WriteToken(token);
        return Ok(new TokenResponse { Token = jwt });
    }
}
