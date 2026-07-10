using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.SqlClient;
using OpenTelemetry.Trace;

namespace Farol;

/// <summary>Database layer: EF Core and SqlClient traces; query text capture is opt-in.</summary>
public static class FarolEntityFrameworkCoreBuilderExtensions
{
    /// <remarks>
    /// Farol composes with instrumentation delegates configured before it runs rather than
    /// replacing them; any previously registered enrich callback is invoked first.
    /// </remarks>
    public static IHostApplicationBuilder AddFarolEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<FarolOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        FarolOptionsSetup.EnsureRegistered(builder, configure);
        AddFarolEntityFrameworkCoreInternal(builder, FarolOptionsSetup.ResolveSnapshot(builder.Configuration, configure));
        return builder;
    }

    internal static void AddFarolEntityFrameworkCoreInternal(IHostApplicationBuilder builder, FarolOptions snapshot)
    {
        if (!snapshot.EntityFrameworkCore.Enabled)
            return;
        if (builder.Services.Any(d => d.ServiceType == typeof(FarolEntityFrameworkCoreMarker)))
            return;
        builder.Services.AddSingleton<FarolEntityFrameworkCoreMarker>();

        // Upstream instrumentation always emits sanitized db.query.text (the opt-out was
        // removed in 1.13.0-beta.1); the enrich callback runs inside the instrumentation
        // before export, so nulling the tag there restores Farol's off-by-default.
        builder.Services.AddOptions<EntityFrameworkInstrumentationOptions>()
            .Configure<IOptions<FarolOptions>>((instrumentation, farol) =>
            {
                if (!farol.Value.EntityFrameworkCore.CaptureQueryText)
                {
                    var existing = instrumentation.EnrichWithIDbCommand;
                    instrumentation.EnrichWithIDbCommand = (activity, command) =>
                    {
                        existing?.Invoke(activity, command);
                        FarolDbQueryTextScrubber.Scrub(activity);
                    };
                }
            });

        builder.Services.AddOptions<SqlClientTraceInstrumentationOptions>()
            .Configure<IOptions<FarolOptions>>((instrumentation, farol) =>
            {
                if (!farol.Value.EntityFrameworkCore.CaptureQueryText)
                {
                    var existing = instrumentation.EnrichWithSqlCommand;
                    instrumentation.EnrichWithSqlCommand = (activity, command) =>
                    {
                        existing?.Invoke(activity, command);
                        FarolDbQueryTextScrubber.Scrub(activity);
                    };
                }
            });

        builder.Services.AddOpenTelemetry().WithTracing(static tracing => tracing
            .AddEntityFrameworkCoreInstrumentation()
            .AddSqlClientInstrumentation());
    }
}

internal sealed class FarolEntityFrameworkCoreMarker { }

internal static class FarolDbQueryTextScrubber
{
    internal static void Scrub(Activity activity)
    {
        activity.SetTag("db.query.text", null);
        activity.SetTag("db.statement", null);
    }
}
