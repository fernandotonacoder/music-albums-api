using FluentValidation;
using MusicAlbums.Application.Models;
using MusicAlbums.Application.Repositories;

namespace MusicAlbums.Application.Validators;

public class MusicAlbumValidator : AbstractValidator<MusicAlbum>
{
    private readonly IMusicAlbumRepository _musicAlbumRepository;
    
    public MusicAlbumValidator(IMusicAlbumRepository musicAlbumRepository)
    {
        _musicAlbumRepository = musicAlbumRepository;
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Genres)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty();

        RuleFor(x => x.YearOfRelease)
            .LessThanOrEqualTo(DateTime.UtcNow.Year);

        RuleFor(x => x.Slug)
            .MustAsync(ValidateSlug)
            .WithMessage("This album already exists in the system");
        
        RuleFor(x => x.Artists)
            .NotEmpty()
            .WithMessage("At least one artist is required");
        
        RuleForEach(x => x.Tracks)
            .SetValidator(new TrackValidator());
        
        RuleFor(x => x.Tracks)
            .Must(HaveUniqueTrackNumbers)
            .When(x => x.Tracks.Any())
            .WithMessage("Track numbers must be unique within an album");
    }

    private bool HaveUniqueTrackNumbers(List<Track> tracks)
    {
        var trackNumbers = tracks.Select(t => t.TrackNumber).ToList();
        return trackNumbers.Count == trackNumbers.Distinct().Count();
    }

    private async Task<bool> ValidateSlug(MusicAlbum musicAlbum, string slug, CancellationToken token = default)
    {
        var existingAlbum = await _musicAlbumRepository.GetBySlugAsync(slug);

        if (existingAlbum is not null)
        {
            return existingAlbum.Id == musicAlbum.Id;
        }

        return existingAlbum is null;
    }
}
