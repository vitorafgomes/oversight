using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Oversight;

internal sealed class SqlHealthCollectorService : BackgroundService
{
    private readonly string _connectionString;
    private readonly TimeSpan _interval;
    private readonly HealthSnapshotCache _cache;
    private readonly IReadOnlyList<ISqlHealthCollector> _collectors;
    private readonly ILogger<SqlHealthCollectorService> _logger;

    public SqlHealthCollectorService(
        IOptions<OversightOptions> options,
        IConfiguration configuration,
        HealthSnapshotCache cache,
        IEnumerable<ISqlHealthCollector> collectors,
        SqlServerHealthMetrics metrics,
        ILogger<SqlHealthCollectorService> logger)
    {
        // The metrics dependency exists so the gauges are constructed with the host,
        // before the first export cycle.
        _ = metrics;
        _connectionString = configuration.GetConnectionString(options.Value.SqlServer.ConnectionStringName) ?? string.Empty;
        _interval = options.Value.SqlServer.CollectionInterval;
        _cache = cache;
        _collectors = collectors.ToList();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield first so host startup is never delayed by the initial collection.
        await Task.Yield();
        try
        {
            await CollectOnceAsync(stoppingToken);
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await CollectOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Host shutdown.
        }
    }

    internal async Task CollectOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await RunCollectorsAsync(connection, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            SqlServerHealthLog.CollectionCycleFailed(_logger, exception.Message, exception);
        }
    }

    internal async Task RunCollectorsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        foreach (var collector in _collectors)
        {
            try
            {
                var findings = await collector.CollectAsync(connection, _cache, cancellationToken);
                foreach (var finding in findings)
                    SqlServerHealthLog.EmitFinding(_logger, finding);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                SqlServerHealthLog.CollectorFailed(_logger, collector.Name, exception.Message, exception);
            }
        }
    }
}
