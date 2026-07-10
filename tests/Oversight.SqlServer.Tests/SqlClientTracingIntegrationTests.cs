using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

// Closes v1 test-plan gap G4: SqlClient-sourced spans asserted against a real SQL Server.
[Collection("sqlserver-container")]
public class SqlClientTracingIntegrationTests(SqlServerContainerFixture fixture)
{
    private const string DockerSkipReason = "Docker is not available; SQL Server container tests skipped.";
    private const string SqlClientSourceName = "OpenTelemetry.Instrumentation.SqlClient";

    [Fact]
    public async Task Sql_client_spans_have_no_query_text_by_default()
    {
        Assert.SkipUnless(fixture.IsAvailable, DockerSkipReason);

        var exported = await ExportSpansAsync(configure: null);

        var sqlSpans = exported.Where(a => a.Source.Name == SqlClientSourceName).ToList();
        sqlSpans.ShouldNotBeEmpty();
        sqlSpans.ShouldAllBe(a => a.GetTagItem("db.query.text") == null && a.GetTagItem("db.statement") == null);
    }

    [Fact]
    public async Task Sql_client_spans_carry_query_text_when_opted_in()
    {
        Assert.SkipUnless(fixture.IsAvailable, DockerSkipReason);

        var exported = await ExportSpansAsync(static oversight =>
            oversight.EntityFrameworkCore.CaptureQueryText = true);

        // SqlClient instrumentation (1.16.0) emits sanitized query text, so literals collapse to
        // "?"; opt-in is verified by the statement being present at all, not by the raw literal.
        exported.Where(a => a.Source.Name == SqlClientSourceName)
            .Select(a => (a.GetTagItem("db.query.text") ?? a.GetTagItem("db.statement")) as string)
            .ShouldContain(text => text != null && text.Contains("SELECT", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<Activity>> ExportSpansAsync(Action<OversightOptions>? configure)
    {
        List<Activity> exported = [];
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightEntityFrameworkCore(configure);
        builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddInMemoryExporter(exported));
        using var host = builder.Build();
        await host.StartAsync();

        await using (var connection = new SqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync();
        }

        host.Services.GetRequiredService<TracerProvider>().ForceFlush();
        await host.StopAsync();
        return exported;
    }
}
