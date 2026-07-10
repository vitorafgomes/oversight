using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class WaitStatisticsCollectorTests
{
    [Theory]
    [InlineData(5, "sys.dm_db_wait_stats")]
    [InlineData(3, "sys.dm_os_wait_stats")]
    [InlineData(8, "sys.dm_os_wait_stats")]
    public void Engine_edition_picks_the_wait_stats_view(int engineEdition, string expected) =>
        WaitStatisticsCollector.WaitStatsViewFor(engineEdition).ShouldBe(expected);

    [Fact]
    public void Dominant_known_wait_produces_a_medium_finding_with_its_cause()
    {
        var finding = WaitStatisticsCollector.DominantWaitFinding(
        [
            new WaitStatisticsCollector.WaitReading("WRITELOG", 8000),
            new WaitStatisticsCollector.WaitReading("CXPACKET", 2000),
        ]);

        finding.ShouldNotBeNull();
        finding.Severity.ToString().ShouldBe("Medium");
        finding.Title.ShouldContain("WRITELOG");
        finding.Title.ShouldContain("Log flush waits");
    }

    [Fact]
    public void Dominant_unknown_wait_uses_the_fallback_cause()
    {
        var finding = WaitStatisticsCollector.DominantWaitFinding(
            [new WaitStatisticsCollector.WaitReading("SOME_EXOTIC_WAIT", 9000)]);

        finding.ShouldNotBeNull();
        finding.Title.ShouldContain("Uncategorized wait");
    }

    [Fact]
    public void Balanced_waits_produce_no_finding()
    {
        var finding = WaitStatisticsCollector.DominantWaitFinding(
        [
            new WaitStatisticsCollector.WaitReading("WRITELOG", 3000),
            new WaitStatisticsCollector.WaitReading("CXPACKET", 3000),
            new WaitStatisticsCollector.WaitReading("PAGEIOLATCH_SH", 3000),
        ]);

        finding.ShouldBeNull();
    }

    [Fact]
    public void No_waits_produce_no_finding() =>
        WaitStatisticsCollector.DominantWaitFinding([]).ShouldBeNull();
}
