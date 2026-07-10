using Oversight;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using Shouldly;
using Xunit;

namespace Oversight.AspNetCore.Tests;

public class AddOversightAspNetCoreTests
{
    [Fact]
    public void Trace_filter_excludes_default_noise_paths()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightAspNetCore();
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
        builder.AddOversightAspNetCore(oversight => oversight.NoiseReduction.ExcludedPaths.Add("/internal/*"));
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
        builder.AddOversightAspNetCore();

        builder.Services.ShouldNotContain(d =>
            d.ImplementationType == typeof(OversightPrometheusStartupFilter));
    }

    [Fact]
    public void Prometheus_startup_filter_is_registered_when_enabled_in_configuration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Oversight:Prometheus:Enabled"] = "true",
        });
        builder.AddOversightAspNetCore();

        builder.Services.ShouldContain(d =>
            d.ServiceType == typeof(IStartupFilter)
            && d.ImplementationType == typeof(OversightPrometheusStartupFilter));
    }

    [Fact]
    public void Calling_add_oversight_asp_net_core_twice_registers_once()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddOversightAspNetCore();
        builder.AddOversightAspNetCore();

        builder.Services.Count(d => d.ServiceType == typeof(OversightAspNetCoreMarker)).ShouldBe(1);
    }

    private static DefaultHttpContext HttpContextWithPath(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context;
    }
}
