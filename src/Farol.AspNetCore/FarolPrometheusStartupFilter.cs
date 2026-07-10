using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Farol;

internal sealed class FarolPrometheusStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
            next(app);
        };
}
