using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MusicAlbums.Application.Database;
using MusicAlbums.Application.Repositories;
using MusicAlbums.Application.Services;

namespace MusicAlbums.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMusicAlbumRatingRepository, MusicAlbumRatingRepository>();
        services.AddSingleton<IRatingService, RatingService>();
        services.AddSingleton<IMusicAlbumRepository, MusicAlbumRepository>();
        services.AddSingleton<IMusicAlbumService, MusicAlbumService>();
        services.AddValidatorsFromAssemblyContaining<IApplicationMarker>(ServiceLifetime.Singleton);
        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IDbConnectionFactory>(_ => 
            new NpgsqlConnectionFactory(connectionString));
        services.AddSingleton<DbInitializer>();
        return services;
    }
}
