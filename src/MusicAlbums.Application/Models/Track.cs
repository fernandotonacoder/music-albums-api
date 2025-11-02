namespace MusicAlbums.Application.Models;

public class Track
{
    public required Guid Id { get; init; }
    
    public required Guid MusicAlbumId { get; init; }
    
    public required string Title { get; set; }
    
    public required int TrackNumber { get; set; }
    
    /// <summary>
    /// Duration in seconds
    /// </summary>
    public int? DurationInSeconds { get; set; }
    
    /// <summary>
    /// Artists associated with this track (for featured artists or different artists per track)
    /// </summary>
    public List<Artist> Artists { get; set; } = new();
}
