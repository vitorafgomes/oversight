using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace Oversight.SqlServer.Tests;

public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public bool IsAvailable { get; private set; }

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new MsSqlBuilder().Build();
            await _container.StartAsync();
            await using (var connection = new SqlConnection(_container.GetConnectionString()))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "CREATE DATABASE oversight_tests;";
                await command.ExecuteNonQueryAsync();
            }
            ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = "oversight_tests",
            }.ConnectionString;
            IsAvailable = true;
        }
        catch (Exception)
        {
            // Docker absent or image pull failed — dependent tests skip, never fail.
            IsAvailable = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}

[CollectionDefinition("sqlserver-container")]
public sealed class SqlServerContainerCollection : ICollectionFixture<SqlServerContainerFixture>;
