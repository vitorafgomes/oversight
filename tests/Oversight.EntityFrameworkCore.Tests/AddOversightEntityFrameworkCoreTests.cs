using System.Diagnostics;
using Oversight;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.SqlClient;
using Shouldly;
using Xunit;

namespace Oversight.EntityFrameworkCore.Tests;

public class AddOversightEntityFrameworkCoreTests
{
    [Fact]
    public void Registers_instrumentation_by_default()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightEntityFrameworkCore();

        builder.Services.ShouldContain(d => d.ServiceType == typeof(OversightEntityFrameworkCoreMarker));
    }

    [Fact]
    public void Skips_registration_when_disabled_in_configuration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Oversight:EntityFrameworkCore:Enabled"] = "false",
        });
        builder.AddOversightEntityFrameworkCore();

        builder.Services.ShouldNotContain(d => d.ServiceType == typeof(OversightEntityFrameworkCoreMarker));
    }

    [Fact]
    public void Installs_query_text_scrubbing_enrich_callbacks_by_default()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightEntityFrameworkCore();
        using var host = builder.Build();

        host.Services.GetRequiredService<IOptions<EntityFrameworkInstrumentationOptions>>()
            .Value.EnrichWithIDbCommand.ShouldNotBeNull();
        host.Services.GetRequiredService<IOptions<SqlClientTraceInstrumentationOptions>>()
            .Value.EnrichWithSqlCommand.ShouldNotBeNull();
    }

    [Fact]
    public void Does_not_scrub_when_query_text_capture_is_opted_in()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightEntityFrameworkCore(oversight => oversight.EntityFrameworkCore.CaptureQueryText = true);
        using var host = builder.Build();

        host.Services.GetRequiredService<IOptions<EntityFrameworkInstrumentationOptions>>()
            .Value.EnrichWithIDbCommand.ShouldBeNull();
        host.Services.GetRequiredService<IOptions<SqlClientTraceInstrumentationOptions>>()
            .Value.EnrichWithSqlCommand.ShouldBeNull();
    }

    [Fact]
    public void Scrubber_removes_query_text_tags_from_an_activity()
    {
        using var activity = new Activity("ef-command");
        activity.SetTag("db.query.text", "SELECT * FROM Users");
        activity.SetTag("db.statement", "SELECT * FROM Users");

        OversightDbQueryTextScrubber.Scrub(activity);

        activity.GetTagItem("db.query.text").ShouldBeNull();
        activity.GetTagItem("db.statement").ShouldBeNull();
    }

    [Fact]
    public void Calling_add_oversight_entity_framework_core_twice_registers_once()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightEntityFrameworkCore();
        builder.AddOversightEntityFrameworkCore();

        builder.Services.Count(d => d.ServiceType == typeof(OversightEntityFrameworkCoreMarker)).ShouldBe(1);
    }

    [Fact]
    public void Composes_user_enrich_with_id_b_command_before_scrubbing()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOptions<EntityFrameworkInstrumentationOptions>()
            .Configure(o => o.EnrichWithIDbCommand = (activity, _) => activity.SetTag("user.marker", "ran"));
        builder.AddOversightEntityFrameworkCore();
        using var host = builder.Build();

        var enrich = host.Services
            .GetRequiredService<IOptions<EntityFrameworkInstrumentationOptions>>().Value.EnrichWithIDbCommand;
        enrich.ShouldNotBeNull();

        using var activity = new Activity("ef-command");
        activity.SetTag("db.query.text", "SELECT * FROM Users");
        activity.SetTag("db.statement", "SELECT * FROM Users");
        enrich!(activity, null!);

        activity.GetTagItem("user.marker").ShouldBe("ran");
        activity.GetTagItem("db.query.text").ShouldBeNull();
        activity.GetTagItem("db.statement").ShouldBeNull();
    }

    [Fact]
    public void Composes_user_enrich_with_sql_command_before_scrubbing()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOptions<SqlClientTraceInstrumentationOptions>()
            .Configure(o => o.EnrichWithSqlCommand = (activity, _) => activity.SetTag("user.marker", "ran"));
        builder.AddOversightEntityFrameworkCore();
        using var host = builder.Build();

        var enrich = host.Services
            .GetRequiredService<IOptions<SqlClientTraceInstrumentationOptions>>().Value.EnrichWithSqlCommand;
        enrich.ShouldNotBeNull();

        using var activity = new Activity("sql-command");
        activity.SetTag("db.query.text", "SELECT * FROM Users");
        activity.SetTag("db.statement", "SELECT * FROM Users");
        enrich!(activity, null!);

        activity.GetTagItem("user.marker").ShouldBe("ran");
        activity.GetTagItem("db.query.text").ShouldBeNull();
        activity.GetTagItem("db.statement").ShouldBeNull();
    }

    [Fact]
    public void Second_call_registers_the_marker_when_the_first_call_was_disabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightEntityFrameworkCore(o => o.EntityFrameworkCore.Enabled = false);
        builder.AddOversightEntityFrameworkCore();

        builder.Services.ShouldContain(d => d.ServiceType == typeof(OversightEntityFrameworkCoreMarker));
    }
}
