namespace MusicAlbums.Contracts.Requests;

public class CreateTrackRequest
{
    public required string Title { get; init; }
    
    public required int TrackNumber { get; init; }
    
    public int? DurationInSeconds { get; init; }
    
    /// <summary>
    /// Artists for this specific track (e.g., featured artists).
    /// If empty, inherits album artists.
    /// </summary>
    public IEnumerable<string> ArtistNames { get; init; } = [];
}
