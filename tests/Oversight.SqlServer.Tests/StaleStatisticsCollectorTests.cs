using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class StaleStatisticsCollectorTests
{
    [Fact]
    public void Update_statistics_script_targets_schema_table_and_statistic() =>
        StaleStatisticsCollector.BuildUpdateStatistics("dbo", "Orders", "_WA_Sys_00000002")
            .ShouldBe("UPDATE STATISTICS [dbo].[Orders] ([_WA_Sys_00000002]);");

    [Theory]
    [InlineData(0.5, "Medium")]
    [InlineData(0.2, "Low")]
    public void Modification_ratio_maps_to_severity(double ratio, string expected) =>
        StaleStatisticsCollector.SeverityForRatio(ratio).ToString().ShouldBe(expected);
}
