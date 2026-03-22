using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicAlbums.Api.Auth;
using MusicAlbums.Api.Mapping;
using MusicAlbums.Application.Services;
using MusicAlbums.Contracts.Requests;
using MusicAlbums.Contracts.Responses;

namespace MusicAlbums.Api.Controllers;

/// <summary>
///     Exposes endpoints for managing authenticated users' album ratings.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("")]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RatingsController"/>.
    /// </summary>
    /// <param name="ratingService">Service used to create, remove, and query user ratings.</param>
    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    /// <summary>
    ///     Creates or updates the current user's rating for an album.
    /// </summary>
    /// <param name="id">Album ID to rate.</param>
    /// <param name="request">Rating payload containing the rating value.</param>
    /// <param name="token">Cancellation token for the request pipeline.</param>
    /// <returns>A 200 OK response when the rating is applied; otherwise 404 Not Found.</returns>
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

    /// <summary>
    ///     Deletes the current user's rating for an album.
    /// </summary>
    /// <param name="id">Album ID for which the rating should be removed.</param>
    /// <param name="token">Cancellation token for the request pipeline.</param>
    /// <returns>A 200 OK response when the rating is removed; otherwise 404 Not Found.</returns>
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

    /// <summary>
    ///     Gets all album ratings created by the current user.
    /// </summary>
    /// <param name="token">Cancellation token for the request pipeline.</param>
    /// <returns>A 200 OK response with the current user's ratings.</returns>
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
