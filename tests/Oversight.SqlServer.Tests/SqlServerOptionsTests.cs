using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Oversight.SqlServer.Tests;

public class SqlServerOptionsTests
{
    [Fact]
    public void Sql_server_collection_is_disabled_by_default() =>
        new OversightOptions().SqlServer.Enabled.ShouldBeFalse();

    [Fact]
    public void Collection_interval_defaults_to_fifteen_minutes() =>
        new OversightOptions().SqlServer.CollectionInterval.ShouldBe(TimeSpan.FromMinutes(15));

    [Fact]
    public void Connection_string_name_defaults_to_empty() =>
        new OversightOptions().SqlServer.ConnectionStringName.ShouldBeEmpty();

    [Fact]
    public void Every_collector_is_enabled_by_default()
    {
        var collectors = new OversightOptions().SqlServer.Collectors;
        collectors.BlockingSessions.ShouldBeTrue();
        collectors.LongRunningTransactions.ShouldBeTrue();
        collectors.MissingIndexes.ShouldBeTrue();
        collectors.StaleStatistics.ShouldBeTrue();
        collectors.WaitStatistics.ShouldBeTrue();
    }

    [Fact]
    public void Binds_sql_server_options_from_the_oversight_section()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Oversight:SqlServer:Enabled"] = "true",
            ["Oversight:SqlServer:ConnectionStringName"] = "AppDb",
            ["Oversight:SqlServer:CollectionInterval"] = "00:05:00",
            ["Oversight:SqlServer:Collectors:MissingIndexes"] = "false",
        });
        builder.AddOversightCore();
        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<OversightOptions>>().Value;

        options.SqlServer.Enabled.ShouldBeTrue();
        options.SqlServer.ConnectionStringName.ShouldBe("AppDb");
        options.SqlServer.CollectionInterval.ShouldBe(TimeSpan.FromMinutes(5));
        options.SqlServer.Collectors.MissingIndexes.ShouldBeFalse();
        options.SqlServer.Collectors.BlockingSessions.ShouldBeTrue();
    }

    [Fact]
    public async Task Enabled_without_connection_string_name_fails_at_startup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightCore(static oversight => oversight.SqlServer.Enabled = true);
        using var host = builder.Build();

        await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task Collection_interval_below_one_minute_fails_at_startup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightCore(static oversight =>
        {
            oversight.SqlServer.Enabled = true;
            oversight.SqlServer.ConnectionStringName = "AppDb";
            oversight.SqlServer.CollectionInterval = TimeSpan.FromSeconds(30);
        });
        using var host = builder.Build();

        await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync());
    }
}
