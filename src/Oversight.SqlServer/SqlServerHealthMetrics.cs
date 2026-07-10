using System.Diagnostics.Metrics;

namespace Oversight;

/// <summary>
/// db.sqlserver.* gauges. They read only the snapshot cache — never the database — so a
/// metrics export can never touch SQL Server. A null reading emits no measurement.
/// </summary>
internal sealed class SqlServerHealthMetrics
{
    internal const string MeterName = "Oversight.SqlServer";

    internal Meter Meter { get; }

    public SqlServerHealthMetrics(IMeterFactory meterFactory, HealthSnapshotCache cache)
    {
        Meter = meterFactory.Create(MeterName);
        Meter.CreateObservableGauge(
            "db.sqlserver.blocking.sessions",
            () => Observe(cache.BlockingSessions),
            unit: "{session}",
            description: "Sessions blocked by another session for at least 5 seconds, at the last collection.");
        Meter.CreateObservableGauge(
            "db.sqlserver.transactions.long_running",
            () => Observe(cache.LongRunningTransactions),
            unit: "{transaction}",
            description: "Transactions open longer than 60 seconds, at the last collection.");
        Meter.CreateObservableGauge(
            "db.sqlserver.missing_indexes.count",
            () => Observe(cache.MissingIndexCount),
            unit: "{index}",
            description: "Missing-index suggestions with optimizer-estimated impact of at least 50%.");
        Meter.CreateObservableGauge(
            "db.sqlserver.missing_indexes.max_impact",
            () => Observe(cache.MissingIndexMaxImpact),
            unit: "%",
            description: "Highest optimizer-estimated improvement among current missing-index suggestions; 0 when none.");
        Meter.CreateObservableGauge(
            "db.sqlserver.statistics.stale.count",
            () => Observe(cache.StaleStatisticsCount),
            unit: "{statistic}",
            description: "Statistics whose modification counter is at least 20% of row count (tables with 1000+ rows).");
        Meter.CreateObservableGauge(
            "db.sqlserver.waits.time",
            () => ObserveWaits(cache.WaitStatistics),
            unit: "ms",
            description: "Cumulative wait time per wait type since server start; top waits only, benign waits filtered.");
    }

    private static IEnumerable<Measurement<long>> Observe(long? value) =>
        value is null ? [] : [new Measurement<long>(value.Value)];

    private static IEnumerable<Measurement<double>> Observe(double? value) =>
        value is null ? [] : [new Measurement<double>(value.Value)];

    private static IEnumerable<Measurement<long>> ObserveWaits(IReadOnlyList<WaitTypeWait>? waits) =>
        waits is null
            ? []
            : waits.Select(static wait => new Measurement<long>(
                wait.WaitTimeMilliseconds,
                new KeyValuePair<string, object?>("db.sqlserver.wait_type", wait.WaitType)));
}
