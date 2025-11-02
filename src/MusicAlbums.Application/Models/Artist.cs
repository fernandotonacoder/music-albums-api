using System.Text.RegularExpressions;

namespace MusicAlbums.Application.Models;

public partial class Artist
{
    public required Guid Id { get; init; }
    
    public required string Name { get; set; }
    
    /// <summary>
    /// Slug for URL-friendly artist identification (e.g., "the-beatles")
    /// </summary>
    public string Slug => GenerateSlug();
    
    private string GenerateSlug()
    {
        var slug = SlugRegex().Replace(Name, string.Empty)
            .ToLower()
            .Replace(" ", "-")
            .Replace("&", "and");
        return slug;
    }

    [GeneratedRegex("[^0-9A-Za-z _&-]", RegexOptions.NonBacktracking, 5)]
    private static partial Regex SlugRegex();
}
