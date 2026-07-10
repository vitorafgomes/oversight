using Microsoft.Data.SqlClient;

namespace Oversight;

/// <summary>
/// Runs a single read-only collector query. SET LOCK_TIMEOUT plus a short command timeout
/// guarantee a health scan can never pile onto an already-blocked server.
/// </summary>
internal static class SqlServerHealthQuery
{
    internal const int CommandTimeoutSeconds = 30;
    internal const int LockTimeoutMilliseconds = 5000;

    internal static SqlCommand CreateCommand(SqlConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SET LOCK_TIMEOUT {LockTimeoutMilliseconds};\n{sql}";
        command.CommandTimeout = CommandTimeoutSeconds;
        return command;
    }

    internal static async Task<List<T>> RunAsync<T>(
        SqlConnection connection,
        string sql,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = CreateCommand(connection, sql);
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);

        var results = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(map(reader));

        return results;
    }
}
