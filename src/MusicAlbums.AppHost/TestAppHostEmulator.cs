using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace MusicAlbums.AppHost;

public static class TestAppHostEmulator
{
    private const string TestJwtKey =
        "test-jwt-key-do-not-use-outside-integration-tests-must-be-long-enough-for-hs256";

    private const string TestApiKey = "test-api-key";

    public static (
        IResourceBuilder<PostgresDatabaseResource> Postgres,
        IResourceBuilder<ParameterResource> JwtKey,
        IResourceBuilder<ParameterResource> ApiKey)
        AddTestResources(IDistributedApplicationBuilder builder)
    {
        var jwtKey = builder.AddParameter("jwt-key", TestJwtKey, secret: true);
        var apiKey = builder.AddParameter("api-key", TestApiKey, secret: true);

        var postgres = builder.AddPostgres("musicalbums-postgres-aspire-testing")
            .AddDatabase("albums");

        return (postgres, jwtKey, apiKey);
    }
}
