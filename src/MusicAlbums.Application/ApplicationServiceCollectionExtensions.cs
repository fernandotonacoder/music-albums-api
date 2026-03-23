using Azure.Identity;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MusicAlbums.Application.Database;
using MusicAlbums.Application.Repositories;
using MusicAlbums.Application.Services;
using Npgsql;

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
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var connStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

        if (string.IsNullOrEmpty(connStringBuilder.Password))
        {
            var credential = new DefaultAzureCredential();
            dataSourceBuilder.UsePeriodicPasswordProvider(async (_, ct) =>
            {
                var context = new Azure.Core.TokenRequestContext(
                    ["https://ossrdbms-aad.database.windows.net/.default"]);
                var token = await credential.GetTokenAsync(context, ct);
                return token.Token;
            }, TimeSpan.FromMinutes(55), TimeSpan.FromSeconds(10));
        }

        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddSingleton<IDbConnectionFactory>(sp =>
            new NpgsqlConnectionFactory(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<DbInitializer>();
        return services;
    }
}
