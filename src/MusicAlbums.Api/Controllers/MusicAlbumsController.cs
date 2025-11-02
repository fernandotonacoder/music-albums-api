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

[ApiController]
[ApiVersion(1.0)]
public class MusicAlbumsController(IMusicAlbumService musicAlbumService, IOutputCacheStore outputCacheStore)
    : ControllerBase
{
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPost(ApiEndpoints.MusicAlbums.Create)]
    [ProducesResponseType(typeof(MusicAlbumResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody]CreateMusicAlbumRequest request,
        CancellationToken token)
    {
        var album = request.MapToMusicAlbum();
        await musicAlbumService.CreateAsync(album, token);
        await outputCacheStore.EvictByTagAsync("albums", token);
        var albumResponse = album.MapToResponse();
        return CreatedAtAction(nameof(GetV1), new { idOrSlug = album.Id }, albumResponse);
    }
    
    [HttpGet(ApiEndpoints.MusicAlbums.Get)]
    [OutputCache(PolicyName = "AlbumCache", Tags = ["albums"])]
    [ProducesResponseType(typeof(MusicAlbumResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetV1([FromRoute] string idOrSlug,
        CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        
        var album = Guid.TryParse(idOrSlug, out var id)
            ? await musicAlbumService.GetByIdAsync(id, userId, token)
            : await musicAlbumService.GetBySlugAsync(idOrSlug, userId, token);
        
        if (album is null)
        {
            return NotFound();
        }

        var response = album.MapToResponse();
        return Ok(response);
    }
    
    [HttpGet(ApiEndpoints.MusicAlbums.GetAll)]
    [OutputCache(PolicyName = "AlbumCache", Tags = ["albums"])]
    [ProducesResponseType(typeof(MusicAlbumsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllMusicAlbumsRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var options = request.MapToOptions()
            .WithUser(userId);
        var albums = await musicAlbumService.GetAllAsync(options, token);
        var albumCount = await musicAlbumService.GetCountAsync(options.Title, options.YearOfRelease, token);
        var albumsResponse = albums.MapToResponse(request.Page, request.PageSize, albumCount);
        return Ok(albumsResponse);
    }

    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPut(ApiEndpoints.MusicAlbums.Update)]
    [ProducesResponseType(typeof(MusicAlbumResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute]Guid id,
        [FromBody]UpdateMusicAlbumRequest request,
        CancellationToken token)
    {
        var album = request.MapToAlbum(id);
        var userId = HttpContext.GetUserId();
        var updatedAlbum = await musicAlbumService.UpdateAsync(album, userId, token);
        if (updatedAlbum is null)
        {
            return NotFound();
        }

        await outputCacheStore.EvictByTagAsync("albums", token);
        var response = updatedAlbum.MapToResponse();
        return Ok(response);
    }

    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpDelete(ApiEndpoints.MusicAlbums.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id,
        CancellationToken token)
    {
        var deleted = await musicAlbumService.DeleteByIdAsync(id, token);
        if (!deleted)
        {
            return NotFound();
        }

        await outputCacheStore.EvictByTagAsync("albums", token);
        return Ok();
    }
}