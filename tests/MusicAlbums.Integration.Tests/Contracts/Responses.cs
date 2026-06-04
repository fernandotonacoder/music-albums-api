namespace MusicAlbums.Tests.Integration.Contracts;

public record ArtistResponse(Guid Id, string Name, string Slug);

public record MusicAlbumRatingResponse(Guid AlbumId, string Slug, int Rating);

public record MusicAlbumResponse(
    Guid Id,
    string Title,
    string Slug,
    int YearOfRelease,
    float? Rating = null,
    int? UserRating = null,
    IEnumerable<string>? Genres = null,
    IEnumerable<ArtistResponse>? Artists = null,
    IEnumerable<TrackResponse>? Tracks = null);

public record MusicAlbumsResponse(
    IEnumerable<MusicAlbumResponse> Items,
    int PageSize,
    int Page,
    int Total) : PagedResponse<MusicAlbumResponse>(Items, PageSize, Page, Total);

public record PagedResponse<TResponse>(
    IEnumerable<TResponse> Items,
    int PageSize,
    int Page,
    int Total)
{
    public bool HasNextPage => Total > (Page * PageSize);
}

public record TrackResponse(
    Guid Id,
    string Title,
    int TrackNumber,
    int? DurationInSeconds = null,
    IEnumerable<ArtistResponse>? Artists = null);

public record ValidationFailureResponse(IEnumerable<ValidationResponse> Errors);

public record ValidationResponse(string PropertyName, string Message);
