using FluentValidation;
using FluentValidation.Results;
using MusicAlbums.Application.Models;
using MusicAlbums.Application.Repositories;

namespace MusicAlbums.Application.Services;

public class RatingService : IRatingService
{
    private readonly IMusicAlbumRatingRepository _musicAlbumRatingRepository;
    private readonly IMusicAlbumRepository _musicAlbumRepository;

    public RatingService(IMusicAlbumRatingRepository musicAlbumRatingRepository, IMusicAlbumRepository musicAlbumRepository)
    {
        _musicAlbumRatingRepository = musicAlbumRatingRepository;
        _musicAlbumRepository = musicAlbumRepository;
    }

    public async Task<bool> RateAlbumAsync(Guid movieId, int rating, Guid userId, CancellationToken token = default)
    {
        if (rating is <= 0 or > 5)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure
                {
                    PropertyName = "Rating",
                    ErrorMessage = "Rating must be between 1 and 5"
                }
            });
        }

        var movieExists = await _musicAlbumRepository.ExistsByIdAsync(movieId, token);
        if (!movieExists)
        {
            return false;
        }

        return await _musicAlbumRatingRepository.RateAlbumAsync(movieId, rating, userId, token);
    }

    public Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
    {
        return _musicAlbumRatingRepository.DeleteRatingAsync(movieId, userId, token);
    }

    public Task<IEnumerable<MusicAlbumRating>> GetRatingsForUserAsync(Guid userId, CancellationToken token = default)
    {
        return _musicAlbumRatingRepository.GetRatingsForUserAsync(userId, token);
    }
}


