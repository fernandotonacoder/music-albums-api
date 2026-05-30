using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Contracts;

public class TokenGenerationRequest
{
    [Required]
    public Guid UserId { get; set; }
    
    public string? Email { get; set; }

    public Dictionary<string, object> CustomClaims { get; set; } = new();
}