using MusicAlbums.Application.Models;

namespace MusicAlbums.Application.Services;

public interface IRatingService
{
    Task<bool> RateAlbumAsync(Guid movieId, int rating, Guid userId, CancellationToken token = default);

    Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default);

    Task<IEnumerable<MusicAlbumRating>> GetRatingsForUserAsync(Guid userId, CancellationToken token = default);
}
