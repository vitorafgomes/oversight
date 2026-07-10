using System.Diagnostics.Metrics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

[Collection("sqlserver-container")]
public class SqlServerCollectorIntegrationTests(SqlServerContainerFixture fixture)
{
    private const string DockerSkipReason = "Docker is not available; SQL Server container tests skipped.";

    [Fact]
    public async Task Blocking_sessions_collector_detects_a_real_block()
    {
        Assert.SkipUnless(fixture.IsAvailable, DockerSkipReason);
        await ExecuteAsync("CREATE TABLE blk_rows (id INT PRIMARY KEY, val INT); INSERT INTO blk_rows VALUES (1, 0);");

        await using var holder = await OpenConnectionAsync();
        var transaction = (SqlTransaction)await holder.BeginTransactionAsync();
        await using (var lockCommand = holder.CreateCommand())
        {
            lockCommand.Transaction = transaction;
            lockCommand.CommandText = "UPDATE blk_rows SET val = 1 WHERE id = 1;";
            await lockCommand.ExecuteNonQueryAsync();
        }
        var blockedTask = Task.Run(async () =>
        {
            await using var blocked = new SqlConnection(fixture.ConnectionString);
            await blocked.OpenAsync();
            await using var command = blocked.CreateCommand();
            command.CommandText = "UPDATE blk_rows SET val = 2 WHERE id = 1;";
            command.CommandTimeout = 30;
            await command.ExecuteNonQueryAsync();
        });
        await Task.Delay(TimeSpan.FromSeconds(2));

        var cache = new HealthSnapshotCache();
        await using var observer = await OpenConnectionAsync();
        var findings = await new BlockingSessionsCollector(minWaitMilliseconds: 100)
            .CollectAsync(observer, cache, CancellationToken.None);

        await transaction.RollbackAsync();
        await blockedTask;

        cache.BlockingSessions.ShouldNotBeNull();
        cache.BlockingSessions.Value.ShouldBeGreaterThanOrEqualTo(1L);
        findings.ShouldContain(f => f.Collector == "blocking_sessions");
    }

    [Fact]
    public async Task Long_running_transactions_collector_detects_an_open_transaction()
    {
        Assert.SkipUnless(fixture.IsAvailable, DockerSkipReason);
        await ExecuteAsync("CREATE TABLE lt_rows (id INT PRIMARY KEY, val INT); INSERT INTO lt_rows VALUES (1, 0);");

        await using var holder = await OpenConnectionAsync();
        var transaction = (SqlTransaction)await holder.BeginTransactionAsync();
        await using (var command = holder.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "UPDATE lt_rows SET val = 1 WHERE id = 1;";
            await command.ExecuteNonQueryAsync();
        }
        await Task.Delay(TimeSpan.FromSeconds(2));

        var cache = new HealthSnapshotCache();
        await using var observer = await OpenConnectionAsync();
        var findings = await new LongRunningTransactionsCollector(minDurationSeconds: 1)
            .CollectAsync(observer, cache, CancellationToken.None);

        await transaction.RollbackAsync();

        cache.LongRunningTransactions.ShouldNotBeNull();
        cache.LongRunningTransactions.Value.ShouldBeGreaterThanOrEqualTo(1L);
        findings.ShouldContain(f => f.Collector == "long_running_transactions");
    }

