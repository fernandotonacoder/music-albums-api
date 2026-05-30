namespace MusicAlbums.Application.Database;

public static class DatabaseTables
{
    // Base Entities (Lookup tables or independent entities)
    public const string Artists = "artists";
    public const string Genres = "genres";

    // Main Entities (Often depend on base entities)
    public const string MusicAlbums = "music_albums";
    public const string Tracks = "tracks";

    // Junction Tables / Relationships (Many-to-Many)
    public const string AlbumArtists = "album_artists";
    public const string TrackArtists = "track_artists";
    public const string MusicAlbumGenres = "music_album_genres";

    // User Interactions
    public const string Ratings = "ratings";
}