using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;

namespace Oversight;

/// <summary>
/// SQL Server health monitoring: read-only DMV collectors exported as db.sqlserver.* metrics
/// and findings-as-logs. Opt-in: nothing runs until Oversight:SqlServer:Enabled is true and a
/// connection string name is configured. Not part of the Oversight meta-package.
/// </summary>
public static class OversightSqlServerBuilderExtensions
{
    public static IHostApplicationBuilder AddOversightSqlServer(
        this IHostApplicationBuilder builder,
        Action<OversightOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        OversightOptionsSetup.EnsureRegistered(builder, configure);
        AddOversightSqlServerInternal(builder, OversightOptionsSetup.ResolveSnapshot(builder.Configuration, configure));
        return builder;
    }

    internal static void AddOversightSqlServerInternal(IHostApplicationBuilder builder, OversightOptions snapshot)
    {
        if (!snapshot.SqlServer.Enabled)
            return;
        if (builder.Services.Any(d => d.ServiceType == typeof(OversightSqlServerMarker)))
            return;
        builder.Services.AddSingleton<OversightSqlServerMarker>();

        var name = snapshot.SqlServer.ConnectionStringName;
        if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(name)))
        {
            throw new InvalidOperationException(
                $"Oversight.SqlServer is enabled but no connection string named '{name}' exists. "
                + $"Add ConnectionStrings:{name} to configuration or set Oversight:SqlServer:ConnectionStringName.");
        }

        builder.Services.AddSingleton<HealthSnapshotCache>();
        builder.Services.AddSingleton<SqlServerHealthMetrics>();

        var collectors = snapshot.SqlServer.Collectors;
        if (collectors.BlockingSessions)
            builder.Services.AddSingleton<ISqlHealthCollector>(static _ => new BlockingSessionsCollector());
        if (collectors.LongRunningTransactions)
            builder.Services.AddSingleton<ISqlHealthCollector>(static _ => new LongRunningTransactionsCollector());
        if (collectors.MissingIndexes)
            builder.Services.AddSingleton<ISqlHealthCollector>(static _ => new MissingIndexesCollector());
        if (collectors.StaleStatistics)
            builder.Services.AddSingleton<ISqlHealthCollector>(static _ => new StaleStatisticsCollector());
        if (collectors.WaitStatistics)
            builder.Services.AddSingleton<ISqlHealthCollector>(static _ => new WaitStatisticsCollector());

        builder.Services.AddHostedService<SqlHealthCollectorService>();
        builder.Services.AddOpenTelemetry()
            .WithMetrics(static metrics => metrics.AddMeter(SqlServerHealthMetrics.MeterName));
    }
}

internal sealed class OversightSqlServerMarker { }
