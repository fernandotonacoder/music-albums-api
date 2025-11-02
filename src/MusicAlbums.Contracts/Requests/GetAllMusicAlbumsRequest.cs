namespace MusicAlbums.Contracts.Requests;

public class GetAllMusicAlbumsRequest : PagedRequest
{
    public string? Title { get; init; }

    public int? Year { get; init; }
    
    public string? SortBy { get; init; }
}
