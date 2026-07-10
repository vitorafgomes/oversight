using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class SqlServerHealthMetricsTests
{
    [Fact]
    public void Gauges_emit_nothing_before_the_first_collection()
    {
        using var provider = MeterFactoryProvider();
        var metrics = new SqlServerHealthMetrics(
            provider.GetRequiredService<IMeterFactory>(), new HealthSnapshotCache());
        using var reader = new GaugeReader(metrics.Meter);

        reader.Record();

        reader.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public void Blocking_sessions_gauge_reflects_the_cache()
    {
        var cache = new HealthSnapshotCache();
        cache.SetBlockingSessions(4);
        using var provider = MeterFactoryProvider();
        var metrics = new SqlServerHealthMetrics(provider.GetRequiredService<IMeterFactory>(), cache);
        using var reader = new GaugeReader(metrics.Meter);

        reader.Record();

        reader.Measurements.ShouldContain(m => m.Instrument == "db.sqlserver.blocking.sessions" && m.Value == 4);
    }

    [Fact]
    public void Missing_index_gauges_reflect_count_and_max_impact()
    {
        var cache = new HealthSnapshotCache();
        cache.SetMissingIndexes(3, 87.5);
        using var provider = MeterFactoryProvider();
        var metrics = new SqlServerHealthMetrics(provider.GetRequiredService<IMeterFactory>(), cache);
        using var reader = new GaugeReader(metrics.Meter);

        reader.Record();

        reader.Measurements.ShouldContain(m => m.Instrument == "db.sqlserver.missing_indexes.count" && m.Value == 3);
        reader.Measurements.ShouldContain(m => m.Instrument == "db.sqlserver.missing_indexes.max_impact" && m.Value == 87.5);
    }

    [Fact]
    public void Transaction_and_statistics_gauges_reflect_the_cache()
    {
        var cache = new HealthSnapshotCache();
        cache.SetLongRunningTransactions(2);
        cache.SetStaleStatistics(9);
        using var provider = MeterFactoryProvider();
        var metrics = new SqlServerHealthMetrics(provider.GetRequiredService<IMeterFactory>(), cache);
        using var reader = new GaugeReader(metrics.Meter);

        reader.Record();

        reader.Measurements.ShouldContain(m => m.Instrument == "db.sqlserver.transactions.long_running" && m.Value == 2);
        reader.Measurements.ShouldContain(m => m.Instrument == "db.sqlserver.statistics.stale.count" && m.Value == 9);
    }

    [Fact]
    public void Wait_gauge_emits_one_measurement_per_wait_type_with_the_attribute()
    {
        var cache = new HealthSnapshotCache();
        cache.SetWaitStatistics(
        [
            new WaitTypeWait("PAGEIOLATCH_SH", 1200),
            new WaitTypeWait("WRITELOG", 800),
        ]);
        using var provider = MeterFactoryProvider();
        var metrics = new SqlServerHealthMetrics(provider.GetRequiredService<IMeterFactory>(), cache);
        using var reader = new GaugeReader(metrics.Meter);

        reader.Record();

        var waits = reader.Measurements.Where(m => m.Instrument == "db.sqlserver.waits.time").ToList();
        waits.Count.ShouldBe(2);
        waits.ShouldContain(m => m.Value == 1200
            && m.Tags.Any(t => t.Key == "db.sqlserver.wait_type" && Equals(t.Value, "PAGEIOLATCH_SH")));
        waits.ShouldContain(m => m.Value == 800
            && m.Tags.Any(t => t.Key == "db.sqlserver.wait_type" && Equals(t.Value, "WRITELOG")));
    }

    private static ServiceProvider MeterFactoryProvider() =>
        new ServiceCollection().AddMetrics().BuildServiceProvider();

    private sealed record Reading(string Instrument, double Value, KeyValuePair<string, object?>[] Tags);

    // Filters by the exact Meter instance so parallel tests with their own caches never interfere.
    private sealed class GaugeReader : IDisposable
    {
        private readonly MeterListener _listener = new();

        public List<Reading> Measurements { get; } = [];

        public GaugeReader(Meter meter)
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, meter))
                    listener.EnableMeasurementEvents(instrument);
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
                Measurements.Add(new Reading(instrument.Name, value, tags.ToArray())));
            _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
                Measurements.Add(new Reading(instrument.Name, value, tags.ToArray())));
            _listener.Start();
        }

        public void Record()
        {
            Measurements.Clear();
            _listener.RecordObservableInstruments();
        }

        public void Dispose() => _listener.Dispose();
    }
}
