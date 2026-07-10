using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Oversight;

/// <summary>Host-agnostic Oversight entry point: OTel pipeline, OTLP export, resource identity.</summary>
public static class OversightCoreBuilderExtensions
{
    public static IHostApplicationBuilder AddOversightCore(
        this IHostApplicationBuilder builder,
        Action<OversightOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        OversightOptionsSetup.EnsureRegistered(builder, configure);
        AddOversightCoreInternal(builder);
        return builder;
    }

    internal static void AddOversightCoreInternal(IHostApplicationBuilder builder)
    {
        if (builder.Services.Any(d => d.ServiceType == typeof(OversightCoreMarker)))
            return;
        builder.Services.AddSingleton<OversightCoreMarker>();

        var fallbackAttributes = OversightResource.BuildFallbackAttributes(
            Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME"),
            Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES"),
            builder.Environment.EnvironmentName,
            Assembly.GetEntryAssembly());

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(fallbackAttributes))
            .WithTracing(static tracing => tracing
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(static metrics => metrics
                // System.Runtime and System.Net.Http are built-in runtime meters on
                // net10.0; the legacy instrumentation packages would duplicate them.
                .AddMeter("System.Runtime")
                .AddMeter("System.Net.Http")
                .AddProcessInstrumentation()
                .AddOtlpExporter());

        builder.Services.AddHostedService<OversightStartupDiagnostics>();
    }
}

internal sealed class OversightCoreMarker { }
