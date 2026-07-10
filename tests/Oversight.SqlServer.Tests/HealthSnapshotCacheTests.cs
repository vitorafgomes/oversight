using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class HealthSnapshotCacheTests
{
    [Fact]
    public void Every_reading_is_null_before_first_collection()
    {
        var cache = new HealthSnapshotCache();
        cache.BlockingSessions.ShouldBeNull();
        cache.LongRunningTransactions.ShouldBeNull();
        cache.MissingIndexCount.ShouldBeNull();
        cache.MissingIndexMaxImpact.ShouldBeNull();
        cache.StaleStatisticsCount.ShouldBeNull();
        cache.WaitStatistics.ShouldBeNull();
    }

    [Fact]
    public void Stores_blocking_sessions()
    {
        var cache = new HealthSnapshotCache();
        cache.SetBlockingSessions(4);
        cache.BlockingSessions.ShouldBe(4L);
    }

    [Fact]
    public void Stores_long_running_transactions()
    {
        var cache = new HealthSnapshotCache();
        cache.SetLongRunningTransactions(2);
        cache.LongRunningTransactions.ShouldBe(2L);
    }

    [Fact]
    public void Stores_missing_indexes_count_and_max_impact()
    {
        var cache = new HealthSnapshotCache();
        cache.SetMissingIndexes(3, 87.5);
        cache.MissingIndexCount.ShouldBe(3L);
        cache.MissingIndexMaxImpact.ShouldBe(87.5);
    }

    [Fact]
    public void Stores_stale_statistics_count()
    {
        var cache = new HealthSnapshotCache();
        cache.SetStaleStatistics(9);
        cache.StaleStatisticsCount.ShouldBe(9L);
    }

    [Fact]
    public void Stores_wait_statistics()
    {
        var cache = new HealthSnapshotCache();
        cache.SetWaitStatistics([new WaitTypeWait("PAGEIOLATCH_SH", 1234)]);
        cache.WaitStatistics.ShouldNotBeNull();
        cache.WaitStatistics.ShouldHaveSingleItem().ShouldBe(new WaitTypeWait("PAGEIOLATCH_SH", 1234));
    }

    [Fact]
    public void A_new_reading_does_not_disturb_other_readings()
    {
        var cache = new HealthSnapshotCache();
        cache.SetBlockingSessions(2);
        cache.SetStaleStatistics(9);
        cache.BlockingSessions.ShouldBe(2L);
        cache.StaleStatisticsCount.ShouldBe(9L);
    }
}
