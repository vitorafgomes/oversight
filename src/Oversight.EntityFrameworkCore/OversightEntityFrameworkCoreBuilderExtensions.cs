using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.SqlClient;
using OpenTelemetry.Trace;

namespace Oversight;

/// <summary>Database layer: EF Core and SqlClient traces; query text capture is opt-in.</summary>
public static class OversightEntityFrameworkCoreBuilderExtensions
{
    /// <remarks>
    /// Oversight composes with instrumentation delegates configured before it runs rather than
    /// replacing them; any previously registered enrich callback is invoked first.
    /// </remarks>
    public static IHostApplicationBuilder AddOversightEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<OversightOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        OversightOptionsSetup.EnsureRegistered(builder, configure);
        AddOversightEntityFrameworkCoreInternal(builder, OversightOptionsSetup.ResolveSnapshot(builder.Configuration, configure));
        return builder;
    }

    internal static void AddOversightEntityFrameworkCoreInternal(IHostApplicationBuilder builder, OversightOptions snapshot)
    {
        if (!snapshot.EntityFrameworkCore.Enabled)
            return;
        if (builder.Services.Any(d => d.ServiceType == typeof(OversightEntityFrameworkCoreMarker)))
            return;
        builder.Services.AddSingleton<OversightEntityFrameworkCoreMarker>();

        // Upstream instrumentation always emits sanitized db.query.text (the opt-out was
        // removed in 1.13.0-beta.1); the enrich callback runs inside the instrumentation
        // before export, so nulling the tag there restores Oversight's off-by-default.
        builder.Services.AddOptions<EntityFrameworkInstrumentationOptions>()
            .Configure<IOptions<OversightOptions>>((instrumentation, oversight) =>
            {
                if (!oversight.Value.EntityFrameworkCore.CaptureQueryText)
                {
                    var existing = instrumentation.EnrichWithIDbCommand;
                    instrumentation.EnrichWithIDbCommand = (activity, command) =>
                    {
                        existing?.Invoke(activity, command);
                        OversightDbQueryTextScrubber.Scrub(activity);
                    };
                }
            });

        builder.Services.AddOptions<SqlClientTraceInstrumentationOptions>()
            .Configure<IOptions<OversightOptions>>((instrumentation, oversight) =>
            {
                if (!oversight.Value.EntityFrameworkCore.CaptureQueryText)
                {
                    var existing = instrumentation.EnrichWithSqlCommand;
                    instrumentation.EnrichWithSqlCommand = (activity, command) =>
                    {
                        existing?.Invoke(activity, command);
                        OversightDbQueryTextScrubber.Scrub(activity);
                    };
                }
            });

        builder.Services.AddOpenTelemetry().WithTracing(static tracing => tracing
            .AddEntityFrameworkCoreInstrumentation()
            .AddSqlClientInstrumentation());
    }
}

internal sealed class OversightEntityFrameworkCoreMarker { }

internal static class OversightDbQueryTextScrubber
{
    internal static void Scrub(Activity activity)
    {
        activity.SetTag("db.query.text", null);
        activity.SetTag("db.statement", null);
    }
}
