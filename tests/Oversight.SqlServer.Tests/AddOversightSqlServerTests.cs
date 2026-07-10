using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class AddOversightSqlServerTests
{
    private static readonly Dictionary<string, string?> EnabledSettings = new()
    {
        ["Oversight:SqlServer:Enabled"] = "true",
        ["Oversight:SqlServer:ConnectionStringName"] = "AppDb",
        ["ConnectionStrings:AppDb"] = "Server=localhost;Database=app;TrustServerCertificate=true",
    };

    [Fact]
    public void Disabled_by_default_registers_nothing()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightSqlServer();

        builder.Services.ShouldNotContain(d => d.ServiceType == typeof(OversightSqlServerMarker));
        builder.Services.ShouldNotContain(d => d.ServiceType == typeof(ISqlHealthCollector));
    }

    [Fact]
    public void Enabled_without_a_known_connection_string_fails_at_registration()
    {
        var builder = Host.CreateApplicationBuilder();

        var exception = Should.Throw<InvalidOperationException>(() =>
            builder.AddOversightSqlServer(static oversight =>
            {
                oversight.SqlServer.Enabled = true;
                oversight.SqlServer.ConnectionStringName = "Missing";
            }));

        exception.Message.ShouldContain("ConnectionStrings:Missing");
    }

    [Fact]
    public void Enabled_registers_cache_metrics_and_all_five_collectors()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(EnabledSettings);
        builder.AddOversightSqlServer();
        using var host = builder.Build();

        host.Services.GetRequiredService<HealthSnapshotCache>().ShouldNotBeNull();
        host.Services.GetRequiredService<SqlServerHealthMetrics>().ShouldNotBeNull();
        host.Services.GetServices<ISqlHealthCollector>().Count().ShouldBe(5);
    }

    [Fact]
    public void Enabled_registers_the_background_collector_service()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(EnabledSettings);
        builder.AddOversightSqlServer();
        using var host = builder.Build();

        host.Services.GetServices<IHostedService>().OfType<SqlHealthCollectorService>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Second_call_is_idempotent()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(EnabledSettings);
        builder.AddOversightSqlServer();
        builder.AddOversightSqlServer();
        using var host = builder.Build();

        builder.Services.Count(d => d.ServiceType == typeof(OversightSqlServerMarker)).ShouldBe(1);
        host.Services.GetServices<ISqlHealthCollector>().Count().ShouldBe(5);
    }

    [Fact]
    public void Collector_toggles_skip_disabled_collectors()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(EnabledSettings);
        builder.AddOversightSqlServer(static oversight =>
        {
            oversight.SqlServer.Collectors.MissingIndexes = false;
            oversight.SqlServer.Collectors.WaitStatistics = false;
        });
        using var host = builder.Build();

        var collectors = host.Services.GetServices<ISqlHealthCollector>().Select(c => c.Name).ToList();
        collectors.Count.ShouldBe(3);
        collectors.ShouldNotContain("missing_indexes");
        collectors.ShouldNotContain("wait_statistics");
    }
}
