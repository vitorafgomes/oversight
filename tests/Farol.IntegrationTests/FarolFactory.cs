using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Farol.IntegrationTests;

internal static class FarolFactory
{
    internal static WebApplicationFactory<Program> Create(
        List<Activity> exportedActivities,
        params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);
            builder.ConfigureTestServices(services =>
                services.ConfigureOpenTelemetryTracerProvider(tracing =>
                    tracing.AddInMemoryExporter(exportedActivities)));
        });

    internal static void Flush(WebApplicationFactory<Program> factory) =>
        factory.Services.GetRequiredService<TracerProvider>().ForceFlush();
}
