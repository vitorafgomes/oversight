using Microsoft.Data.SqlClient;

namespace Oversight;

internal sealed class LongRunningTransactionsCollector(int minDurationSeconds = 60) : ISqlHealthCollector
{
    public string Name => "long_running_transactions";

    // transaction_begin_time is server-local time, so the comparison uses GETDATE().
    private const string Sql = """
        SELECT st.session_id,
            at.transaction_id,
            ISNULL(at.name, '') AS transaction_name,
            DATEDIFF(SECOND, at.transaction_begin_time, GETDATE()) AS duration_seconds
        FROM sys.dm_tran_active_transactions AS at
        JOIN sys.dm_tran_session_transactions AS st ON st.transaction_id = at.transaction_id
        WHERE DATEDIFF(SECOND, at.transaction_begin_time, GETDATE()) >= @min_duration_seconds
        ORDER BY duration_seconds DESC;
        """;

    public async Task<IReadOnlyList<SqlHealthFinding>> CollectAsync(
        SqlConnection connection, HealthSnapshotCache cache, CancellationToken cancellationToken)
    {
        var rows = await SqlServerHealthQuery.RunAsync(
            connection,
            Sql,
            static r => new OpenTransaction(
                r.ReadInt32("session_id"),
                r.ReadInt64("transaction_id"),
                r.ReadString("transaction_name"),
                r.ReadInt64("duration_seconds")),
            cancellationToken,
            ("@min_duration_seconds", minDurationSeconds));

        cache.SetLongRunningTransactions(rows.Count);

        return rows.Select(row => new SqlHealthFinding(
            Name,
            SeverityForDuration(row.DurationSeconds),
            $"Transaction {row.TransactionId} open for {row.DurationSeconds}s on session {row.SessionId}",
            SuggestedScript: null)).ToList();
    }

    internal static SqlHealthSeverity SeverityForDuration(long durationSeconds) => durationSeconds switch
    {
        >= 600 => SqlHealthSeverity.High,
        >= 180 => SqlHealthSeverity.Medium,
        _ => SqlHealthSeverity.Low,
    };

    private sealed record OpenTransaction(int SessionId, long TransactionId, string Name, long DurationSeconds);
}
