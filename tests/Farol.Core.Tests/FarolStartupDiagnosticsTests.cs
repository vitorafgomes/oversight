using Farol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using Xunit;

namespace Farol.Core.Tests;

[CollectionDefinition("OtlpEnvironment", DisableParallelization = true)]
public sealed class OtlpEnvironmentCollection { }

public class OtlpEndpointConfiguredTests
{
    [Fact]
    public void Reports_unconfigured_when_no_endpoint_variable_is_set() =>
        FarolStartupDiagnostics.OtlpEndpointConfigured(static _ => null).ShouldBeFalse();

    [Fact]
    public void Reports_configured_when_the_generic_endpoint_is_set() =>
        FarolStartupDiagnostics.OtlpEndpointConfigured(
            static name => name == "OTEL_EXPORTER_OTLP_ENDPOINT" ? "http://collector:4317" : null)
        .ShouldBeTrue();

    [Fact]
    public void Reports_configured_when_only_a_signal_specific_endpoint_is_set() =>
        FarolStartupDiagnostics.OtlpEndpointConfigured(
            static name => name == "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT" ? "http://collector:4318" : null)
        .ShouldBeTrue();
}

[Collection("OtlpEnvironment")]
public class FarolStartupDiagnosticsTests
{
    [Fact]
    public async Task Warns_when_no_otlp_endpoint_is_configured()
    {
        using var scope = new EnvironmentVariableScope(
            ("OTEL_EXPORTER_OTLP_ENDPOINT", null),
            ("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", null),
            ("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", null));
        var logger = new FakeLogger<FarolStartupDiagnostics>();
        var diagnostics = new FarolStartupDiagnostics(logger);

        await diagnostics.StartAsync(CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldContain(r =>
            r.Level == LogLevel.Warning && r.Message.Contains("telemetry has no destination"));
    }

    [Fact]
    public async Task Stays_silent_when_an_otlp_endpoint_is_configured()
    {
        using var scope = new EnvironmentVariableScope(
            ("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317"));
        var logger = new FakeLogger<FarolStartupDiagnostics>();
        var diagnostics = new FarolStartupDiagnostics(logger);

        await diagnostics.StartAsync(CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldBeEmpty();
    }
}
