using FluentValidation;
using MusicAlbums.Application.Models;
using MusicAlbums.Application.Repositories;

namespace MusicAlbums.Application.Services;

public class MusicAlbumService(
    IMusicAlbumRepository musicAlbumRepository,
    IValidator<MusicAlbum> albumValidator,
    IMusicAlbumRatingRepository musicAlbumRatingRepository,
    IValidator<GetAllMusicAlbumsOptions> optionsValidator)
    : IMusicAlbumService
{
    public async Task<bool> CreateAsync(MusicAlbum musicAlbum, CancellationToken token = default)
    {
        await albumValidator.ValidateAndThrowAsync(musicAlbum, cancellationToken: token);
        return await musicAlbumRepository.CreateAsync(musicAlbum, token);
    }

    public Task<MusicAlbum?> GetByIdAsync(Guid id, Guid? userid = default, CancellationToken token = default)
    {
        return musicAlbumRepository.GetByIdAsync(id, userid, token);
    }

    public Task<MusicAlbum?> GetBySlugAsync(string slug, Guid? userid = default, CancellationToken token = default)
    {
        return musicAlbumRepository.GetBySlugAsync(slug, userid, token);
    }

    public async Task<IEnumerable<MusicAlbum>> GetAllAsync(GetAllMusicAlbumsOptions options, CancellationToken token = default)
    {
        await optionsValidator.ValidateAndThrowAsync(options, token);
        
        return await musicAlbumRepository.GetAllAsync(options, token);
    }

    public async Task<MusicAlbum?> UpdateAsync(MusicAlbum musicAlbum, Guid? userid = default, CancellationToken token = default)
    {
        await albumValidator.ValidateAndThrowAsync(musicAlbum, cancellationToken: token);
        
        var albumExists = await musicAlbumRepository.ExistsByIdAsync(musicAlbum.Id, token);
        if (!albumExists)
        {
            return null;
        }

        await musicAlbumRepository.UpdateAsync(musicAlbum, token);

        if (!userid.HasValue)
        {
            var rating = await musicAlbumRatingRepository.GetRatingAsync(musicAlbum.Id, token);
            musicAlbum.Rating = rating;
            return musicAlbum;
        }
        
        var ratings = await musicAlbumRatingRepository.GetRatingAsync(musicAlbum.Id, userid.Value, token);
        musicAlbum.Rating = ratings.Rating;
        musicAlbum.UserRating = ratings.UserRating;
        return musicAlbum;
    }

    public Task<bool> DeleteByIdAsync(Guid id, CancellationToken token = default)
    {
        return musicAlbumRepository.DeleteByIdAsync(id, token);
    }

    public Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken token = default)
    {
        return musicAlbumRepository.GetCountAsync(title, yearOfRelease, token);
    }
}