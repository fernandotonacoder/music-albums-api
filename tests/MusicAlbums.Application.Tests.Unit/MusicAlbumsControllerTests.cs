using Microsoft.AspNetCore.Mvc;
using Moq;
using MusicAlbums.Application.Models;
using MusicAlbums.Application.Tests.Unit.Fixtures;
using MusicAlbums.Contracts.Requests;
using MusicAlbums.Contracts.Responses;
using Xunit;

namespace MusicAlbums.Application.Tests.Unit;

public class MusicAlbumsControllerTests(
    MusicAlbumsControllerFixture fixture) : IClassFixture<MusicAlbumsControllerFixture>
{
    [Fact]
    public async Task GetAll_ReturnsOkObjectResult()
    {
        var (controller, _, _) = MusicAlbumsControllerFixture.CreateSut();

        var result = await controller.GetAll(new GetAllMusicAlbumsRequest(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResponseMetadata()
    {
        var request = new GetAllMusicAlbumsRequest { Page = 3, PageSize = 20 };
        var (controller, serviceMock, _) = MusicAlbumsControllerFixture.CreateSut();
        serviceMock
            .Setup(x => x.GetCountAsync(It.IsAny<string?>(), It.IsAny<int?>(), CancellationToken.None))
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
        var (controller, serviceMock, _) = MusicAlbumsControllerFixture.CreateSut(userId);
        serviceMock
            .Setup(x => x.GetAllAsync(It.IsAny<GetAllMusicAlbumsOptions>(), CancellationToken.None))
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
        var (controller, serviceMock, _) = MusicAlbumsControllerFixture.CreateSut();

        await controller.GetAll(request, CancellationToken.None);

        serviceMock.Verify(
            x => x.GetCountAsync("Dark Side", 1973, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_DoesNotEvictOutputCache()
    {
        var (controller, _, outputCacheStoreMock) = MusicAlbumsControllerFixture.CreateSut();

        await controller.GetAll(new GetAllMusicAlbumsRequest(), CancellationToken.None);

        outputCacheStoreMock.Verify(
            x => x.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
