using Microsoft.Data.SqlClient;

namespace Oversight;

internal sealed class MissingIndexesCollector(double minAverageImpact = 50) : ISqlHealthCollector
{
    public string Name => "missing_indexes";

    // The 50% default keeps only suggestions the optimizer itself rates as high-impact.
    private const string Sql = """
        SELECT TOP 20 d.statement AS table_name,
            gs.avg_user_impact,
            gs.user_seeks + gs.user_scans AS uses,
            ISNULL(d.equality_columns, '') AS equality_columns,
            ISNULL(d.inequality_columns, '') AS inequality_columns,
            ISNULL(d.included_columns, '') AS included_columns
        FROM sys.dm_db_missing_index_group_stats AS gs
        JOIN sys.dm_db_missing_index_groups AS g ON g.index_group_handle = gs.group_handle
        JOIN sys.dm_db_missing_index_details AS d ON d.index_handle = g.index_handle
        WHERE d.database_id = DB_ID()
            AND gs.avg_user_impact >= @min_impact
            AND gs.user_seeks + gs.user_scans > 0
        ORDER BY gs.avg_user_impact DESC;
        """;

    public async Task<IReadOnlyList<SqlHealthFinding>> CollectAsync(
        SqlConnection connection, HealthSnapshotCache cache, CancellationToken cancellationToken)
    {
        var rows = await SqlServerHealthQuery.RunAsync(
            connection,
            Sql,
            static r => new MissingIndex(
                r.ReadString("table_name"),
                r.ReadDouble("avg_user_impact"),
                r.ReadInt64("uses"),
                r.ReadString("equality_columns"),
                r.ReadString("inequality_columns"),
                r.ReadString("included_columns")),
            cancellationToken,
            ("@min_impact", minAverageImpact));

        cache.SetMissingIndexes(rows.Count, rows.Count == 0 ? 0 : rows.Max(static row => row.AverageImpact));

        return rows.Select(row => new SqlHealthFinding(
            Name,
            SeverityForImpact(row.AverageImpact),
            $"Missing index on {row.Table} (optimizer estimates {row.AverageImpact:0}% improvement, {row.Uses} qualifying executions)",
            BuildCreateIndex(row.Table, row.EqualityColumns, row.InequalityColumns, row.IncludedColumns))).ToList();
    }

    internal static SqlHealthSeverity SeverityForImpact(double averageImpact) => averageImpact switch
    {
        >= 90 => SqlHealthSeverity.High,
        >= 70 => SqlHealthSeverity.Medium,
        _ => SqlHealthSeverity.Low,
    };

    internal static string BuildCreateIndex(
        string table, string equalityColumns, string inequalityColumns, string includedColumns)
    {
        var keyColumns = string.Join(", ",
            new[] { equalityColumns, inequalityColumns }.Where(static part => !string.IsNullOrWhiteSpace(part)));
        // A name derived from table + key columns so applying two suggestions never collides.
        var tableName = table.Split('.')[^1].Trim('[', ']');
        var columnPart = string.Concat(keyColumns.Where(char.IsLetterOrDigit));
        var indexName = $"IX_Oversight_{tableName}_{columnPart}";
        if (indexName.Length > 120)
            indexName = indexName[..120];
        var include = string.IsNullOrWhiteSpace(includedColumns) ? string.Empty : $" INCLUDE ({includedColumns})";
        return $"CREATE INDEX {indexName} ON {table} ({keyColumns}){include};";
    }

    private sealed record MissingIndex(
        string Table,
        double AverageImpact,
        long Uses,
        string EqualityColumns,
        string InequalityColumns,
        string IncludedColumns);
}