    [Fact]
    public async Task Missing_indexes_collector_reports_suggestions_with_create_index_script()
    {
        Assert.SkipUnless(fixture.IsAvailable, DockerSkipReason);
        await ExecuteAsync("""
            CREATE TABLE mi_orders (id INT IDENTITY PRIMARY KEY, customer_id INT NOT NULL, padding CHAR(200) NOT NULL DEFAULT 'x');
            INSERT INTO mi_orders (customer_id)
            SELECT TOP (50000) ABS(CHECKSUM(NEWID())) % 1000
            FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b;
            """);
        // A wide, row-returning predicate forces full cost-based optimization; SQL Server does not
        // record missing-index suggestions for the trivial plan a bare COUNT(*) would produce.
        for (var i = 0; i < 10; i++)
            await ExecuteAsync("SELECT id, padding FROM mi_orders WHERE customer_id = 42 ORDER BY padding;");

        var cache = new HealthSnapshotCache();
        await using var connection = await OpenConnectionAsync();
        var findings = await new MissingIndexesCollector(minAverageImpact: 0)
            .CollectAsync(connection, cache, CancellationToken.None);

        cache.MissingIndexCount.ShouldNotBeNull();
        cache.MissingIndexCount.Value.ShouldBeGreaterThanOrEqualTo(1L);
        cache.MissingIndexMaxImpact.ShouldNotBeNull();
        findings.ShouldContain(f => f.SuggestedScript != null && f.SuggestedScript.StartsWith("CREATE INDEX IX_Oversight_"));
    }

    [Fact]
    public async Task Stale_statistics_collector_flags_heavily_modified_statistics()
    {
        Assert.SkipUnless(fixture.IsAvailable, DockerSkipReason);
        await ExecuteAsync("""
            CREATE TABLE ss_events (id INT IDENTITY PRIMARY KEY, val INT NOT NULL);
            INSERT INTO ss_events (val)
            SELECT TOP (2000) ABS(CHECKSUM(NEWID())) % 100
            FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b;
            """);
        await ExecuteAsync("SELECT COUNT(*) FROM ss_events WHERE val = 1;");
        await ExecuteAsync("UPDATE ss_events SET val = val + 1;");

        var cache = new HealthSnapshotCache();
        await using var connection = await OpenConnectionAsync();
        var findings = await new StaleStatisticsCollector(minRows: 1000, minModificationRatio: 0.2)
            .CollectAsync(connection, cache, CancellationToken.None);

        cache.StaleStatisticsCount.ShouldNotBeNull();
        cache.StaleStatisticsCount.Value.ShouldBeGreaterThanOrEqualTo(1L);
        findings.ShouldContain(f => f.SuggestedScript != null && f.SuggestedScript.StartsWith("UPDATE STATISTICS"));
    }

    [Fact]
    public async Task Wait_statistics_collector_reads_server_waits()
    {
        Assert.SkipUnless(fixture.IsAvailable, DockerSkipReason);

        var cache = new HealthSnapshotCache();
        await using var connection = await OpenConnectionAsync();
        await new WaitStatisticsCollector().CollectAsync(connection, cache, CancellationToken.None);

        cache.WaitStatistics.ShouldNotBeNull();
    }

    [Fact]
    public async Task Full_cycle_updates_every_reading()
    {
        Assert.SkipUnless(fixture.IsAvailable, DockerSkipReason);

        var options = Options.Create(new OversightOptions());
        options.Value.SqlServer.Enabled = true;
        options.Value.SqlServer.ConnectionStringName = "Monitored";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Monitored"] = fixture.ConnectionString,
        }).Build();
        var cache = new HealthSnapshotCache();
        using var provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var metrics = new SqlServerHealthMetrics(provider.GetRequiredService<IMeterFactory>(), cache);
        ISqlHealthCollector[] collectors =
        [
            new BlockingSessionsCollector(),
            new LongRunningTransactionsCollector(),
            new MissingIndexesCollector(),
            new StaleStatisticsCollector(),
            new WaitStatisticsCollector(),
        ];
        var service = new SqlHealthCollectorService(
            options, configuration, cache, collectors, metrics, new FakeLogger<SqlHealthCollectorService>());

        await service.CollectOnceAsync(CancellationToken.None);

        cache.BlockingSessions.ShouldNotBeNull();
        cache.LongRunningTransactions.ShouldNotBeNull();
        cache.MissingIndexCount.ShouldNotBeNull();
        cache.MissingIndexMaxImpact.ShouldNotBeNull();
        cache.StaleStatisticsCount.ShouldNotBeNull();
        cache.WaitStatistics.ShouldNotBeNull();
    }

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;
        await command.ExecuteNonQueryAsync();
    }
}
