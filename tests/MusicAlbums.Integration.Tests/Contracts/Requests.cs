namespace MusicAlbums.Tests.Integration.Contracts;

public record CreateArtistRequest(string Name);

public record CreateMusicAlbumRequest(
    string Title,
    int YearOfRelease,
    IEnumerable<string> Genres,
    IEnumerable<string> ArtistNames,
    IEnumerable<CreateTrackRequest>? Tracks = null);

public record CreateTrackRequest(
    string Title,
    int TrackNumber,
    int? DurationInSeconds = null,
    IEnumerable<string>? ArtistNames = null);

public record GetAllMusicAlbumsRequest(
    string? Title,
    int? Year,
    string? SortBy,
    int Page = 1,
    int PageSize = 10);

public record PagedRequest(int Page = 1, int PageSize = 10);

public record RateMusicAlbumRequest(int Rating);

public record UpdateMusicAlbumRequest(
    string Title,
    int YearOfRelease,
    IEnumerable<string> Genres,
    IEnumerable<string> ArtistNames,
    IEnumerable<CreateTrackRequest>? Tracks = null);
