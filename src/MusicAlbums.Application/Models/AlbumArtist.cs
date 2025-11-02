namespace MusicAlbums.Application.Models;

/// <summary>
/// Represents the relationship between an album and an artist
/// </summary>
public class AlbumArtist
{
    public required Guid AlbumId { get; set; }
    
    public required Guid ArtistId { get; set; }
    
    /// <summary>
    /// Order of the artist in the album credits (1 = primary artist)
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Role of the artist (e.g., "Main Artist", "Featured", "Composer", "Producer")
    /// </summary>
    public string Role { get; set; } = "Main Artist";
}

