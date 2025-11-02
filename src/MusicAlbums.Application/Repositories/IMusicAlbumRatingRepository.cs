using MusicAlbums.Application.Models;

namespace MusicAlbums.Application.Repositories;

public interface IMusicAlbumRatingRepository
{
    Task<bool> RateAlbumAsync(Guid albumId, int rating, Guid userId, CancellationToken token = default);

    Task<float?> GetRatingAsync(Guid albumId, CancellationToken token = default);
    
    Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid albumId, Guid userId, CancellationToken token = default);

    Task<bool> DeleteRatingAsync(Guid albumId, Guid userId, CancellationToken token = default);

    Task<IEnumerable<MusicAlbumRating>> GetRatingsForUserAsync(Guid userId, CancellationToken token = default);
}



