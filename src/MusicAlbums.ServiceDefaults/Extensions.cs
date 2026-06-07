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

    extension(IHostEnvironment)
    {
        // Runtime base images set DOTNET_RUNNING_IN_CONTAINER=true.
        private static bool IsContainer => string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    extension(WebApplication app)
    {
        // Behind the Container Apps ingress TLS is edge-terminated and HTTP→HTTPS is already
        // redirected, so only emit HSTS. No UseHttpsRedirection: no HTTPS port in the container.
        public void UseHttpsHardening()
        {
            if (IHostEnvironment.IsContainer)
            {
                app.UseHsts();
            }
        }
    }

    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        private void ConfigureOpenTelemetry()
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
                        .AddRuntimeInstrumentation()
                        // Npgsql connection-pool and command metrics.
                        .AddMeter("Npgsql");
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(builder.Environment.ApplicationName)
                        // SQL command spans; surfaces the database as a dependency in the Application Map.
                        .AddSource("Npgsql")
                        .AddAspNetCoreInstrumentation(options =>
                            // Keep health-probe noise out of traces.
                            options.Filter = context =>
                                !context.Request.Path.StartsWithSegments(HealthEndpointPrefix)
                        )
                        .AddHttpClientInstrumentation();
                });

            builder.AddOpenTelemetryExporters();
        }

        private void AddOpenTelemetryExporters()
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

        public void AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();

            // Behind a TLS-terminating proxy: trust X-Forwarded-Proto so generated URLs (OpenAPI
            // `servers`) use https and Scalar doesn't block them as mixed content. Proxy IP varies,
            // so clear the allow-list. Needs app.UseForwardedHeaders().
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
        }
    }
}
