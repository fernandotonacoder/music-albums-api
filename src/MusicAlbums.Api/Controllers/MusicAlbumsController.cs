using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MusicAlbums.Api.Auth;
using MusicAlbums.Api.Mapping;
using MusicAlbums.Application.Services;
using MusicAlbums.Contracts.Requests;
using MusicAlbums.Contracts.Responses;

namespace MusicAlbums.Api.Controllers;

/// <summary>
///     Exposes endpoints for creating, querying, updating, deleting, and rating music albums.
/// </summary>
/// <param name="musicAlbumService">Service used for album read/write operations.</param>
/// <param name="outputCacheStore">Output cache store used to evict album cache entries after mutations.</param>
[ApiController]
[ApiVersion(1.0)]
[Route("")]
public class MusicAlbumsController(
    IMusicAlbumService musicAlbumService,
    IOutputCacheStore outputCacheStore) : ControllerBase
{
    /// <summary>
    ///     Creates a new music album and evicts the albums cache tag.
    /// </summary>
    /// <param name="request">Album payload with title, release year, genres, artists, and tracks.</param>
    /// <param name="token">Cancellation token for the request pipeline.</param>
    /// <returns>
    ///     A 201 Created response with the created album payload; or 400 Bad Request when validation fails.
    /// </returns>
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPost(ApiEndpoints.MusicAlbums.Create)]
    [ProducesResponseType(typeof(MusicAlbumResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMusicAlbumRequest request, CancellationToken token)
    {
        var album = request.MapToMusicAlbum();
        await musicAlbumService.CreateAsync(album, token);
        await outputCacheStore.EvictByTagAsync("albums", token);
        var albumResponse = album.MapToResponse();
        return CreatedAtAction(nameof(Get), new { idOrSlug = album.Id }, albumResponse);
    }

    /// <summary>
    ///     Gets a single album by ID or slug.
    ///     If <paramref name="idOrSlug" /> parses as a GUID, the album is fetched by ID; otherwise, it is fetched by slug.
    /// </summary>
    /// <param name="idOrSlug">Album identifier (GUID) or URL slug.</param>
    /// <param name="token">Cancellation token for the request pipeline.</param>
    /// <returns>A 200 OK response with the album when found; otherwise 404 Not Found.</returns>
    [HttpGet(ApiEndpoints.MusicAlbums.Get)]
    [OutputCache(PolicyName = "AlbumCache", Tags = ["albums"])]
    [ProducesResponseType(typeof(MusicAlbumResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string idOrSlug, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();

        var album = Guid.TryParse(idOrSlug, out var id)
            ? await musicAlbumService.GetByIdAsync(id, userId, token)
            : await musicAlbumService.GetBySlugAsync(idOrSlug, userId, token);

        if (album is null) return NotFound();

        var response = album.MapToResponse();
        return Ok(response);
    }

    /// <summary>
    ///     Gets a paged list of albums using optional filters and sorting.
    /// </summary>
    /// <param name="request">Query options including title/year filters, sort field, page, and page size.</param>
    /// <param name="token">Cancellation token for the request pipeline.</param>
    /// <returns>A 200 OK response containing the paged albums result.</returns>
    [HttpGet(ApiEndpoints.MusicAlbums.GetAll)]
    [OutputCache(PolicyName = "AlbumCache", Tags = ["albums"])]
    [ProducesResponseType(typeof(MusicAlbumsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllMusicAlbumsRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var options = request.MapToOptions()
            .WithUser(userId);
        var albums = await musicAlbumService.GetAllAsync(options, token);
        var albumCount = await musicAlbumService.GetCountAsync(options.Title, options.YearOfRelease, token);
        var albumsResponse = albums.MapToResponse(request.Page, request.PageSize, albumCount);
        return Ok(albumsResponse);
    }

    /// <summary>
    ///     Updates an existing album by ID and evicts the albums cache tag.
    /// </summary>
    /// <param name="id">Album ID to update.</param>
    /// <param name="request">Updated album payload.</param>
    /// <param name="token">Cancellation token for the request pipeline.</param>
    /// <returns>
    ///     A 200 OK response with the updated album when found; 404 Not Found when the album does not exist; or
    ///     400 Bad Request when validation fails.
    /// </returns>
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPut(ApiEndpoints.MusicAlbums.Update)]
    [ProducesResponseType(typeof(MusicAlbumResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdateMusicAlbumRequest request, CancellationToken token)
    {
        var album = request.MapToAlbum(id);
        var userId = HttpContext.GetUserId();
        var updatedAlbum = await musicAlbumService.UpdateAsync(album, userId, token);

        if (updatedAlbum is null) return NotFound();

        await outputCacheStore.EvictByTagAsync("albums", token);
        var response = updatedAlbum.MapToResponse();
        return Ok(response);
    }

    /// <summary>
    ///     Deletes an album by ID and evicts the albums cache tag.
    /// </summary>
    /// <param name="id">Album ID to delete.</param>
    /// <param name="token">Cancellation token for the request pipeline.</param>
    /// <returns>A 200 OK response when deleted; otherwise 404 Not Found.</returns>
    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpDelete(ApiEndpoints.MusicAlbums.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken token)
    {
        var deleted = await musicAlbumService.DeleteByIdAsync(id, token);

        if (!deleted) return NotFound();

        await outputCacheStore.EvictByTagAsync("albums", token);
        return Ok();
    }
}