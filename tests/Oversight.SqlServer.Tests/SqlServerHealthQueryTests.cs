using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class SqlServerHealthQueryTests
{
    [Fact]
    public void Command_prepends_the_lock_timeout_guard()
    {
        using var connection = new SqlConnection();
        using var command = SqlServerHealthQuery.CreateCommand(connection, "SELECT 1;");
        command.CommandText.ShouldStartWith("SET LOCK_TIMEOUT 5000;");
    }

    [Fact]
    public void Command_timeout_is_thirty_seconds()
    {
        using var connection = new SqlConnection();
        using var command = SqlServerHealthQuery.CreateCommand(connection, "SELECT 1;");
        command.CommandTimeout.ShouldBe(30);
    }

    [Fact]
    public void Original_sql_is_preserved_after_the_guard()
    {
        using var connection = new SqlConnection();
        using var command = SqlServerHealthQuery.CreateCommand(connection, "SELECT wait_type FROM sys.dm_os_wait_stats;");
        command.CommandText.ShouldEndWith("SELECT wait_type FROM sys.dm_os_wait_stats;");
    }
}
