using Oversight;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Oversight.Tests;

public class AddOversightTests
{
    [Fact]
    public void Registers_all_three_package_pipelines()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversight();

        builder.Services.ShouldContain(d => d.ServiceType == typeof(OversightCoreMarker));
        builder.Services.ShouldContain(d => d.ServiceType == typeof(OversightAspNetCoreMarker));
        builder.Services.ShouldContain(d => d.ServiceType == typeof(OversightEntityFrameworkCoreMarker));
    }

    [Fact]
    public void Applies_the_configuration_lambda_exactly_once()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversight(oversight => oversight.NoiseReduction.ExcludedPaths.Add("/custom/*"));
        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<OversightOptions>>().Value;

        options.NoiseReduction.ExcludedPaths.Count(p => p == "/custom/*").ShouldBe(1);
    }

    [Fact]
    public void Prometheus_can_be_enabled_through_the_lambda()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversight(oversight => oversight.Prometheus.Enabled = true);

        builder.Services.ShouldContain(d =>
            d.ServiceType == typeof(IStartupFilter)
            && d.ImplementationType == typeof(OversightPrometheusStartupFilter));
    }

    [Fact]
    public void Calling_add_oversight_twice_is_idempotent()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversight();
        builder.AddOversight();

        builder.Services.Count(d => d.ServiceType == typeof(OversightCoreMarker)).ShouldBe(1);
        builder.Services.Count(d => d.ServiceType == typeof(OversightAspNetCoreMarker)).ShouldBe(1);
        builder.Services.Count(d => d.ServiceType == typeof(OversightEntityFrameworkCoreMarker)).ShouldBe(1);
    }
}
