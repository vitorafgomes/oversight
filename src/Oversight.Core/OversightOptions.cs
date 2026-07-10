namespace Oversight;

/// <summary>
/// Oversight-specific configuration, bound from the "Oversight" appsettings section and/or
/// the AddOversight* lambda. Standard OTEL_* environment variables are never duplicated here.
/// </summary>
public sealed class OversightOptions
{
    public const string SectionName = "Oversight";

    public PrometheusOptions Prometheus { get; } = new();

    public NoiseReductionOptions NoiseReduction { get; } = new();

    public EntityFrameworkCoreOptions EntityFrameworkCore { get; } = new();

    public SqlServerOptions SqlServer { get; } = new();

    public sealed class PrometheusOptions
    {
        /// <summary>Exposes a Prometheus scrape endpoint at /metrics. Default: false.</summary>
        public bool Enabled { get; set; }
    }

    public sealed class NoiseReductionOptions
    {
        /// <summary>
        /// Path globs excluded from server traces. Values bound from configuration are
        /// appended to these defaults; call Clear() in the lambda to replace them.
        /// </summary>
        public IList<string> ExcludedPaths { get; } =
            new List<string> { "/health", "/healthz", "/alive", "/ready", "/metrics" };
    }

    public sealed class EntityFrameworkCoreOptions
    {
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// When false (default) Oversight strips db.query.text/db.statement from database spans.
        /// </summary>
        public bool CaptureQueryText { get; set; }
    }

    public sealed class SqlServerOptions
    {
        /// <summary>
        /// Enables SQL Server health collection (Oversight.SqlServer package). Default: false —
        /// opt-in because it requires a connection string.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>Name of the ConnectionStrings entry for the monitored database.</summary>
        public string ConnectionStringName { get; set; } = string.Empty;

        /// <summary>Interval between collection cycles. Default: 15 minutes; minimum: 1 minute.</summary>
        public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromMinutes(15);

        public SqlServerCollectorOptions Collectors { get; } = new();
    }

    public sealed class SqlServerCollectorOptions
    {
        public bool BlockingSessions { get; set; } = true;

        public bool LongRunningTransactions { get; set; } = true;

        public bool MissingIndexes { get; set; } = true;

        public bool StaleStatistics { get; set; } = true;

        public bool WaitStatistics { get; set; } = true;
    }
}
