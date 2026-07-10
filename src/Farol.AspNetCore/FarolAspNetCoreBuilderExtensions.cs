using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Farol;

/// <summary>ASP.NET Core layer: server traces with noise reduction, server metrics, optional Prometheus endpoint.</summary>
public static class FarolAspNetCoreBuilderExtensions
{
    /// <remarks>
    /// Farol composes with instrumentation delegates configured before it runs rather than
    /// replacing them; any previously registered request filter must also pass for a request
    /// to be traced.
    /// </remarks>
    public static IHostApplicationBuilder AddFarolAspNetCore(
        this IHostApplicationBuilder builder,
        Action<FarolOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        FarolOptionsSetup.EnsureRegistered(builder, configure);
        AddFarolAspNetCoreInternal(builder, FarolOptionsSetup.ResolveSnapshot(builder.Configuration, configure));
        return builder;
    }

    internal static void AddFarolAspNetCoreInternal(IHostApplicationBuilder builder, FarolOptions snapshot)
    {
        if (builder.Services.Any(d => d.ServiceType == typeof(FarolAspNetCoreMarker)))
            return;
        builder.Services.AddSingleton<FarolAspNetCoreMarker>();

        builder.Services.AddOptions<AspNetCoreTraceInstrumentationOptions>()
            .Configure<IOptions<FarolOptions>>(static (instrumentation, farol) =>
            {
                var matcher = new PathGlobMatcher(farol.Value.NoiseReduction.ExcludedPaths);
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
            builder.Services.AddTransient<IStartupFilter, FarolPrometheusStartupFilter>();
        }
    }
}

internal sealed class FarolAspNetCoreMarker { }
