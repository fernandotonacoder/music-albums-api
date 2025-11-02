using System.Data;
using Dapper;
using MusicAlbums.Application.Database;
using MusicAlbums.Application.Models;

namespace MusicAlbums.Application.Repositories;

public class MusicAlbumRepository(IDbConnectionFactory dbConnectionFactory) : IMusicAlbumRepository
{
    public async Task<bool> CreateAsync(MusicAlbum musicAlbum, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        using var transaction = connection.BeginTransaction();

        var result = await connection.ExecuteAsync(new CommandDefinition("""
            insert into music_albums (id, slug, title, year_of_release) 
            values (@Id, @Slug, @Title, @YearOfRelease)
            """, musicAlbum, cancellationToken: token));

        if (result > 0)
        {
            // Insert/link artists for album
            await UpsertAndLinkAlbumArtistsAsync(connection, musicAlbum.Id, musicAlbum.Artists, token);
            
            foreach (var genre in musicAlbum.Genres)
            {
                var genreId = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition("""
                    select id from genres where name = @Name
                    """, new { Name = genre }, cancellationToken: token));

                if (!genreId.HasValue)
                {
                    genreId = await connection.QuerySingleAsync<int>(new CommandDefinition("""
                        insert into genres (name) values (@Name)
                        returning id
                        """, new { Name = genre }, cancellationToken: token));
                }

                await connection.ExecuteAsync(new CommandDefinition("""
                    insert into music_album_genres (music_album_id, genre_id) 
                    values (@MusicAlbumId, @GenreId)
                    """, new { MusicAlbumId = musicAlbum.Id, GenreId = genreId.Value }, cancellationToken: token));
            }
            
            // Insert tracks
            foreach (var track in musicAlbum.Tracks)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    insert into tracks (id, music_album_id, title, track_number, duration_in_seconds)
                    values (@Id, @MusicAlbumId, @Title, @TrackNumber, @DurationInSeconds)
                    """, new
                {
                    Id = track.Id,
                    MusicAlbumId = musicAlbum.Id,
                    Title = track.Title,
                    TrackNumber = track.TrackNumber,
                    DurationInSeconds = track.DurationInSeconds
                }, cancellationToken: token));
                
                // Insert/link artists for track if specified
                if (track.Artists.Any())
                {
                    await UpsertAndLinkTrackArtistsAsync(connection, track.Id, track.Artists, token);
                }
            }
        }

        transaction.Commit();
        return result > 0;
    }

    private async Task UpsertAndLinkAlbumArtistsAsync(IDbConnection connection, Guid albumId, 
        List<Artist> artists, CancellationToken token)
    {
        for (int i = 0; i < artists.Count; i++)
        {
            var artist = artists[i];
            
            // Check if artist exists by name
            var existingArtistId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition("""
                select id from artists where LOWER(name) = LOWER(@Name)
                """, new { artist.Name }, cancellationToken: token));

            Guid artistId;
            if (existingArtistId.HasValue)
            {
                artistId = existingArtistId.Value;
            }
            else
            {
                // Insert new artist
                artistId = artist.Id;
                await connection.ExecuteAsync(new CommandDefinition("""
                    insert into artists (id, name, slug)
                    values (@Id, @Name, @Slug)
                    """, new
                {
                    Id = artistId,
                    artist.Name,
                    artist.Slug
                }, cancellationToken: token));
            }

            // Link artist to album
            await connection.ExecuteAsync(new CommandDefinition("""
                insert into album_artists (album_id, artist_id, artist_order, role)
                values (@AlbumId, @ArtistId, @Order, @Role)
                ON CONFLICT (album_id, artist_id) DO NOTHING
                """, new
            {
                AlbumId = albumId,
                ArtistId = artistId,
                Order = i + 1,
                Role = "Main Artist"
            }, cancellationToken: token));
        }
    }

    private async Task UpsertAndLinkTrackArtistsAsync(IDbConnection connection, Guid trackId, 
        List<Artist> artists, CancellationToken token)
    {
        for (int i = 0; i < artists.Count; i++)
        {
            var artist = artists[i];
            
            // Check if artist exists by name
            var existingArtistId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition("""
                select id from artists where LOWER(name) = LOWER(@Name)
                """, new { artist.Name }, cancellationToken: token));

            Guid artistId;
            if (existingArtistId.HasValue)
            {
                artistId = existingArtistId.Value;
            }
            else
            {
                // Insert new artist
                artistId = artist.Id;
                await connection.ExecuteAsync(new CommandDefinition("""
                    insert into artists (id, name, slug)
                    values (@Id, @Name, @Slug)
                    """, new
                {
                    Id = artistId,
                    artist.Name,
                    artist.Slug
                }, cancellationToken: token));
            }

            // Link artist to track
            await connection.ExecuteAsync(new CommandDefinition("""
                insert into track_artists (track_id, artist_id, artist_order, role)
                values (@TrackId, @ArtistId, @Order, @Role)
                ON CONFLICT (track_id, artist_id) DO NOTHING
                """, new
            {
                TrackId = trackId,
                ArtistId = artistId,
                Order = i + 1,
                Role = i == 0 ? "Main Artist" : "Featured"
            }, cancellationToken: token));
        }
    }

    public async Task<MusicAlbum?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);

        var albumDictionary = new Dictionary<Guid, MusicAlbum>();
        var result = await connection.QueryAsync<MusicAlbum, string, MusicAlbum>(new CommandDefinition("""
        select ma.id as Id,
               ma.slug as Slug,
               ma.title as Title,
               ma.year_of_release as YearOfRelease,
               g.name 
        from music_albums ma 
        left join music_album_genres mag on ma.id = mag.music_album_id
        left join genres g on mag.genre_id = g.id
        where ma.id = @Id
        """, new { Id = id }, cancellationToken: token),
            (album, genre) =>
            {
                if (!albumDictionary.TryGetValue(album.Id, out var albumEntry))
                {
                    albumEntry = album;
                    albumEntry.Genres = [];
                    albumDictionary.Add(albumEntry.Id, albumEntry);
                }

                if (!string.IsNullOrEmpty(genre))
                {
                    albumEntry.Genres.Add(genre);
                }

                return albumEntry;
            },
            splitOn: "name");

        var foundAlbum = result.FirstOrDefault();

        if (foundAlbum is null)
        {
            return null;
        }

        // Load artists for album
        var albumArtists = await connection.QueryAsync<Artist>(new CommandDefinition("""
            select a.id as Id, a.name as Name, a.slug as Slug
            from artists a
            join album_artists aa on a.id = aa.artist_id
            where aa.album_id = @Id
            order by aa.artist_order
            """, new { Id = id }, cancellationToken: token));
        
        foundAlbum.Artists = albumArtists.ToList();

        // Load tracks
        var tracks = await connection.QueryAsync<Track>(new CommandDefinition("""
            select id as Id,
                   music_album_id as MusicAlbumId,
                   title as Title,
                   track_number as TrackNumber,
                   duration_in_seconds as DurationInSeconds
            from tracks
            where music_album_id = @Id
            order by track_number
            """, new { Id = id }, cancellationToken: token));
        
        foundAlbum.Tracks = tracks.ToList();

        // Load artists for each track
        if (foundAlbum.Tracks.Any())
        {
            var trackIds = foundAlbum.Tracks.Select(t => t.Id).ToArray();
            var trackArtistsData = await connection.QueryAsync<Guid, Artist, (Guid TrackId, Artist Artist)>(
                new CommandDefinition("""
                select ta.track_id, a.id as Id, a.name as Name, a.slug as Slug
                from artists a
                join track_artists ta on a.id = ta.artist_id
                where ta.track_id = ANY(@TrackIds)
                order by ta.artist_order
                """, new { TrackIds = trackIds }, cancellationToken: token),
                (trackId, artist) => (trackId, artist),
                splitOn: "Id");

            var trackArtistsByTrack = trackArtistsData
                .GroupBy(x => x.TrackId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Artist).ToList());

            foreach (var track in foundAlbum.Tracks)
            {
                track.Artists = trackArtistsByTrack.GetValueOrDefault(track.Id, new List<Artist>());
            }
        }

        if (userId.HasValue)
        {
            var rating = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition("""
            select rating from ratings where music_album_id = @music_album_id and user_id = @user_id
            """, new { music_album_id = id, user_id = userId.Value }, cancellationToken: token));
            foundAlbum.UserRating = rating;
        }
        return foundAlbum;
    }

    public async Task<MusicAlbum?> GetBySlugAsync(string slug, Guid? userId = default, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        var albumDictionary = new Dictionary<Guid, MusicAlbum>();
        var result = await connection.QueryAsync<MusicAlbum, string, MusicAlbum>(new CommandDefinition("""
        select ma.id as Id,
               ma.slug as Slug,
               ma.title as Title,
               ma.year_of_release as YearOfRelease,
               g.name 
        from music_albums ma 
        left join music_album_genres mag on ma.id = mag.music_album_id
        left join genres g on mag.genre_id = g.id
        where ma.slug = @Slug
        """, new { Slug = slug }, cancellationToken: token),
            (album, genre) =>
            {
                if (!albumDictionary.TryGetValue(album.Id, out var albumEntry))
                {
                    albumEntry = album;
                    albumEntry.Genres = [];
                    albumDictionary.Add(albumEntry.Id, albumEntry);
                }

                if (!string.IsNullOrEmpty(genre))
                {
                    albumEntry.Genres.Add(genre);
                }

                return albumEntry;
            },
            splitOn: "name");

        var foundAlbum = result.FirstOrDefault();

        if (foundAlbum is null)
        {
            return null;
        }

        // Load artists for album
        var albumArtists = await connection.QueryAsync<Artist>(new CommandDefinition("""
            select a.id as Id, a.name as Name, a.slug as Slug
            from artists a
            join album_artists aa on a.id = aa.artist_id
            where aa.album_id = @Id
            order by aa.artist_order
            """, new { Id = foundAlbum.Id }, cancellationToken: token));
        
        foundAlbum.Artists = albumArtists.ToList();

        // Load tracks
        var tracks = await connection.QueryAsync<Track>(new CommandDefinition("""
            select id as Id,
                   music_album_id as MusicAlbumId,
                   title as Title,
                   track_number as TrackNumber,
                   duration_in_seconds as DurationInSeconds
            from tracks
            where music_album_id = @Id
            order by track_number
            """, new { Id = foundAlbum.Id }, cancellationToken: token));
        
        foundAlbum.Tracks = tracks.ToList();

        // Load artists for each track
        if (foundAlbum.Tracks.Any())
        {
            var trackIds = foundAlbum.Tracks.Select(t => t.Id).ToArray();
            var trackArtistsData = await connection.QueryAsync<Guid, Artist, (Guid TrackId, Artist Artist)>(
                new CommandDefinition("""
                select ta.track_id, a.id as Id, a.name as Name, a.slug as Slug
                from artists a
                join track_artists ta on a.id = ta.artist_id
                where ta.track_id = ANY(@TrackIds)
                order by ta.artist_order
                """, new { TrackIds = trackIds }, cancellationToken: token),
                (trackId, artist) => (trackId, artist),
                splitOn: "Id");

            var trackArtistsByTrack = trackArtistsData
                .GroupBy(x => x.TrackId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Artist).ToList());

            foreach (var track in foundAlbum.Tracks)
            {
                track.Artists = trackArtistsByTrack.GetValueOrDefault(track.Id, new List<Artist>());
            }
        }

        if (userId.HasValue)
        {
            var rating = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition("""
            select rating from ratings where music_album_id = @music_album_id and user_id = @user_id
            """, new { music_album_id = foundAlbum.Id, user_id = userId.Value }, cancellationToken: token));

            foundAlbum.UserRating = rating;
        }

        return foundAlbum;
    }

    public async Task<IEnumerable<MusicAlbum>> GetAllAsync(GetAllMusicAlbumsOptions options, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);

        var sqlBuilder = new SqlBuilder();
        var template = sqlBuilder.AddTemplate("""
        select distinct ma.id as Id,
               ma.slug as Slug,
               ma.title as Title,
               ma.year_of_release as YearOfRelease,
               round(avg(r.rating), 1) as Rating, 
               user_rating.rating as UserRating
        from music_albums ma 
        left join ratings r on ma.id = r.music_album_id
        left join (select * from ratings where user_id = @userId) as user_rating
        on ma.id = user_rating.music_album_id
        /**where**/
        group by ma.id, ma.slug, ma.title, ma.year_of_release, user_rating.rating
        /**orderby**/
        limit @pageSize
        offset @offset
        """);

        if (!string.IsNullOrEmpty(options.Title))
        {
            sqlBuilder.Where("ma.title like '%' || @title || '%'", new { title = options.Title });
        }

        if (options.YearOfRelease.HasValue)
        {
            sqlBuilder.Where("ma.year_of_release = @yearOfRelease", new { yearOfRelease = options.YearOfRelease.Value });
        }

        if (options.SortField is not null && options.SortOrder != SortOrder.Unsorted)
        {
            var sortDirection = options.SortOrder switch
            {
                SortOrder.Ascending => "ASC",
                SortOrder.Descending => "DESC",
                _ => "ASC"
            };

            var dbSortField = options.SortField.ToLowerInvariant() switch
            {
                "yearofrelease" => "year_of_release",
                "title" => "title",
                _ => options.SortField
            };

            sqlBuilder.OrderBy($"{dbSortField} {sortDirection}");
        }

        var offset = (options.Page - 1) * options.PageSize;

        var parameters = new DynamicParameters(template.Parameters);
        parameters.Add("userId", options.UserId);
        parameters.Add("pageSize", options.PageSize);
        parameters.Add("offset", offset);

        var albums = await connection.QueryAsync<MusicAlbum>(
            new CommandDefinition(template.RawSql, parameters, cancellationToken: token));

        var albumList = albums.ToList();

        if (albumList.Count != 0)
        {
            var albumIds = albumList.Select(a => a.Id).ToArray();

            var genreData = await connection.QueryAsync<AlbumGenre>(new CommandDefinition("""
            select ma.id as AlbumId, g.name as GenreName
            from music_albums ma 
            join music_album_genres mag on ma.id = mag.music_album_id
            join genres g on mag.genre_id = g.id
            where ma.id = ANY(@albumIds)
            """, new { albumIds }, cancellationToken: token));

            var genresByAlbum = genreData
                .GroupBy(g => g.AlbumId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.GenreName).ToList());

            // Load artists for albums
            var artistData = await connection.QueryAsync<Guid, Artist, (Guid AlbumId, Artist Artist)>(
                new CommandDefinition("""
                select aa.album_id, a.id as Id, a.name as Name, a.slug as Slug
                from artists a
                join album_artists aa on a.id = aa.artist_id
                where aa.album_id = ANY(@albumIds)
                order by aa.artist_order
                """, new { albumIds }, cancellationToken: token),
                (albumId, artist) => (albumId, artist),
                splitOn: "Id");

            var artistsByAlbum = artistData
                .GroupBy(a => a.AlbumId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Artist).ToList());

            // Load tracks for albums
            var trackData = await connection.QueryAsync<Track>(new CommandDefinition("""
            select id as Id,
                   music_album_id as MusicAlbumId,
                   title as Title,
                   track_number as TrackNumber,
                   duration_in_seconds as DurationInSeconds
            from tracks
            where music_album_id = ANY(@albumIds)
            order by track_number
            """, new { albumIds }, cancellationToken: token));

            var tracksByAlbum = trackData
                .GroupBy(t => t.MusicAlbumId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Load artists for tracks if there are any tracks
            if (trackData.Any())
            {
                var trackIds = trackData.Select(t => t.Id).ToArray();
                var trackArtistsData = await connection.QueryAsync<Guid, Artist, (Guid TrackId, Artist Artist)>(
                    new CommandDefinition("""
                    select ta.track_id, a.id as Id, a.name as Name, a.slug as Slug
                    from artists a
                    join track_artists ta on a.id = ta.artist_id
                    where ta.track_id = ANY(@TrackIds)
                    order by ta.artist_order
                    """, new { TrackIds = trackIds }, cancellationToken: token),
                    (trackId, artist) => (trackId, artist),
                    splitOn: "Id");

                var trackArtistsByTrack = trackArtistsData
                    .GroupBy(x => x.TrackId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Artist).ToList());

                foreach (var track in trackData)
                {
                    track.Artists = trackArtistsByTrack.GetValueOrDefault(track.Id, new List<Artist>());
                }
            }

            foreach (var album in albumList)
            {
                album.Genres = genresByAlbum.GetValueOrDefault(album.Id, []);
                album.Artists = artistsByAlbum.GetValueOrDefault(album.Id, []);
                album.Tracks = tracksByAlbum.GetValueOrDefault(album.Id, []);
            }
        }

        return albumList;
    }

    public async Task<bool> UpdateAsync(MusicAlbum musicAlbum, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        using var transaction = connection.BeginTransaction();

        // Delete and re-insert artists
        await connection.ExecuteAsync(new CommandDefinition("""
        delete from album_artists where album_id = @id
        """, new { id = musicAlbum.Id }, cancellationToken: token));

        await UpsertAndLinkAlbumArtistsAsync(connection, musicAlbum.Id, musicAlbum.Artists, token);

        // Delete and re-insert genres
        await connection.ExecuteAsync(new CommandDefinition("""
        delete from music_album_genres where music_album_id = @id
        """, new { id = musicAlbum.Id }, cancellationToken: token));

        foreach (var genre in musicAlbum.Genres)
        {
            var genreId = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition("""
            select id from genres where name = @Name
            """, new { Name = genre }, cancellationToken: token));

            if (!genreId.HasValue)
            {
                genreId = await connection.QuerySingleAsync<int>(new CommandDefinition("""
                insert into genres (name) values (@Name)
                returning id
                """, new { Name = genre }, cancellationToken: token));
            }

            await connection.ExecuteAsync(new CommandDefinition("""
            insert into music_album_genres (music_album_id, genre_id) 
            values (@MusicAlbumId, @GenreId)
            """, new { MusicAlbumId = musicAlbum.Id, GenreId = genreId.Value }, cancellationToken: token));
        }

        // Delete existing tracks and insert new ones
        await connection.ExecuteAsync(new CommandDefinition("""
        delete from tracks where music_album_id = @id
        """, new { id = musicAlbum.Id }, cancellationToken: token));

        foreach (var track in musicAlbum.Tracks)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                insert into tracks (id, music_album_id, title, track_number, duration_in_seconds)
                values (@Id, @MusicAlbumId, @Title, @TrackNumber, @DurationInSeconds)
                """, new
            {
                Id = track.Id,
                MusicAlbumId = musicAlbum.Id,
                Title = track.Title,
                TrackNumber = track.TrackNumber,
                DurationInSeconds = track.DurationInSeconds
            }, cancellationToken: token));
            
            // Insert/link artists for track if specified
            if (track.Artists.Any())
            {
                await UpsertAndLinkTrackArtistsAsync(connection, track.Id, track.Artists, token);
            }
        }

        var result = await connection.ExecuteAsync(new CommandDefinition("""
        update music_albums 
        set slug = @Slug, 
            title = @Title, 
            year_of_release = @YearOfRelease
        where id = @Id
        """, new
        {
            Id = musicAlbum.Id,
            Slug = musicAlbum.Slug,
            Title = musicAlbum.Title,
            YearOfRelease = musicAlbum.YearOfRelease
        }, cancellationToken: token));

        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        using var transaction = connection.BeginTransaction();

        var result = await connection.ExecuteAsync(new CommandDefinition("""
            delete from music_albums where id = @id
            """, new { id }, cancellationToken: token));

        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            select count(1) from music_albums where id = @id
            """, new { id }, cancellationToken: token));
    }

    public async Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        return await connection.QuerySingleAsync<int>(new CommandDefinition("""
            select count(id) from music_albums
            where (@title is null or title like ('%' || @title || '%'))
            and  (@yearOfRelease is null or year_of_release = @yearOfRelease)
            """, new
        {
            title,
            yearOfRelease
        }, cancellationToken: token));
    }
}