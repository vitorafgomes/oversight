using System.Diagnostics.Metrics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class SqlHealthCollectorServiceTests
{
    [Fact]
    public async Task Unreachable_database_degrades_to_a_warning_and_no_data()
    {
        var cache = new HealthSnapshotCache();
        var logger = new FakeLogger<SqlHealthCollectorService>();
        using var provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var service = CreateService(
            cache,
            logger,
            provider,
            "Server=localhost,65535;User Id=sa;Password=none;Connect Timeout=1;TrustServerCertificate=true",
            [new BlockingSessionsCollector()]);

        await service.CollectOnceAsync(CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 5303 && r.Level == LogLevel.Warning);
        cache.BlockingSessions.ShouldBeNull();
    }

    [Fact]
    public async Task Failing_collector_keeps_the_last_reading_and_later_collectors_still_run()
    {
        var cache = new HealthSnapshotCache();
        cache.SetBlockingSessions(3);
        var logger = new FakeLogger<SqlHealthCollectorService>();
        using var provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var service = CreateService(cache, logger, provider, "Server=unused",
            [new ThrowingCollector(), new RecordingCollector()]);

        await using var connection = new SqlConnection();
        await service.RunCollectorsAsync(connection, CancellationToken.None);

        cache.BlockingSessions.ShouldBe(3L);
        cache.StaleStatisticsCount.ShouldBe(7L);
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 5302 && r.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Findings_are_emitted_through_the_logger()
    {
        var cache = new HealthSnapshotCache();
        var logger = new FakeLogger<SqlHealthCollectorService>();
        using var provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var service = CreateService(cache, logger, provider, "Server=unused", [new RecordingCollector()]);

        await using var connection = new SqlConnection();
        await service.RunCollectorsAsync(connection, CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 5301 && r.Message.Contains("recorded finding"));
    }

    private static SqlHealthCollectorService CreateService(
        HealthSnapshotCache cache,
        FakeLogger<SqlHealthCollectorService> logger,
        ServiceProvider meterFactoryProvider,
        string connectionString,
        ISqlHealthCollector[] collectors)
    {
        var options = Options.Create(new OversightOptions());
        options.Value.SqlServer.Enabled = true;
        options.Value.SqlServer.ConnectionStringName = "Monitored";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Monitored"] = connectionString,
        }).Build();
        var metrics = new SqlServerHealthMetrics(
            meterFactoryProvider.GetRequiredService<IMeterFactory>(), cache);
        return new SqlHealthCollectorService(options, configuration, cache, collectors, metrics, logger);
    }

    private sealed class ThrowingCollector : ISqlHealthCollector
    {
        public string Name => "throwing";

        public Task<IReadOnlyList<SqlHealthFinding>> CollectAsync(
            SqlConnection connection, HealthSnapshotCache cache, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("collector blew up");
    }

    private sealed class RecordingCollector : ISqlHealthCollector
    {
        public string Name => "recording";

        public Task<IReadOnlyList<SqlHealthFinding>> CollectAsync(
            SqlConnection connection, HealthSnapshotCache cache, CancellationToken cancellationToken)
        {
            cache.SetStaleStatistics(7);
            return Task.FromResult<IReadOnlyList<SqlHealthFinding>>(
                [new SqlHealthFinding("recording", SqlHealthSeverity.Low, "recorded finding", null)]);
        }
    }
}
