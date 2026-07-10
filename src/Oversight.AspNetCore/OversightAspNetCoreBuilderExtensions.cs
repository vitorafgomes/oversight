using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Oversight;

/// <summary>ASP.NET Core layer: server traces with noise reduction, server metrics, optional Prometheus endpoint.</summary>
public static class OversightAspNetCoreBuilderExtensions
{
    /// <remarks>
    /// Oversight composes with instrumentation delegates configured before it runs rather than
    /// replacing them; any previously registered request filter must also pass for a request
    /// to be traced.
    /// </remarks>
    public static IHostApplicationBuilder AddOversightAspNetCore(
        this IHostApplicationBuilder builder,
        Action<OversightOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        OversightOptionsSetup.EnsureRegistered(builder, configure);
        AddOversightAspNetCoreInternal(builder, OversightOptionsSetup.ResolveSnapshot(builder.Configuration, configure));
        return builder;
    }

    internal static void AddOversightAspNetCoreInternal(IHostApplicationBuilder builder, OversightOptions snapshot)
    {
        if (builder.Services.Any(d => d.ServiceType == typeof(OversightAspNetCoreMarker)))
            return;
        builder.Services.AddSingleton<OversightAspNetCoreMarker>();

        builder.Services.AddOptions<AspNetCoreTraceInstrumentationOptions>()
            .Configure<IOptions<OversightOptions>>(static (instrumentation, oversight) =>
            {
                var matcher = new PathGlobMatcher(oversight.Value.NoiseReduction.ExcludedPaths);
                var existingFilter = instrumentation.Filter;
                instrumentation.Filter = context =>
                    (existingFilter?.Invoke(context) ?? true)
                    && !matcher.IsExcluded(context.Request.Path.Value ?? "/");
            });

        builder.Services.AddOpenTelemetry()
            .WithTracing(static tracing => tracing.AddAspNetCoreInstrumentation())
            .WithMetrics(static metrics => metrics.AddMeter("Microsoft.AspNetCore.Hosting"));

        if (snapshot.Prometheus.Enabled)
        {
            builder.Services.AddOpenTelemetry()
                .WithMetrics(static metrics => metrics.AddPrometheusExporter());
            builder.Services.AddTransient<IStartupFilter, OversightPrometheusStartupFilter>();
        }
    }
}

internal sealed class OversightAspNetCoreMarker { }
