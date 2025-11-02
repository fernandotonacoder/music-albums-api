using FluentValidation;
using MusicAlbums.Application.Models;

namespace MusicAlbums.Application.Validators;

public class TrackValidator : AbstractValidator<Track>
{
    public TrackValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Track title is required");

        RuleFor(x => x.TrackNumber)
            .GreaterThan(0)
            .WithMessage("Track number must be greater than 0");

        RuleFor(x => x.DurationInSeconds)
            .GreaterThan(0)
            .When(x => x.DurationInSeconds.HasValue)
            .WithMessage("Duration must be greater than 0 seconds");
    }
}

