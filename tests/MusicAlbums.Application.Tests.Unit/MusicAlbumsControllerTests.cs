using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MusicAlbums.Application.Models;
using MusicAlbums.Application.Services;
using MusicAlbums.Application.Tests.Unit.Factories;
using MusicAlbums.Contracts.Requests;
using MusicAlbums.Contracts.Responses;
using Xunit;

namespace MusicAlbums.Application.Tests.Unit;

public class MusicAlbumsControllerTests
{
    #region GetAll method

    [Fact]
    public async Task GetAll_ReturnsOkObjectResult()
    {
        var (controller, _, _) = MusicAlbumsControllerTestFactory.CreateSut();

        var result = await controller.GetAll(new GetAllMusicAlbumsRequest(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResponseMetadata()
    {
        var request = new GetAllMusicAlbumsRequest { Page = 3, PageSize = 20 };
        var (controller, serviceMock, _) = MusicAlbumsControllerTestFactory.CreateSut();
        serviceMock
            .Setup(x => x.GetCountAsync(
                It.IsAny<string?>(), It.IsAny<int?>(), CancellationToken.None))
            .ReturnsAsync(57);

        var result = await controller.GetAll(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MusicAlbumsResponse>(okResult.Value);
        Assert.Equal(3, response.Page);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(57, response.Total);
    }

    [Fact]
    public async Task GetAll_MapsRequestAndUserToOptionsPassedToService()
    {
        var userId = Guid.NewGuid();
        var request = new GetAllMusicAlbumsRequest
        {
            Title = "Dark Side",
            Year = 1973,
            SortBy = "-year",
            Page = 2,
            PageSize = 5
        };

        GetAllMusicAlbumsOptions? capturedOptions = null;
        var (controller, serviceMock, _) = MusicAlbumsControllerTestFactory.CreateSut(userId);
        serviceMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<GetAllMusicAlbumsOptions>(), CancellationToken.None))
            .Callback<GetAllMusicAlbumsOptions, CancellationToken>((options, _) => capturedOptions = options)
            .ReturnsAsync(Array.Empty<MusicAlbum>());

        await controller.GetAll(request, CancellationToken.None);

        Assert.NotNull(capturedOptions);
        Assert.Equal("Dark Side", capturedOptions!.Title);
        Assert.Equal(1973, capturedOptions.YearOfRelease);
        Assert.Equal("year", capturedOptions.SortField);
        Assert.Equal(SortOrder.Descending, capturedOptions.SortOrder);
        Assert.Equal(2, capturedOptions.Page);
        Assert.Equal(5, capturedOptions.PageSize);
        Assert.Equal(userId, capturedOptions.UserId);
    }

    [Fact]
    public async Task GetAll_CallsGetCountWithMappedFilters()
    {
        var request = new GetAllMusicAlbumsRequest { Title = "Dark Side", Year = 1973 };
        var (controller, serviceMock, _) = MusicAlbumsControllerTestFactory.CreateSut();

        await controller.GetAll(request, CancellationToken.None);

        serviceMock.Verify(
            x => x.GetCountAsync("Dark Side", 1973, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_DoesNotEvictOutputCache()
    {
        var (controller, _, outputCacheStoreMock) = MusicAlbumsControllerTestFactory.CreateSut();

        await controller.GetAll(new GetAllMusicAlbumsRequest(), CancellationToken.None);

        outputCacheStoreMock.Verify(
            x => x.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Create method

    [Fact]
    public async Task Create_WhenRequestIsValid_ReturnsCreatedAtActionToGetWithResponse()
    {
        var (controller, serviceMock, _) = MusicAlbumsControllerTestFactory.CreateSut();

        var request = BuildValidCreateRequest();

        MusicAlbum? capturedAlbum = null;
        serviceMock.Setup(x => x.CreateAsync(It.IsAny<MusicAlbum>(), It.IsAny<CancellationToken>()))
            .Callback<MusicAlbum, CancellationToken>((album, _) => capturedAlbum = album)
            .ReturnsAsync(true);

        var result = await controller.Create(request, CancellationToken.None);
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(StatusCodes.Status201Created, createdAtActionResult.StatusCode);
        Assert.Equal(nameof(controller.Get), createdAtActionResult.ActionName);
        Assert.NotNull(createdAtActionResult.RouteValues);
        Assert.True(createdAtActionResult.RouteValues.TryGetValue("idOrSlug", out var idOrSlug));

        var response = Assert.IsType<MusicAlbumResponse>(createdAtActionResult.Value);
        Assert.NotNull(capturedAlbum);
        Assert.Equal(capturedAlbum!.Id, response.Id);
        Assert.Equal(capturedAlbum.Id.ToString(), idOrSlug?.ToString());
        Assert.Equal(request.Title, response.Title);
        Assert.Equal(request.YearOfRelease, response.YearOfRelease);
        Assert.Equal(request.Tracks.Count(), response.Tracks.Count());
    }

    [Fact]
    public async Task Create_WhenRequestIsValid_CallsCreateAsyncWithMappedAlbum()
    {
        var (controller, serviceMock, _) = MusicAlbumsControllerTestFactory.CreateSut();

        var request = BuildValidCreateRequest();
        SetupCreateSucceeds(serviceMock);

        await controller.Create(request, CancellationToken.None);

        serviceMock.Verify(
            x => x.CreateAsync(It.Is<MusicAlbum>(album =>
                    album.Title == request.Title
                    && album.YearOfRelease == request.YearOfRelease
                    && album.Tracks.Count == request.Tracks.Count()
                    && album.Genres.SequenceEqual(request.Genres)
                    && album.Artists.Select(a => a.Name).SequenceEqual(request.ArtistNames)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenRequestIsValid_EvictsAlbumsCacheTag()
    {
        var (controller, serviceMock, outputCacheStoreMock) = MusicAlbumsControllerTestFactory.CreateSut();

        var request = BuildValidCreateRequest();
        SetupCreateSucceeds(serviceMock);

        await controller.Create(request, CancellationToken.None);

        outputCacheStoreMock.Verify(
            x => x.EvictByTagAsync("albums", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region private helper methods

    private static CreateMusicAlbumRequest BuildValidCreateRequest()
    {
        return new CreateMusicAlbumRequest()
        {
            Title = "Test Album",
            YearOfRelease = 2026,
            Genres = ["Rock", "Pop"],
            ArtistNames = ["TestArtist1", "TestArtist2"],
            Tracks =
            [
                new CreateTrackRequest
                {
                    Title = "Test Track 1",
                    DurationInSeconds = 240,
                    ArtistNames =
                    [
                        "TestArtist1"
                    ],
                    TrackNumber = 1
                },
                new CreateTrackRequest
                {
                    Title = "Test Track 2",
                    DurationInSeconds = 200,
                    ArtistNames =
                    [
                        "TestArtist2"
                    ],
                    TrackNumber = 2
                }
            ]
        };
    }

    private static void SetupCreateSucceeds(Mock<IMusicAlbumService> serviceMock)
    {
        serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<MusicAlbum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    #endregion
}