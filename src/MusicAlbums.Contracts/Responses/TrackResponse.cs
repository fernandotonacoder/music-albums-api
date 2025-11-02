namespace MusicAlbums.Contracts.Responses;

public class TrackResponse
{
    public required Guid Id { get; init; }
    
    public required string Title { get; init; }
    
    public required int TrackNumber { get; init; }
    
    public int? DurationInSeconds { get; init; }
    
    /// <summary>
    /// Artists for this track. If empty, use album artists.
    /// </summary>
    public IEnumerable<ArtistResponse> Artists { get; init; } = [];
}
