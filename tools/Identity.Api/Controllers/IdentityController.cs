using Identity.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Identity.Api.Controllers;

[ApiController]
[Route("")]
public class IdentityController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public IdentityController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GenerateToken(
        [FromBody] TokenGenerationRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("Valid userId is required");

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenSecret = _configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException(
                "Jwt:Secret is not correctly configured.");
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
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        var jwt = tokenHandler.WriteToken(token);
        return Ok(new TokenResponse { Token = jwt });
    }
}
