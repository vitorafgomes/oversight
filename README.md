# Farol

Opinionated OpenTelemetry defaults for .NET 10 in one call. Like Aspire
ServiceDefaults, but standalone, centrally upgradable via NuGet, and working in
any host.

## Quick start

```bash
dotnet add package Farol
```

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddFarol();
```

That is production-grade traces and metrics: OTLP export, HTTP server/client,
EF Core and SqlClient, runtime and process metrics, resource identity, and
health-check noise excluded from traces. Point it at a collector with the
standard variable:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://my-collector:4317
```

No endpoint configured? Farol defaults to `localhost:4317` and logs a startup
warning — it never fails, and an unreachable collector never breaks your app.

## Packages

| Package | Use when |
|---|---|
| `Farol` | ASP.NET Core apps (aggregates the three below) — `AddFarol()` |
| `Farol.Core` | Worker Services / console hosts — `AddFarolCore()` |
| `Farol.AspNetCore` | Granular: HTTP server traces/metrics, noise filter, Prometheus — `AddFarolAspNetCore()` |
| `Farol.EntityFrameworkCore` | Granular: EF Core + SqlClient traces — `AddFarolEntityFrameworkCore()` |

## Configuration

Standard `OTEL_*` environment variables (endpoint, protocol, headers, sampler,
resource attributes) always win — Farol never re-invents them. The `"Farol"`
appsettings section (or the lambda) controls only Farol-specific behavior:

| Key | Default | Meaning |
|---|---|---|
| `Farol:Prometheus:Enabled` | `false` | Serve Prometheus text at `/metrics` |
| `Farol:NoiseReduction:ExcludedPaths` | `/health /healthz /alive /ready /metrics` | Path globs excluded from server traces (config values append to defaults) |
| `Farol:EntityFrameworkCore:Enabled` | `true` | Register EF Core + SqlClient instrumentation |
| `Farol:EntityFrameworkCore:CaptureQueryText` | `false` | Opt-in: keep `db.query.text` on database spans |

```csharp
builder.AddFarol(farol =>
{
    farol.Prometheus.Enabled = true;
    farol.NoiseReduction.ExcludedPaths.Add("/internal/*");
});
```

Identity fallbacks when env vars are absent: `service.name` = entry assembly
name, `service.version` = `AssemblyInformationalVersion`,
`deployment.environment` = host environment name.

More: [docs/configuration.md](docs/configuration.md) ·
[docs/philosophy.md](docs/philosophy.md)

## Building

```bash
dotnet build
dotnet test
```

## License

MIT
