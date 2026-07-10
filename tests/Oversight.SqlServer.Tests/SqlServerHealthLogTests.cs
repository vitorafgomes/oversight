using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class SqlServerHealthLogTests
{
    [Fact]
    public void High_severity_findings_log_as_warning()
    {
        var logger = new FakeLogger();
        SqlServerHealthLog.EmitFinding(logger, new SqlHealthFinding(
            "missing_indexes", SqlHealthSeverity.High, "Missing index on dbo.Orders", "CREATE INDEX IX_Oversight_Orders ON dbo.Orders (CustomerId);"));

        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Id.Id.ShouldBe(5301);
        record.Message.ShouldContain("Missing index on dbo.Orders");
    }

    [Fact]
    public void Medium_severity_findings_log_as_information()
    {
        var logger = new FakeLogger();
        SqlServerHealthLog.EmitFinding(logger, new SqlHealthFinding(
            "stale_statistics", SqlHealthSeverity.Medium, "Stale statistics", "UPDATE STATISTICS [dbo].[Orders];"));

        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public void Low_severity_findings_log_as_information()
    {
        var logger = new FakeLogger();
        SqlServerHealthLog.EmitFinding(logger, new SqlHealthFinding(
            "blocking_sessions", SqlHealthSeverity.Low, "Short block", null));

        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public void Finding_carries_collector_and_script_as_structured_state()
    {
        var logger = new FakeLogger();
        SqlServerHealthLog.EmitFinding(logger, new SqlHealthFinding(
            "missing_indexes", SqlHealthSeverity.High, "Missing index", "CREATE INDEX X ON T (C);"));

        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.StructuredState.ShouldNotBeNull();
        record.StructuredState!.Any(kv => kv.Key == "Collector" && kv.Value == "missing_indexes").ShouldBeTrue();
        record.StructuredState!.Any(kv => kv.Key == "SuggestedScript" && kv.Value == "CREATE INDEX X ON T (C);").ShouldBeTrue();
    }

    [Fact]
    public void Collector_failure_logs_a_warning_with_the_collector_name()
    {
        var logger = new FakeLogger();
        SqlServerHealthLog.CollectorFailed(logger, "wait_statistics", "boom", new InvalidOperationException("boom"));

        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Id.Id.ShouldBe(5302);
        record.Message.ShouldContain("wait_statistics");
    }
}
