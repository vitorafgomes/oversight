using Farol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Farol.Core.Tests;

public class FarolOptionsBindingTests
{
    [Fact]
    public void Binds_options_from_the_farol_configuration_section()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Farol:Prometheus:Enabled"] = "true",
            ["Farol:EntityFrameworkCore:CaptureQueryText"] = "true",
            ["Farol:NoiseReduction:ExcludedPaths:0"] = "/internal/*",
        });
        builder.AddFarolCore();
        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<FarolOptions>>().Value;

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
            ["Farol:Prometheus:Enabled"] = "false",
        });
        builder.AddFarolCore(farol => farol.Prometheus.Enabled = true);
        using var host = builder.Build();

        host.Services.GetRequiredService<IOptions<FarolOptions>>()
            .Value.Prometheus.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Invalid_excluded_path_fails_at_startup_not_at_request_time()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarolCore(farol => farol.NoiseReduction.ExcludedPaths.Add("no-leading-slash"));
        using var host = builder.Build();

        await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public void Resolve_snapshot_merges_section_and_lambda()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Farol:EntityFrameworkCore:Enabled"] = "false",
            })
            .Build();

        var snapshot = FarolOptionsSetup.ResolveSnapshot(
            configuration, farol => farol.Prometheus.Enabled = true);

        snapshot.EntityFrameworkCore.Enabled.ShouldBeFalse();
        snapshot.Prometheus.Enabled.ShouldBeTrue();
    }
}
