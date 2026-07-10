using Farol;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Farol.Tests;

public class AddFarolTests
{
    [Fact]
    public void Registers_all_three_package_pipelines()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarol();

        builder.Services.ShouldContain(d => d.ServiceType == typeof(FarolCoreMarker));
        builder.Services.ShouldContain(d => d.ServiceType == typeof(FarolAspNetCoreMarker));
        builder.Services.ShouldContain(d => d.ServiceType == typeof(FarolEntityFrameworkCoreMarker));
    }

    [Fact]
    public void Applies_the_configuration_lambda_exactly_once()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarol(farol => farol.NoiseReduction.ExcludedPaths.Add("/custom/*"));
        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<FarolOptions>>().Value;

        options.NoiseReduction.ExcludedPaths.Count(p => p == "/custom/*").ShouldBe(1);
    }

    [Fact]
    public void Prometheus_can_be_enabled_through_the_lambda()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarol(farol => farol.Prometheus.Enabled = true);

        builder.Services.ShouldContain(d =>
            d.ServiceType == typeof(IStartupFilter)
            && d.ImplementationType == typeof(FarolPrometheusStartupFilter));
    }

    [Fact]
    public void Calling_add_farol_twice_is_idempotent()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarol();
        builder.AddFarol();

        builder.Services.Count(d => d.ServiceType == typeof(FarolCoreMarker)).ShouldBe(1);
        builder.Services.Count(d => d.ServiceType == typeof(FarolAspNetCoreMarker)).ShouldBe(1);
        builder.Services.Count(d => d.ServiceType == typeof(FarolEntityFrameworkCoreMarker)).ShouldBe(1);
    }
}
