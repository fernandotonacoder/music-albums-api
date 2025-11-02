using MusicAlbums.Application.Models;

namespace MusicAlbums.Application.Services;

public interface IMusicAlbumService
{
    Task<bool> CreateAsync(MusicAlbum musicAlbum, CancellationToken token = default);
    
    Task<MusicAlbum?> GetByIdAsync(Guid id, Guid? userid = default, CancellationToken token = default);
    
    Task<MusicAlbum?> GetBySlugAsync(string slug, Guid? userid = default, CancellationToken token = default);
    
    Task<IEnumerable<MusicAlbum>> GetAllAsync(GetAllMusicAlbumsOptions options, CancellationToken token = default);
    
    Task<MusicAlbum?> UpdateAsync(MusicAlbum musicAlbum, Guid? userid = default, CancellationToken token = default);
    
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken token = default);

    Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken token = default);
}
