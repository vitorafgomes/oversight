namespace Farol;

/// <summary>
/// Farol-specific configuration, bound from the "Farol" appsettings section and/or
/// the AddFarol* lambda. Standard OTEL_* environment variables are never duplicated here.
/// </summary>
public sealed class FarolOptions
{
    public const string SectionName = "Farol";

    public PrometheusOptions Prometheus { get; } = new();

    public NoiseReductionOptions NoiseReduction { get; } = new();

    public EntityFrameworkCoreOptions EntityFrameworkCore { get; } = new();

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
        /// When false (default) Farol strips db.query.text/db.statement from database spans.
        /// </summary>
        public bool CaptureQueryText { get; set; }
    }
}
