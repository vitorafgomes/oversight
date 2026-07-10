using Microsoft.Extensions.Logging;

namespace Oversight;

internal static partial class SqlServerHealthLog
{
    internal static void EmitFinding(ILogger logger, SqlHealthFinding finding) =>
        Finding(
            logger,
            finding.Severity == SqlHealthSeverity.High ? LogLevel.Warning : LogLevel.Information,
            finding.Collector,
            finding.Severity,
            finding.Title,
            finding.SuggestedScript);

    [LoggerMessage(EventId = 5301, Message = "Oversight.SqlServer finding [{Collector}] severity={Severity}: {Title}. Suggested script (never executed by Oversight): {SuggestedScript}")]
    private static partial void Finding(ILogger logger, LogLevel level, string collector, SqlHealthSeverity severity, string title, string? suggestedScript);

    [LoggerMessage(EventId = 5302, Level = LogLevel.Warning, Message = "Oversight.SqlServer collector {Collector} failed; keeping the last cached reading. {Reason}")]
    internal static partial void CollectorFailed(ILogger logger, string collector, string reason, Exception exception);

    [LoggerMessage(EventId = 5303, Level = LogLevel.Warning, Message = "Oversight.SqlServer collection cycle failed; keeping all last cached readings. {Reason}")]
    internal static partial void CollectionCycleFailed(ILogger logger, string reason, Exception exception);
}
