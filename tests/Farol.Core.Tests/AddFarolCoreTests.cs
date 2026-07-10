using System.Diagnostics;
using Farol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shouldly;
using Xunit;

namespace Farol.Core.Tests;

public class AddFarolCoreTests
{
    [Fact]
    public void Registers_tracer_and_meter_providers()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarolCore();
        using var host = builder.Build();

        host.Services.GetService<TracerProvider>().ShouldNotBeNull();
        host.Services.GetService<MeterProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void Registers_the_startup_diagnostics_hosted_service()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarolCore();

        builder.Services.ShouldContain(d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(FarolStartupDiagnostics));
    }

    [Fact]
    public void Calling_add_farol_core_twice_registers_the_pipeline_once()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarolCore();
        builder.AddFarolCore();

        builder.Services.Count(d => d.ServiceType == typeof(FarolCoreMarker)).ShouldBe(1);
    }

    [Fact]
    public void Exports_spans_through_the_configured_pipeline()
    {
        List<Activity> exported = [];
        var builder = Host.CreateApplicationBuilder();
        builder.AddFarolCore();
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing
            .AddSource("Farol.Core.Tests")
            .AddInMemoryExporter(exported));
        using var host = builder.Build();
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();

        using var source = new ActivitySource("Farol.Core.Tests");
        using (var activity = source.StartActivity("test-span"))
        {
            activity.ShouldNotBeNull();
        }

        tracerProvider.ForceFlush();
        exported.ShouldContain(a => a.OperationName == "test-span");
    }
}
