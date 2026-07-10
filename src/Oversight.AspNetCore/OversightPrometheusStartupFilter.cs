using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Oversight;

internal sealed class OversightPrometheusStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
            next(app);
        };
}
