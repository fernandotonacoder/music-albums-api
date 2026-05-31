using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace MusicAlbums.ServiceDefaults;

public static class Extensions
{
    private const string HealthEndpointPrefix = "/_health";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        // Both apps run behind a TLS-terminating reverse proxy (Azure Container Apps ingress in the
        // cloud, the Aspire proxy locally). Honor X-Forwarded-Proto so the request scheme is seen as
        // https; otherwise generated URLs — notably the OpenAPI `servers` entry — come out as http,
        // and the HTTPS-served Scalar UI blocks the calls as mixed content ("Failed to fetch"). The
        // proxy IP is not fixed, so the known-proxy allow-list is cleared. Needs app.UseForwardedHeaders().
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    // In the deployed container the app is HTTP-only behind the Azure Container Apps ingress: TLS is
    // terminated at the edge (which also upgrades HTTP→HTTPS), so emit HSTS to keep browsers on HTTPS
    // for the public FQDN. No app-level UseHttpsRedirection — there is no HTTPS port in the container
    // (it would only log "Failed to determine the https port"), the ingress already redirects, and HSTS
    // on localhost would be a footgun. The .NET runtime image sets DOTNET_RUNNING_IN_CONTAINER=true.
    public static WebApplication UseHttpsHardening(this WebApplication app)
    {
        var inContainer = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (inContainer)
        {
            app.UseHsts();
        }

        return app;
    }

    private static void ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPrefix)
                    )
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();
    }

    private static void AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor();
        }
    }
}
