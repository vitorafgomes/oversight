using Oversight;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Oversight.Core.Tests;

public class OversightOptionsBindingTests
{
    [Fact]
    public void Binds_options_from_the_oversight_configuration_section()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Oversight:Prometheus:Enabled"] = "true",
            ["Oversight:EntityFrameworkCore:CaptureQueryText"] = "true",
            ["Oversight:NoiseReduction:ExcludedPaths:0"] = "/internal/*",
        });
        builder.AddOversightCore();
        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<OversightOptions>>().Value;

        options.Prometheus.Enabled.ShouldBeTrue();
        options.EntityFrameworkCore.CaptureQueryText.ShouldBeTrue();
        // Configuration values are appended to the built-in defaults, not replacing them.
        options.NoiseReduction.ExcludedPaths.ShouldContain("/internal/*");
        options.NoiseReduction.ExcludedPaths.ShouldContain("/health");
    }

    [Fact]
    public void Lambda_configuration_wins_over_the_configuration_section()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Oversight:Prometheus:Enabled"] = "false",
        });
        builder.AddOversightCore(oversight => oversight.Prometheus.Enabled = true);
        using var host = builder.Build();

        host.Services.GetRequiredService<IOptions<OversightOptions>>()
            .Value.Prometheus.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Invalid_excluded_path_fails_at_startup_not_at_request_time()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightCore(oversight => oversight.NoiseReduction.ExcludedPaths.Add("no-leading-slash"));
        using var host = builder.Build();

        await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public void Resolve_snapshot_merges_section_and_lambda()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Oversight:EntityFrameworkCore:Enabled"] = "false",
            })
            .Build();

        var snapshot = OversightOptionsSetup.ResolveSnapshot(
            configuration, oversight => oversight.Prometheus.Enabled = true);

        snapshot.EntityFrameworkCore.Enabled.ShouldBeFalse();
        snapshot.Prometheus.Enabled.ShouldBeTrue();
    }
}
