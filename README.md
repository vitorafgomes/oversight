# Oversight

Opinionated OpenTelemetry defaults for .NET 10 in one call. Like Aspire
ServiceDefaults, but standalone, centrally upgradable via NuGet, and working in
any host.

## Quick start

```bash
dotnet add package Oversight
```

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddOversight();
```

That is production-grade traces and metrics: OTLP export, HTTP server/client,
EF Core and SqlClient, runtime and process metrics, resource identity, and
health-check noise excluded from traces. Point it at a collector with the
standard variable:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://my-collector:4317
```

No endpoint configured? Oversight defaults to `localhost:4317` and logs a startup
warning — it never fails, and an unreachable collector never breaks your app.

## Packages

| Package | Use when |
|---|---|
| `Oversight` | ASP.NET Core apps (aggregates the three below) — `AddOversight()` |
| `Oversight.Core` | Worker Services / console hosts — `AddOversightCore()` |
| `Oversight.AspNetCore` | Granular: HTTP server traces/metrics, noise filter, Prometheus — `AddOversightAspNetCore()` |
| `Oversight.EntityFrameworkCore` | Granular: EF Core + SqlClient traces — `AddOversightEntityFrameworkCore()` |
| `Oversight.SqlServer` | Opt-in: SQL Server health monitoring (blocking, missing indexes, waits) — `AddOversightSqlServer()` |

## Configuration

Standard `OTEL_*` environment variables (endpoint, protocol, headers, sampler,
resource attributes) always win — Oversight never re-invents them. The `"Oversight"`
appsettings section (or the lambda) controls only Oversight-specific behavior:

| Key | Default | Meaning |
|---|---|---|
| `Oversight:Prometheus:Enabled` | `false` | Serve Prometheus text at `/metrics` |
| `Oversight:NoiseReduction:ExcludedPaths` | `/health /healthz /alive /ready /metrics` | Path globs excluded from server traces (config values append to defaults) |
| `Oversight:EntityFrameworkCore:Enabled` | `true` | Register EF Core + SqlClient instrumentation |
| `Oversight:EntityFrameworkCore:CaptureQueryText` | `false` | Opt-in: keep `db.query.text` on database spans |
| `Oversight:SqlServer:Enabled` | `false` | Opt-in: collect SQL Server health metrics and findings |
| `Oversight:SqlServer:ConnectionStringName` | — | `ConnectionStrings` entry of the monitored database (required when enabled) |
| `Oversight:SqlServer:CollectionInterval` | `00:15:00` | Time between collection cycles (minimum 1 minute) |
| `Oversight:SqlServer:Collectors:<Name>` | `true` | Per-collector toggles: `BlockingSessions`, `LongRunningTransactions`, `MissingIndexes`, `StaleStatistics`, `WaitStatistics` |

```csharp
builder.AddOversight(oversight =>
{
    oversight.Prometheus.Enabled = true;
    oversight.NoiseReduction.ExcludedPaths.Add("/internal/*");
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
