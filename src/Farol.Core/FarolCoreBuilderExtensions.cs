using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Farol;

/// <summary>Host-agnostic Farol entry point: OTel pipeline, OTLP export, resource identity.</summary>
public static class FarolCoreBuilderExtensions
{
    public static IHostApplicationBuilder AddFarolCore(
        this IHostApplicationBuilder builder,
        Action<FarolOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        FarolOptionsSetup.EnsureRegistered(builder, configure);
        AddFarolCoreInternal(builder);
        return builder;
    }

    internal static void AddFarolCoreInternal(IHostApplicationBuilder builder)
    {
        if (builder.Services.Any(d => d.ServiceType == typeof(FarolCoreMarker)))
            return;
        builder.Services.AddSingleton<FarolCoreMarker>();

        var fallbackAttributes = FarolResource.BuildFallbackAttributes(
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

        builder.Services.AddHostedService<FarolStartupDiagnostics>();
    }
}

internal sealed class FarolCoreMarker { }
