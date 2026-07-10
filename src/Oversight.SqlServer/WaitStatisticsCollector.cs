using System.Globalization;
using Microsoft.Data.SqlClient;

namespace Oversight;

internal sealed class WaitStatisticsCollector : ISqlHealthCollector
{
    internal const double DominantShareThreshold = 40;

    public string Name => "wait_statistics";

    private string? _waitStatsView;

    private static readonly Dictionary<string, string> Causes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PAGEIOLATCH_SH"] = "Reads waiting on data-file I/O — check missing indexes and storage throughput.",
        ["PAGEIOLATCH_EX"] = "Writes waiting on data-file I/O — storage throughput pressure.",
        ["LCK_M_X"] = "Exclusive lock waits — long or contending write transactions.",
        ["LCK_M_S"] = "Shared lock waits — readers blocked by writers; consider snapshot isolation.",
        ["WRITELOG"] = "Log flush waits — log I/O pressure or many small transactions.",
        ["RESOURCE_SEMAPHORE"] = "Memory-grant waits — oversized sorts/hashes or memory pressure.",
        ["SOS_SCHEDULER_YIELD"] = "CPU pressure — sustained high CPU.",
        ["CXPACKET"] = "Parallelism waits — review MAXDOP and cost threshold for parallelism.",
        ["THREADPOOL"] = "Worker-thread starvation — too many concurrent blocked requests.",
        ["PAGELATCH_EX"] = "Hot-page contention — last-page insert hotspot or tempdb contention.",
    };

    // Idle, background and housekeeping waits accumulate whether or not the database is
    // under load; leaving them in makes them the top "problem" on any healthy server.
    private const string Sql = """
        SELECT TOP 10 wait_type, wait_time_ms, waiting_tasks_count
        FROM {0}
        WHERE waiting_tasks_count > 0
            AND wait_time_ms >= 1000
            AND wait_type NOT IN (
                'BROKER_TASK_STOP','BROKER_TO_FLUSH','CHECKPOINT_QUEUE','CLR_AUTO_EVENT','CLR_MANUAL_EVENT',
                'CLR_SEMAPHORE','DIRTY_PAGE_POLL','DISPATCHER_QUEUE_SEMAPHORE','FT_IFTS_SCHEDULER_IDLE_WAIT',
                'HADR_CLUSAPI_CALL','HADR_FILESTREAM_IOMGR_IOCOMPLETION','HADR_LOGCAPTURE_WAIT',
                'HADR_NOTIFICATION_DEQUEUE','HADR_TIMER_TASK','HADR_WORK_QUEUE','LAZYWRITER_SLEEP','LOGMGR_QUEUE',
                'ONDEMAND_TASK_QUEUE','PARALLEL_REDO_DRAIN_WORKER','PARALLEL_REDO_LOG_CACHE','PARALLEL_REDO_TRAN_LIST',
                'PARALLEL_REDO_WORKER_SYNC','PARALLEL_REDO_WORKER_WAIT','PREEMPTIVE_XE_GETTARGETSTATE',
                'PVS_PREALLOCATE','PWAIT_ALL_COMPONENTS_INITIALIZED','QDS_ASYNC_QUEUE',
                'QDS_CLEANUP_STALE_QUERIES_TASK_MAIN_LOOP_SLEEP','QDS_PERSIST_TASK_MAIN_LOOP_SLEEP','QDS_SHUTDOWN_QUEUE',
                'REQUEST_FOR_DEADLOCK_SEARCH','RESOURCE_GOVERNOR_IDLE','SLEEP_DBSTARTUP','SLEEP_MASTERDBREADY',
                'SLEEP_MASTERMDREADY','SLEEP_MASTERUPGRADED','SLEEP_MSDBSTARTUP','SLEEP_SYSTEMTASK','SLEEP_TASK',
                'SLEEP_TEMPDBSTARTUP','SOS_WORK_DISPATCHER','SP_SERVER_DIAGNOSTICS_SLEEP','SQLTRACE_BUFFER_FLUSH',
                'SQLTRACE_INCREMENTAL_FLUSH_SLEEP','STARTUP_DEPENDENCY_MANAGER','VDI_CLIENT_OTHER','WAITFOR',
                'XE_DISPATCHER_JOIN','XE_DISPATCHER_WAIT','XE_TIMER_EVENT')
        ORDER BY wait_time_ms DESC;
        """;

    public async Task<IReadOnlyList<SqlHealthFinding>> CollectAsync(
        SqlConnection connection, HealthSnapshotCache cache, CancellationToken cancellationToken)
    {
        var view = await ResolveWaitStatsViewAsync(connection, cancellationToken);
        var rows = await SqlServerHealthQuery.RunAsync(
            connection,
            string.Format(CultureInfo.InvariantCulture, Sql, view),
            static r => new WaitReading(r.ReadString("wait_type"), r.ReadInt64("wait_time_ms")),
            cancellationToken);

        cache.SetWaitStatistics(rows
            .Select(static row => new WaitTypeWait(row.WaitType, row.WaitTimeMilliseconds))
            .ToList());

        var dominant = DominantWaitFinding(rows);
        return dominant is null ? [] : [dominant];
    }

    private async Task<string> ResolveWaitStatsViewAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (_waitStatsView is not null)
            return _waitStatsView;

        await using var command = SqlServerHealthQuery.CreateCommand(
            connection, "SELECT CAST(SERVERPROPERTY('EngineEdition') AS int);");
        var edition = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
        _waitStatsView = WaitStatsViewFor(edition);
        return _waitStatsView;
    }

    // EngineEdition 5 = Azure SQL Database, the only edition without sys.dm_os_wait_stats.
    internal static string WaitStatsViewFor(int engineEdition) =>
        engineEdition == 5 ? "sys.dm_db_wait_stats" : "sys.dm_os_wait_stats";

    internal static SqlHealthFinding? DominantWaitFinding(IReadOnlyList<WaitReading> rows)
    {
        if (rows.Count == 0)
            return null;

        var total = rows.Sum(static row => row.WaitTimeMilliseconds);
        if (total == 0)
            return null;

        var top = rows[0];
        var share = (double)top.WaitTimeMilliseconds / total * 100;
        if (share < DominantShareThreshold)
            return null;

        var cause = Causes.TryGetValue(top.WaitType, out var known)
            ? known
            : "Uncategorized wait dominating tracked wait time — investigate before acting.";
        return new SqlHealthFinding(
            "wait_statistics",
            SqlHealthSeverity.Medium,
            $"Dominant wait {top.WaitType} ({share:0}% of tracked wait time). {cause}",
            SuggestedScript: null);
    }

    internal sealed record WaitReading(string WaitType, long WaitTimeMilliseconds);
}
