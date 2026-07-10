using System.Diagnostics;
using System.Net;
using Shouldly;
using Xunit;

namespace Oversight.IntegrationTests;

public class PrometheusIntegrationTests
{
    [Fact]
    public async Task Metrics_endpoint_is_not_mapped_by_default()
    {
        List<Activity> exported = [];
        await using var factory = OversightFactory.Create(exported);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/metrics");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Metrics_endpoint_serves_prometheus_text_when_enabled()
    {
        List<Activity> exported = [];
        await using var factory = OversightFactory.Create(exported, ("Oversight:Prometheus:Enabled", "true"));
        var client = factory.CreateClient();

        (await client.GetAsync("/api/hello")).EnsureSuccessStatusCode();
        var response = await client.GetAsync("/metrics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("# TYPE");
        body.ShouldContain("http_server_request_duration");
    }
}
