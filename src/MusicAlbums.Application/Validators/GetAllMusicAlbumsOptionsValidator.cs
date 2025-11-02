using FluentValidation;
using MusicAlbums.Application.Models;

namespace MusicAlbums.Application.Validators;

public class GetAllMusicAlbumsOptionsValidator : AbstractValidator<GetAllMusicAlbumsOptions>
{
    private static readonly string[] AcceptableSortFields =
    [
        "title", "yearofrelease"
    ];

    public GetAllMusicAlbumsOptionsValidator()
    {
        RuleFor(x => x.YearOfRelease)
            .LessThanOrEqualTo(DateTime.UtcNow.Year);

        RuleFor(x => x.SortField)
            .Must(x => x is null || AcceptableSortFields.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("You can only sort by 'title' or 'yearofrelease'");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 25)
            .WithMessage("You can get between 1 and 25 albums per page");
    }
}