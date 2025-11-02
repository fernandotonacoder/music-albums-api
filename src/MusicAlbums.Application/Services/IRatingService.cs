using MusicAlbums.Application.Models;

namespace MusicAlbums.Application.Services;

public interface IRatingService
{
    Task<bool> RateAlbumAsync(Guid albumId, int rating, Guid userId, CancellationToken token = default);

    Task<bool> DeleteRatingAsync(Guid albumId, Guid userId, CancellationToken token = default);

    Task<IEnumerable<MusicAlbumRating>> GetRatingsForUserAsync(Guid userId, CancellationToken token = default);
}
