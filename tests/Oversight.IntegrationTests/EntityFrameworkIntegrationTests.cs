using System.Diagnostics;
using Shouldly;
using Xunit;

namespace Oversight.IntegrationTests;

public class EntityFrameworkIntegrationTests
{
    private const string EfSourceName = "OpenTelemetry.Instrumentation.EntityFrameworkCore";

    [Fact]
    public async Task Ef_spans_have_no_query_text_by_default()
    {
        List<Activity> exported = [];
        await using var factory = OversightFactory.Create(exported);
        var client = factory.CreateClient();

        (await client.GetAsync("/api/todos")).EnsureSuccessStatusCode();
        OversightFactory.Flush(factory);

        var efSpans = exported.Where(a => a.Source.Name == EfSourceName).ToList();
        efSpans.ShouldNotBeEmpty();
        efSpans.ShouldAllBe(a =>
            a.GetTagItem("db.query.text") == null && a.GetTagItem("db.statement") == null);
    }

    [Fact]
    public async Task Ef_spans_carry_query_text_when_opted_in()
    {
        List<Activity> exported = [];
        await using var factory = OversightFactory.Create(
            exported, ("Oversight:EntityFrameworkCore:CaptureQueryText", "true"));
        var client = factory.CreateClient();

        (await client.GetAsync("/api/todos")).EnsureSuccessStatusCode();
        OversightFactory.Flush(factory);

        var queryTexts = exported
            .Where(a => a.Source.Name == EfSourceName)
            .Select(a => (a.GetTagItem("db.query.text") ?? a.GetTagItem("db.statement")) as string)
            .Where(t => t is not null)
            .ToList();
        queryTexts.ShouldContain(t => t!.Contains("Todos"));
    }
}
