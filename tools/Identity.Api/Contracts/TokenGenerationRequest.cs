using System.Text.Json.Serialization;

namespace Identity.Api.Contracts;

public class TokenGenerationRequest
{
    [JsonRequired]
    public Guid UserId { get; set; }
    
    public string? Email { get; set; }

    public Dictionary<string, object> CustomClaims { get; set; } = new();
}