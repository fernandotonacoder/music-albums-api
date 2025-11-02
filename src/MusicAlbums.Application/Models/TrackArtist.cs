namespace MusicAlbums.Application.Models;

/// <summary>
/// Represents the relationship between a track and an artist
/// </summary>
public class TrackArtist
{
    public required Guid TrackId { get; set; }
    
    public required Guid ArtistId { get; set; }
    
    /// <summary>
    /// Order of the artist in the track credits (1 = primary artist)
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Role of the artist (e.g., "Main Artist", "Featured", "Composer")
    /// </summary>
    public string Role { get; set; } = "Main Artist";
}
