namespace MusicAlbums.Application.Models;

public class MusicAlbumRating
{
    public required Guid AlbumId { get; init; }
    
    public required string Slug { get; init; }
    
    public required int Rating { get; init; }
}