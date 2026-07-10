using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class MissingIndexesCollectorTests
{
    [Fact]
    public void Create_index_script_includes_key_and_included_columns() =>
        MissingIndexesCollector.BuildCreateIndex("[dbo].[Orders]", "[CustomerId]", "[CreatedAt]", "[Total]")
            .ShouldBe("CREATE INDEX IX_Oversight_Orders_CustomerIdCreatedAt ON [dbo].[Orders] ([CustomerId], [CreatedAt]) INCLUDE ([Total]);");

    [Fact]
    public void Create_index_script_omits_include_when_no_included_columns() =>
        MissingIndexesCollector.BuildCreateIndex("[dbo].[Orders]", "[CustomerId]", "", "")
            .ShouldBe("CREATE INDEX IX_Oversight_Orders_CustomerId ON [dbo].[Orders] ([CustomerId]);");

    [Fact]
    public void Create_index_name_is_capped_at_120_characters()
    {
        var wideColumn = "[" + new string('C', 200) + "]";
        var script = MissingIndexesCollector.BuildCreateIndex("[dbo].[Orders]", wideColumn, "", "");
        var name = script.Split(' ')[2];
        name.Length.ShouldBe(120);
        name.ShouldStartWith("IX_Oversight_Orders_");
    }

    [Theory]
    [InlineData(90, "High")]
    [InlineData(70, "Medium")]
    [InlineData(50, "Low")]
    public void Estimated_impact_maps_to_severity(double impact, string expected) =>
        MissingIndexesCollector.SeverityForImpact(impact).ToString().ShouldBe(expected);
}
