using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicAlbums.Api.Auth;
using MusicAlbums.Api.Mapping;
using MusicAlbums.Application.Services;
using MusicAlbums.Contracts.Requests;
using MusicAlbums.Contracts.Responses;

namespace MusicAlbums.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [Authorize]
    [HttpPut(ApiEndpoints.MusicAlbums.Rate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RateAlbum([FromRoute] Guid id,
        [FromBody] RateMusicAlbumRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var result = await _ratingService.RateAlbumAsync(id, request.Rating, userId!.Value, token);
        return result ? Ok() : NotFound();
    }

    [Authorize]
    [HttpDelete(ApiEndpoints.MusicAlbums.DeleteRating)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRating([FromRoute] Guid id,
        CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var result = await _ratingService.DeleteRatingAsync(id, userId!.Value, token);
        return result ? Ok() : NotFound();
    }

    [Authorize]
    [HttpGet(ApiEndpoints.Ratings.GetUserRatings)]
    [ProducesResponseType(typeof(IEnumerable<MusicAlbumRatingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRatings(CancellationToken token = default)
    {
        var userId = HttpContext.GetUserId();
        var ratings = await _ratingService.GetRatingsForUserAsync(userId!.Value, token);
        var ratingsResponse = ratings.MapToResponse();
        return Ok(ratingsResponse);
    }
}
