using Microsoft.Data.SqlClient;

namespace Oversight;

/// <summary>
/// A single read-only health probe over DMVs. Implementations write their reading to the
/// cache on success and return findings; they never write to the target database. One
/// collector failing never fails the cycle (the service isolates each call).
/// </summary>
internal interface ISqlHealthCollector
{
    string Name { get; }

    Task<IReadOnlyList<SqlHealthFinding>> CollectAsync(
        SqlConnection connection,
        HealthSnapshotCache cache,
        CancellationToken cancellationToken);
}
