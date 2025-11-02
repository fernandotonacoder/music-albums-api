namespace MusicAlbums.Contracts.Responses;

public class ArtistResponse
{
    public required Guid Id { get; init; }
    
    public required string Name { get; init; }
    
    public required string Slug { get; init; }
}
