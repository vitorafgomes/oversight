# Oversight.SqlServer

SQL Server health monitoring for Oversight: a background service runs read-only
DMV collectors (blocking sessions, long-running transactions, missing indexes,
stale statistics, wait statistics) on a configurable interval and exposes the
results as `db.sqlserver.*` OpenTelemetry metrics plus findings-as-logs with
suggested scripts that are never executed.

    builder.AddOversightSqlServer(oversight =>
    {
        oversight.SqlServer.Enabled = true;
        oversight.SqlServer.ConnectionStringName = "AppDb";
    });

Opt-in by design: nothing runs until `Oversight:SqlServer:Enabled` is `true` and
a connection string name is configured. Pair with `Oversight.Core` (or the
`Oversight` meta-package) for OTLP export. Docs: https://github.com/vitorafgomes/oversight
