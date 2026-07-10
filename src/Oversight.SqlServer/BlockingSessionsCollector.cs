using Microsoft.Data.SqlClient;

namespace Oversight;

internal sealed class BlockingSessionsCollector(int minWaitMilliseconds = 5000) : ISqlHealthCollector
{
    public string Name => "blocking_sessions";

    // The default 5 s floor ignores transient lock waits caught mid-scan.
    private const string Sql = """
        SELECT r.session_id,
            r.blocking_session_id,
            ISNULL(r.wait_type, '') AS wait_type,
            r.wait_time
        FROM sys.dm_exec_requests AS r
        WHERE r.blocking_session_id <> 0
            AND r.wait_time >= @min_wait_ms
        ORDER BY r.wait_time DESC;
        """;

    public async Task<IReadOnlyList<SqlHealthFinding>> CollectAsync(
        SqlConnection connection, HealthSnapshotCache cache, CancellationToken cancellationToken)
    {
        var rows = await SqlServerHealthQuery.RunAsync(
            connection,
            Sql,
            static r => new BlockedSession(
                r.ReadInt32("session_id"),
                r.ReadInt32("blocking_session_id"),
                r.ReadString("wait_type"),
                r.ReadInt64("wait_time")),
            cancellationToken,
            ("@min_wait_ms", minWaitMilliseconds));

        cache.SetBlockingSessions(rows.Count);

        return rows.Select(row => new SqlHealthFinding(
            Name,
            SeverityForWait(row.WaitMilliseconds),
            $"Session {row.SessionId} blocked by session {row.BlockingSessionId} for {row.WaitMilliseconds / 1000.0:0.#}s ({row.WaitType})",
            SuggestedScript: null)).ToList();
    }

    internal static SqlHealthSeverity SeverityForWait(long waitMilliseconds) => waitMilliseconds switch
    {
        >= 60000 => SqlHealthSeverity.High,
        >= 20000 => SqlHealthSeverity.Medium,
        _ => SqlHealthSeverity.Low,
    };

    private sealed record BlockedSession(int SessionId, int BlockingSessionId, string WaitType, long WaitMilliseconds);
}
