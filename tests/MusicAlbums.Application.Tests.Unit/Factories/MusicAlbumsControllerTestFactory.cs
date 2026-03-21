using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using MusicAlbums.Api.Controllers;
using MusicAlbums.Application.Models;
using MusicAlbums.Application.Services;

namespace MusicAlbums.Application.Tests.Unit.Factories;

public static class MusicAlbumsControllerTestFactory
{
    public static (MusicAlbumsController Controller, Mock<IMusicAlbumService> ServiceMock,
        Mock<IOutputCacheStore> OutputCacheStoreMock) CreateSut(Guid? userId = null)
    {
        var serviceMock = new Mock<IMusicAlbumService>();
        serviceMock
            .Setup(x => x.GetAllAsync(It.IsAny<GetAllMusicAlbumsOptions>(), CancellationToken.None))
            .ReturnsAsync(Array.Empty<MusicAlbum>());
        serviceMock
            .Setup(x => x.GetCountAsync(It.IsAny<string?>(), It.IsAny<int?>(), CancellationToken.None))
            .ReturnsAsync(0);

        var outputCacheStoreMock = new Mock<IOutputCacheStore>();
        var controller = new MusicAlbumsController(serviceMock.Object, outputCacheStoreMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = BuildHttpContext(userId)
            }
        };

        return (controller, serviceMock, outputCacheStoreMock);
    }

    private static DefaultHttpContext BuildHttpContext(Guid? userId)
    {
        var claims = userId is null
            ? Array.Empty<Claim>()
            : [new Claim("userid", userId.Value.ToString())];

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };

        return httpContext;
    }
}

