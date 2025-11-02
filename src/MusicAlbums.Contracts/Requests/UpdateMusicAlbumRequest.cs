namespace MusicAlbums.Contracts.Requests;

public class UpdateMusicAlbumRequest
{
    public required string Title { get; init; }

    public required int YearOfRelease { get; init; }

    public required IEnumerable<string> Genres { get; init; } = Enumerable.Empty<string>();
    
    public required IEnumerable<string> ArtistNames { get; init; } = [];
    
    public IEnumerable<CreateTrackRequest> Tracks { get; init; } = [];
}
