using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using Xunit;

namespace Farol.IntegrationTests;

[CollectionDefinition("OtlpEnvironment", DisableParallelization = true)]
public sealed class OtlpEnvironmentCollection { }

[Collection("OtlpEnvironment")]
public class OtlpBoundaryTests
{
    [Fact]
    public async Task Unreachable_otlp_endpoint_does_not_break_the_app()
    {
        using var scope = new EnvironmentVariableScope(
            ("OTEL_EXPORTER_OTLP_ENDPOINT", "http://127.0.0.1:59999"));
        List<Activity> exported = [];
        await using var factory = FarolFactory.Create(exported);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("hello");
    }

    [Fact]
    public async Task Missing_otlp_endpoint_logs_the_no_destination_warning()
    {
        using var scope = new EnvironmentVariableScope(
            ("OTEL_EXPORTER_OTLP_ENDPOINT", null),
            ("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", null),
            ("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", null));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddFakeLogging()));
        _ = factory.CreateClient();

        var collector = factory.Services.GetRequiredService<FakeLogCollector>();

        collector.GetSnapshot().ShouldContain(r =>
            r.Level == LogLevel.Warning && r.Message.Contains("telemetry has no destination"));
    }
}
