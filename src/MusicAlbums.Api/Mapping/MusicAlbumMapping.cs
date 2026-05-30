using MusicAlbums.Application.Models;
using MusicAlbums.Contracts.Requests;
using MusicAlbums.Contracts.Responses;

namespace MusicAlbums.Api.Mapping;

public static class MusicAlbumMapping
{
    public static MusicAlbum MapToMusicAlbum(this CreateMusicAlbumRequest request)
    {
        var album = new MusicAlbum
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            YearOfRelease = request.YearOfRelease,
            Genres = request.Genres.ToList(),
            Artists = request.ArtistNames.Select(name => new Artist
            {
                Id = Guid.NewGuid(),
                Name = name
            }).ToList()
        };
        
        album.Tracks = request.Tracks.Select(t => new Track
        {
            Id = Guid.NewGuid(),
            MusicAlbumId = album.Id,
            Title = t.Title,
            TrackNumber = t.TrackNumber,
            DurationInSeconds = t.DurationInSeconds,
            // If track has artists, use them; otherwise inherit from album
            Artists = t.ArtistNames.Any() 
                ? t.ArtistNames.Select(name => new Artist
                {
                    Id = Guid.NewGuid(),
                    Name = name
                }).ToList()
                : album.Artists.ToList()
        }).ToList();
        
        return album;
    }
    
    public static MusicAlbum MapToAlbum(this UpdateMusicAlbumRequest request, Guid id)
    {
        var album = new MusicAlbum
        {
            Id = id,
            Title = request.Title,
            YearOfRelease = request.YearOfRelease,
            Genres = request.Genres.ToList(),
            Artists = request.ArtistNames.Select(name => new Artist
            {
                Id = Guid.NewGuid(),
                Name = name
            }).ToList()
        };
        
        album.Tracks = request.Tracks.Select(t => new Track
        {
            Id = Guid.NewGuid(),
            MusicAlbumId = id,
            Title = t.Title,
            TrackNumber = t.TrackNumber,
            DurationInSeconds = t.DurationInSeconds,
            // If track has artists, use them; otherwise inherit from album
            Artists = t.ArtistNames.Any() 
                ? t.ArtistNames.Select(name => new Artist
                {
                    Id = Guid.NewGuid(),
                    Name = name
                }).ToList()
                : album.Artists.ToList()
        }).ToList();
        
        return album;
    }

    public static MusicAlbumResponse MapToResponse(this MusicAlbum musicAlbum)
    {
        return new MusicAlbumResponse
        {
            Id = musicAlbum.Id,
            Title = musicAlbum.Title,
            Slug = musicAlbum.Slug,
            Rating = musicAlbum.Rating,
            UserRating = musicAlbum.UserRating,
            YearOfRelease = musicAlbum.YearOfRelease,
            Genres = musicAlbum.Genres,
            Artists = musicAlbum.Artists.Select(a => new ArtistResponse
            {
                Id = a.Id,
                Name = a.Name,
                Slug = a.Slug
            }).ToList(),
            Tracks = musicAlbum.Tracks.Select(t => new TrackResponse
            {
                Id = t.Id,
                Title = t.Title,
                TrackNumber = t.TrackNumber,
                DurationInSeconds = t.DurationInSeconds,
                Artists = t.Artists.Select(a => new ArtistResponse
                {
                    Id = a.Id,
                    Name = a.Name,
                    Slug = a.Slug
                }).ToList()
            }).OrderBy(t => t.TrackNumber)
        };
    }

    public static MusicAlbumsResponse MapToResponse(this IEnumerable<MusicAlbum> albums,
        int page, int pageSize, int totalCount)
    {
        return new MusicAlbumsResponse
        {
            Items = albums.Select(MapToResponse),
            Page = page,
            PageSize = pageSize,
            Total = totalCount
        };
    }
    
    public static IEnumerable<MusicAlbumRatingResponse> MapToResponse(this IEnumerable<MusicAlbumRating> ratings)
    {
        return ratings.Select(x => new MusicAlbumRatingResponse
        {
            Rating = x.Rating,
            Slug = x.Slug,
            AlbumId = x.AlbumId
        });
    }

    public static GetAllMusicAlbumsOptions MapToOptions(this GetAllMusicAlbumsRequest request)
    {
        var sortOrder = SortOrder.Unsorted;
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            sortOrder = request.SortBy.StartsWith('-')
                ? SortOrder.Descending
                : SortOrder.Ascending;
        }

        return new GetAllMusicAlbumsOptions
        {
            Title = request.Title,
            YearOfRelease = request.Year,
            SortField = request.SortBy?.Trim('+', '-'),
            SortOrder = sortOrder,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public static GetAllMusicAlbumsOptions WithUser(this GetAllMusicAlbumsOptions options,
        Guid? userId)
    {
        options.UserId = userId;
        return options;
    }
}