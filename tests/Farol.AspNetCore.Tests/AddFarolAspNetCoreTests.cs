using Farol;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using Shouldly;
using Xunit;

namespace Farol.AspNetCore.Tests;

public class AddFarolAspNetCoreTests
{
    [Fact]
    public void Trace_filter_excludes_default_noise_paths()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarolAspNetCore();
        using var host = builder.Build();

        var filter = host.Services
            .GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>().Value.Filter;

        filter.ShouldNotBeNull();
        filter!(HttpContextWithPath("/health")).ShouldBeFalse();
        filter(HttpContextWithPath("/metrics")).ShouldBeFalse();
        filter(HttpContextWithPath("/api/orders")).ShouldBeTrue();
    }

    [Fact]
    public void Trace_filter_honors_globs_added_through_the_lambda()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarolAspNetCore(farol => farol.NoiseReduction.ExcludedPaths.Add("/internal/*"));
        using var host = builder.Build();

        var filter = host.Services
            .GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>().Value.Filter;

        filter!(HttpContextWithPath("/internal/jobs/42")).ShouldBeFalse();
        filter(HttpContextWithPath("/api/orders")).ShouldBeTrue();
    }

    [Fact]
    public void Prometheus_startup_filter_is_not_registered_by_default()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarolAspNetCore();

        builder.Services.ShouldNotContain(d =>
            d.ImplementationType == typeof(FarolPrometheusStartupFilter));
    }

    [Fact]
    public void Prometheus_startup_filter_is_registered_when_enabled_in_configuration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Farol:Prometheus:Enabled"] = "true",
        });
        builder.AddFarolAspNetCore();

        builder.Services.ShouldContain(d =>
            d.ServiceType == typeof(IStartupFilter)
            && d.ImplementationType == typeof(FarolPrometheusStartupFilter));
    }

    [Fact]
    public void Calling_add_farol_asp_net_core_twice_registers_once()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarolAspNetCore();
        builder.AddFarolAspNetCore();

        builder.Services.Count(d => d.ServiceType == typeof(FarolAspNetCoreMarker)).ShouldBe(1);
    }

    private static DefaultHttpContext HttpContextWithPath(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context;
    }
}
