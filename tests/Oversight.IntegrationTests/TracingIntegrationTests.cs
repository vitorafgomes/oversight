using System.Diagnostics;
using System.Net;
using Shouldly;
using Xunit;

namespace Oversight.IntegrationTests;

public class TracingIntegrationTests
{
    [Fact]
    public async Task Health_request_produces_no_server_span()
    {
        List<Activity> exported = [];
        await using var factory = OversightFactory.Create(exported);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        OversightFactory.Flush(factory);
        exported.Where(a => a.Kind == ActivityKind.Server).ShouldBeEmpty();
    }

    [Fact]
    public async Task Normal_route_produces_a_server_span_with_http_route()
    {
        List<Activity> exported = [];
        await using var factory = OversightFactory.Create(exported);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        OversightFactory.Flush(factory);
        var serverSpan = exported.Single(a => a.Kind == ActivityKind.Server);
        serverSpan.GetTagItem("http.route").ShouldBe("/api/hello");
    }

    [Fact]
    public async Task Custom_excluded_glob_from_configuration_is_honored()
    {
        List<Activity> exported = [];
        await using var factory = OversightFactory.Create(
            exported, ("Oversight:NoiseReduction:ExcludedPaths:0", "/api/hello"));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        OversightFactory.Flush(factory);
        exported.Where(a => a.Kind == ActivityKind.Server).ShouldBeEmpty();
    }
}
