using Microsoft.Data.SqlClient;

namespace Oversight;

internal sealed class StaleStatisticsCollector(long minRows = 1000, double minModificationRatio = 0.2) : ISqlHealthCollector
{
    public string Name => "stale_statistics";

    // Tiny tables are excluded: stale statistics there do not hurt the optimizer.
    private const string Sql = """
        SELECT OBJECT_SCHEMA_NAME(s.object_id) AS schema_name,
            OBJECT_NAME(s.object_id) AS table_name,
            s.name AS statistic_name,
            sp.rows AS row_count,
            sp.modification_counter
        FROM sys.stats AS s
        JOIN sys.objects AS o ON o.object_id = s.object_id AND o.type = 'U'
        CROSS APPLY sys.dm_db_stats_properties(s.object_id, s.stats_id) AS sp
        WHERE sp.rows >= @min_rows
            AND sp.modification_counter >= sp.rows * @min_ratio
        ORDER BY sp.modification_counter DESC;
        """;

    public async Task<IReadOnlyList<SqlHealthFinding>> CollectAsync(
        SqlConnection connection, HealthSnapshotCache cache, CancellationToken cancellationToken)
    {
        var rows = await SqlServerHealthQuery.RunAsync(
            connection,
            Sql,
            static r => new StaleStatistic(
                r.ReadString("schema_name"),
                r.ReadString("table_name"),
                r.ReadString("statistic_name"),
                r.ReadInt64("row_count"),
                r.ReadInt64("modification_counter")),
            cancellationToken,
            ("@min_rows", minRows),
            ("@min_ratio", minModificationRatio));

        cache.SetStaleStatistics(rows.Count);

        return rows.Select(static row => new SqlHealthFinding(
            "stale_statistics",
            SeverityForRatio(row.RowCount == 0 ? 0 : (double)row.ModificationCounter / row.RowCount),
            $"Statistics {row.Statistic} on {row.Schema}.{row.Table} have {row.ModificationCounter:N0} modifications over {row.RowCount:N0} rows",
            BuildUpdateStatistics(row.Schema, row.Table, row.Statistic))).ToList();
    }

    internal static SqlHealthSeverity SeverityForRatio(double ratio) =>
        ratio >= 0.5 ? SqlHealthSeverity.Medium : SqlHealthSeverity.Low;

    internal static string BuildUpdateStatistics(string schema, string table, string statistic) =>
        $"UPDATE STATISTICS [{schema}].[{table}] ([{statistic}]);";

    private sealed record StaleStatistic(
        string Schema, string Table, string Statistic, long RowCount, long ModificationCounter);
}
