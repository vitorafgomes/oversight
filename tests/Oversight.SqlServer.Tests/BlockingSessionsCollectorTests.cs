using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class BlockingSessionsCollectorTests
{
    [Theory]
    [InlineData(60000, "High")]
    [InlineData(20000, "Medium")]
    [InlineData(5000, "Low")]
    public void Wait_time_maps_to_severity(long waitMilliseconds, string expected) =>
        BlockingSessionsCollector.SeverityForWait(waitMilliseconds).ToString().ShouldBe(expected);
}
