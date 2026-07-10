using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class LongRunningTransactionsCollectorTests
{
    [Theory]
    [InlineData(600, "High")]
    [InlineData(180, "Medium")]
    [InlineData(60, "Low")]
    public void Open_duration_maps_to_severity(long durationSeconds, string expected) =>
        LongRunningTransactionsCollector.SeverityForDuration(durationSeconds).ToString().ShouldBe(expected);
}
