using Dapper;
using MusicAlbums.Application.Database;
using MusicAlbums.Application.Models;

namespace MusicAlbums.Application.Repositories;

public class MusicAlbumRatingRepository(IDbConnectionFactory dbConnectionFactory) : IMusicAlbumRatingRepository
{
    public async Task<bool> RateAlbumAsync(Guid albumId, int rating, Guid userId, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        var result = await connection.ExecuteAsync(new CommandDefinition("""
            insert into ratings(user_id, music_album_id, rating) 
            values (@userId, @albumId, @rating)
            on conflict (user_id, music_album_id) do update 
                set rating = @rating, updated_at = now()
            """, new { userId, albumId, rating }, cancellationToken: token));
        return result > 0;
    }
    
    public async Task<float?> GetRatingAsync(Guid albumId, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        return await connection.QuerySingleOrDefaultAsync<float?>(new CommandDefinition("""
            select round(avg(r.rating), 1) from ratings r
            where music_album_id = @album_id
            """, new { album_id = albumId }, cancellationToken: token));
    }
    
    public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid albumId, Guid userId, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        return await connection.QuerySingleOrDefaultAsync<(float?, int?)>(new CommandDefinition("""
            select round(avg(rating), 1), 
                   (select rating 
                    from ratings 
                    where music_album_id = @album_id 
                      and user_id = @user_id
                    limit 1) 
            from ratings
            where music_album_id = @album_id
            """, new { album_id = albumId, user_id = userId }, cancellationToken: token));
    }
    
    public async Task<bool> DeleteRatingAsync(Guid albumId, Guid userId, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        var result = await connection.ExecuteAsync(new CommandDefinition("""
            delete from ratings
            where music_album_id = @album_id
            and user_id = @user_id
            """, new { userId, album_id = albumId }, cancellationToken: token));
        
        return result > 0;
    }
    
    public async Task<IEnumerable<MusicAlbumRating>> GetRatingsForUserAsync(Guid userId, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);
        return await connection.QueryAsync<MusicAlbumRating>(new CommandDefinition("""
            select r.rating as Rating, 
                   r.music_album_id as AlbumId, 
                   ma.slug as Slug
            from ratings r
            inner join music_albums ma on r.music_album_id = ma.id
            where user_id = @user_id
            """, new { user_id = userId }, cancellationToken: token));
    }
}