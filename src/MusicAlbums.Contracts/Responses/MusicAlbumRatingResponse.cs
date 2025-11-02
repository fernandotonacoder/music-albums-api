namespace MusicAlbums.Contracts.Responses;

public class MusicAlbumRatingResponse
{
    public required Guid AlbumId { get; init; }
    
    public required string Slug { get; init; }
    
    public required int Rating { get; init; }
}
