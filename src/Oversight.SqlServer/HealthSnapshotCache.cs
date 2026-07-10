namespace Oversight;

internal sealed record WaitTypeWait(string WaitType, long WaitTimeMilliseconds);

/// <summary>
/// Last successful reading per collector. Null means "never collected" and the matching
/// gauge emits no measurement. Written by the collector timer, read by the metrics
/// export thread, so every access is guarded.
/// </summary>
internal sealed class HealthSnapshotCache
{
    private readonly Lock _gate = new();
    private long? _blockingSessions;
    private long? _longRunningTransactions;
    private long? _missingIndexCount;
    private double? _missingIndexMaxImpact;
    private long? _staleStatisticsCount;
    private IReadOnlyList<WaitTypeWait>? _waitStatistics;

    internal long? BlockingSessions { get { lock (_gate) return _blockingSessions; } }

    internal long? LongRunningTransactions { get { lock (_gate) return _longRunningTransactions; } }

    internal long? MissingIndexCount { get { lock (_gate) return _missingIndexCount; } }

    internal double? MissingIndexMaxImpact { get { lock (_gate) return _missingIndexMaxImpact; } }

    internal long? StaleStatisticsCount { get { lock (_gate) return _staleStatisticsCount; } }

    internal IReadOnlyList<WaitTypeWait>? WaitStatistics { get { lock (_gate) return _waitStatistics; } }

    internal void SetBlockingSessions(long value) { lock (_gate) _blockingSessions = value; }

    internal void SetLongRunningTransactions(long value) { lock (_gate) _longRunningTransactions = value; }

    internal void SetMissingIndexes(long count, double maxImpact)
    {
        lock (_gate)
        {
            _missingIndexCount = count;
            _missingIndexMaxImpact = maxImpact;
        }
    }

    internal void SetStaleStatistics(long value) { lock (_gate) _staleStatisticsCount = value; }

    internal void SetWaitStatistics(IReadOnlyList<WaitTypeWait> waits) { lock (_gate) _waitStatistics = waits; }
}
