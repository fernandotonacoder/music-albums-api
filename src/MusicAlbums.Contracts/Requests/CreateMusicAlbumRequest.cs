namespace MusicAlbums.Contracts.Requests;

public class CreateMusicAlbumRequest
{
    public required string Title { get; init; }

    public required int YearOfRelease { get; init; }

    public required IEnumerable<string> Genres { get; init; } = [];
    
    /// <summary>
    /// Artist names for the album. Can be a single artist or multiple.
    /// Examples: ["The Beatles"], ["Jay-Z", "Kanye West"]
    /// </summary>
    public required IEnumerable<string> ArtistNames { get; init; } = [];
    
    public IEnumerable<CreateTrackRequest> Tracks { get; init; } = [];
}