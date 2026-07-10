# Oversight

Production-grade OpenTelemetry traces and metrics for ASP.NET Core in one call:

    var builder = WebApplication.CreateBuilder(args);
    builder.AddOversight();

Defaults: OTLP export (standard `OTEL_*` env vars, `localhost:4317` fallback),
HTTP server/client + EF Core + runtime/process telemetry, health-check noise
excluded from traces, SQL text capture off. Optional Prometheus `/metrics`
endpoint. Pure workers should install `Oversight.Core` instead.
Docs: https://github.com/vitorafgomes/oversight
