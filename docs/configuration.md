# Configuration reference

## Precedence

1. Standard `OTEL_*` environment variables (highest — never overridden by Oversight).
2. The `AddOversight*` lambda.
3. The `"Oversight"` appsettings section.
4. Oversight defaults.

## OTel standard variables (handled by the SDK, not Oversight)

`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`,
`OTEL_EXPORTER_OTLP_HEADERS`, `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`,
`OTEL_TRACES_SAMPLER` (default `parentbased_always_on`).

## Oversight section

See the README table. Notes:

- `ExcludedPaths` entries must start with `/`; `*` matches any characters
  including `/`. Invalid entries fail at startup, never at request time.
- Configuration-bound `ExcludedPaths` values are appended to the defaults; call
  `ExcludedPaths.Clear()` in the lambda to replace them.
- `Prometheus:Enabled` and `EntityFrameworkCore:Enabled` are read when the
  `AddOversight*` method runs (they add/skip registrations); set them in
  appsettings or in the same call's lambda.
- Configure options once — pass the lambda to a single `AddOversight*` call.

## Security

`db.query.text` / `db.statement` are stripped from database spans unless
`Oversight:EntityFrameworkCore:CaptureQueryText` is `true`. Query parameters are
never captured.

## SQL Server monitoring (Oversight.SqlServer)

Opt-in package: install `Oversight.SqlServer` and call `AddOversightSqlServer()`.
It is not part of the `Oversight` meta-package because it needs a connection string.

    builder.AddOversightSqlServer(oversight =>
    {
        oversight.SqlServer.Enabled = true;
        oversight.SqlServer.ConnectionStringName = "AppDb";
    });

A background service collects every `CollectionInterval` (default 15 minutes) into an
in-memory snapshot; the gauges below read only that snapshot and emit nothing until the
first successful collection.

| Metric | Unit | Meaning |
|---|---|---|
| `db.sqlserver.blocking.sessions` | `{session}` | Sessions blocked >= 5 s at the last collection |
| `db.sqlserver.transactions.long_running` | `{transaction}` | Transactions open >= 60 s at the last collection |
| `db.sqlserver.missing_indexes.count` | `{index}` | Missing-index suggestions with estimated impact >= 50% |
| `db.sqlserver.missing_indexes.max_impact` | `%` | Highest optimizer-estimated improvement among suggestions |
| `db.sqlserver.statistics.stale.count` | `{statistic}` | Statistics with modifications >= 20% of rows (tables >= 1000 rows) |
| `db.sqlserver.waits.time` | `ms` | Cumulative wait time per `db.sqlserver.wait_type` (top 10, benign waits filtered) |

Findings are structured `ILogger` records (event id 5301) with severity and a suggested
script (for example a generated `CREATE INDEX`) that Oversight **never executes**. High
severity logs as Warning, Medium/Low as Information.

Safety: every collector query is read-only, prefixed with `SET LOCK_TIMEOUT 5000` and
capped at a 30 s command timeout. A failing collector logs a warning (event id 5302) and
keeps the last cached reading; a failed cycle logs one warning (5303) — the host app never
breaks. Only misconfiguration fails fast: enabling without a resolvable connection string
name throws at startup. The monitoring login needs `VIEW SERVER STATE` (or
`VIEW DATABASE STATE` on Azure SQL Database).
