using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Oversight;

internal sealed class OversightStartupDiagnostics(ILogger<OversightStartupDiagnostics> logger) : IHostedService
{
    private static readonly string[] EndpointVariables =
    [
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT",
        "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT",
    ];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!OtlpEndpointConfigured(Environment.GetEnvironmentVariable))
        {
            logger.LogWarning(
                "Oversight: telemetry has no destination. No OTLP endpoint is configured "
                + "(OTEL_EXPORTER_OTLP_ENDPOINT or a signal-specific variant), so the SDK "
                + "falls back to localhost:4317. Point OTEL_EXPORTER_OTLP_ENDPOINT at your collector.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal static bool OtlpEndpointConfigured(Func<string, string?> getEnvironmentVariable) =>
        EndpointVariables.Any(name => !string.IsNullOrWhiteSpace(getEnvironmentVariable(name)));
}
