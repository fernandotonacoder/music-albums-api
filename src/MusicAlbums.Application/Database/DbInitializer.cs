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
        await _retryPipeline.ExecuteAsync(async token =>
        {
            logger.LogInformation("Attempting to connect to database and initialize schema...");

            using var connection = await dbConnectionFactory.CreateConnectionAsync(token);

            await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS music_albums (
                id UUID PRIMARY KEY,
                slug TEXT NOT NULL, 
                title TEXT NOT NULL,
                year_of_release INTEGER NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );
        """);

            await connection.ExecuteAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS music_albums_slug_idx
            ON music_albums
            USING btree(slug);
        """);

            await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS genres (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL UNIQUE
            );
        """);

            await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS music_album_genres (
                music_album_id UUID NOT NULL REFERENCES music_albums (id) ON DELETE CASCADE,
                genre_id INTEGER NOT NULL REFERENCES genres (id) ON DELETE RESTRICT,
                PRIMARY KEY (music_album_id, genre_id)
            );
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS music_album_genres_genre_id_idx
            ON music_album_genres(genre_id);
        """);

            await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS tracks (
                id UUID PRIMARY KEY,
                music_album_id UUID NOT NULL REFERENCES music_albums (id) ON DELETE CASCADE,
                title TEXT NOT NULL,
                track_number INTEGER NOT NULL,
                duration_in_seconds INTEGER,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT unique_track_number_per_album UNIQUE (music_album_id, track_number)
            );
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS tracks_music_album_id_idx
            ON tracks(music_album_id);
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS tracks_track_number_idx
            ON tracks(track_number);
        """);

            await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS artists (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL,
                slug TEXT NOT NULL UNIQUE,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS artists_slug_idx
            ON artists(slug);
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS artists_name_idx
            ON artists(name);
        """);

            await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS album_artists (
                album_id UUID NOT NULL REFERENCES music_albums (id) ON DELETE CASCADE,
                artist_id UUID NOT NULL REFERENCES artists (id) ON DELETE CASCADE,
                artist_order INTEGER NOT NULL DEFAULT 1,
                role TEXT NOT NULL DEFAULT 'Main Artist',
                PRIMARY KEY (album_id, artist_id)
            );
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS album_artists_album_id_idx
            ON album_artists(album_id);
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS album_artists_artist_id_idx
            ON album_artists(artist_id);
        """);

            await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS track_artists (
                track_id UUID NOT NULL REFERENCES tracks (id) ON DELETE CASCADE,
                artist_id UUID NOT NULL REFERENCES artists (id) ON DELETE CASCADE,
                artist_order INTEGER NOT NULL DEFAULT 1,
                role TEXT NOT NULL DEFAULT 'Main Artist',
                PRIMARY KEY (track_id, artist_id)
            );
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS track_artists_track_id_idx
            ON track_artists(track_id);
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS track_artists_artist_id_idx
            ON track_artists(artist_id);
        """);

            await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS ratings (
                user_id UUID NOT NULL,
                music_album_id UUID NOT NULL REFERENCES music_albums (id) ON DELETE CASCADE,
                rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 5),
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (user_id, music_album_id)
            );
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS ratings_music_album_id_idx
            ON ratings(music_album_id);
        """);

            await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS ratings_rating_idx
            ON ratings(rating);
        """);

            logger.LogInformation("Database schema initialized successfully.");
        }, cancellationToken);
    }
}