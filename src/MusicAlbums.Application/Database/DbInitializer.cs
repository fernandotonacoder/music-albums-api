using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MusicAlbums.Application.Database;

public class DbInitializer(IDbConnectionFactory dbConnectionFactory, ILogger<DbInitializer> logger)
{
    private readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(5),
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = args =>
            {
                logger.LogWarning(
                    "Database connection attempt {AttemptNumber} failed. Retrying in {RetryDelay}...",
                    args.AttemptNumber + 1,
                    args.RetryDelay);
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _retryPipeline.ExecuteAsync(
            static async (instance, token) =>
                await instance.ExecuteInitializationAsync(token), this, cancellationToken);
    }

    #region private tables creation methods
    
    private async ValueTask ExecuteInitializationAsync(CancellationToken token)
    {
        logger.LogInformation("Attempting to connect to database and initialize schema...");

        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        
        await SetMusicAlbumsTable(connection);
        await SetGenresTable(connection);
        await SetArtistsTable(connection);
        
        await SetMusicAlbumGenresTable(connection);
        await SetTracksTable(connection);
        await SetAlbumArtistsTable(connection);
        await SetTrackArtistsTable(connection);
        
        await SetRatingsTable(connection);

        logger.LogInformation("Database schema initialized successfully.");
    }
    
    private static async Task SetMusicAlbumsTable(IDbConnection connection)
    {
        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {DatabaseTables.MusicAlbums} (
                id UUID PRIMARY KEY,
                slug TEXT NOT NULL, 
                title TEXT NOT NULL,
                year_of_release INTEGER NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );
        """);

        await connection.ExecuteAsync($"""
            CREATE UNIQUE INDEX IF NOT EXISTS {DatabaseTables.MusicAlbums}_slug_idx
            ON {DatabaseTables.MusicAlbums}
            USING btree(slug);
        """);
    }
    
    private static async Task SetGenresTable(IDbConnection connection)
    {
        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {DatabaseTables.Genres} (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL UNIQUE
            );
        """);
    }

    private static async Task SetArtistsTable(IDbConnection connection)
    {
        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {DatabaseTables.Artists} (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL,
                slug TEXT NOT NULL UNIQUE,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.Artists}_slug_idx
            ON {DatabaseTables.Artists}(slug);
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.Artists}_name_idx
            ON {DatabaseTables.Artists}(name);
        """);
    }

    private static async Task SetMusicAlbumGenresTable(IDbConnection connection)
    {
        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {DatabaseTables.MusicAlbumGenres} (
                music_album_id UUID NOT NULL REFERENCES {DatabaseTables.MusicAlbums} (id) ON DELETE CASCADE,
                genre_id INTEGER NOT NULL REFERENCES {DatabaseTables.Genres} (id) ON DELETE RESTRICT,
                PRIMARY KEY (music_album_id, genre_id)
            );
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.MusicAlbumGenres}_genre_id_idx
            ON {DatabaseTables.MusicAlbumGenres}(genre_id);
        """);
    }

    private static async Task SetTracksTable(IDbConnection connection)
    {
        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {DatabaseTables.Tracks} (
                id UUID PRIMARY KEY,
                music_album_id UUID NOT NULL REFERENCES {DatabaseTables.MusicAlbums} (id) ON DELETE CASCADE,
                title TEXT NOT NULL,
                track_number INTEGER NOT NULL,
                duration_in_seconds INTEGER,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT unique_track_number_per_album UNIQUE (music_album_id, track_number)
            );
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.Tracks}_music_album_id_idx
            ON {DatabaseTables.Tracks}(music_album_id);
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.Tracks}_track_number_idx
            ON {DatabaseTables.Tracks}(track_number);
        """);
    }

    private static async Task SetAlbumArtistsTable(IDbConnection connection)
    {
        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {DatabaseTables.AlbumArtists} (
                album_id UUID NOT NULL REFERENCES {DatabaseTables.MusicAlbums} (id) ON DELETE CASCADE,
                artist_id UUID NOT NULL REFERENCES {DatabaseTables.Artists} (id) ON DELETE CASCADE,
                artist_order INTEGER NOT NULL DEFAULT 1,
                role TEXT NOT NULL DEFAULT 'Main Artist',
                PRIMARY KEY (album_id, artist_id)
            );
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.AlbumArtists}_album_id_idx
            ON {DatabaseTables.AlbumArtists}(album_id);
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.AlbumArtists}_artist_id_idx
            ON {DatabaseTables.AlbumArtists}(artist_id);
        """);
    }

    private static async Task SetTrackArtistsTable(IDbConnection connection)
    {
        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {DatabaseTables.TrackArtists} (
                track_id UUID NOT NULL REFERENCES {DatabaseTables.Tracks} (id) ON DELETE CASCADE,
                artist_id UUID NOT NULL REFERENCES {DatabaseTables.Artists} (id) ON DELETE CASCADE,
                artist_order INTEGER NOT NULL DEFAULT 1,
                role TEXT NOT NULL DEFAULT 'Main Artist',
                PRIMARY KEY (track_id, artist_id)
            );
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.TrackArtists}_track_id_idx
            ON {DatabaseTables.TrackArtists}(track_id);
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.TrackArtists}_artist_id_idx
            ON {DatabaseTables.TrackArtists}(artist_id);
        """);
    }

    private static async Task SetRatingsTable(IDbConnection connection)
    {
        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {DatabaseTables.Ratings} (
                user_id UUID NOT NULL,
                music_album_id UUID NOT NULL REFERENCES {DatabaseTables.MusicAlbums} (id) ON DELETE CASCADE,
                rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 5),
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (user_id, music_album_id)
            );
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.Ratings}_music_album_id_idx
            ON {DatabaseTables.Ratings}(music_album_id);
        """);

        await connection.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS {DatabaseTables.Ratings}_rating_idx
            ON {DatabaseTables.Ratings}(rating);
        """);
    }
    
    #endregion
}