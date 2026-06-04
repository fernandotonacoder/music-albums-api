using System.Net.Http.Json;
using MusicAlbums.Tests.Integration.Contracts;

namespace MusicAlbums.Tests.Integration;

[Collection("AspireTests")]
public sealed class GetAllMusicAlbumsTests(DistributedAppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetAllMusicAlbums_ShouldReturnEmptyCollection_WhenNoAlbumsExist()
    {
        var response = await fixture.MusicAlbumsApiClient.GetAsync(
            ApiEndpoints.MusicAlbums.GetAll, TestContext.Current.CancellationToken);
        var albums = await response.Content.ReadFromJsonAsync<MusicAlbumsResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        albums!.Items.Should().BeEmpty();
        albums.Total.Should().Be(0);
    }
}
