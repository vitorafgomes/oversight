using Microsoft.Extensions.Hosting;

namespace Oversight;

/// <summary>One-call Oversight setup for ASP.NET Core hosts: core + web + EF Core telemetry.</summary>
public static class OversightBuilderExtensions
{
    public static IHostApplicationBuilder AddOversight(this IHostApplicationBuilder builder) =>
        builder.AddOversight(static _ => { });

    public static IHostApplicationBuilder AddOversight(
        this IHostApplicationBuilder builder,
        Action<OversightOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        OversightOptionsSetup.EnsureRegistered(builder, configure);
        var snapshot = OversightOptionsSetup.ResolveSnapshot(builder.Configuration, configure);

        OversightCoreBuilderExtensions.AddOversightCoreInternal(builder);
        OversightAspNetCoreBuilderExtensions.AddOversightAspNetCoreInternal(builder, snapshot);
        OversightEntityFrameworkCoreBuilderExtensions.AddOversightEntityFrameworkCoreInternal(builder, snapshot);

        return builder;
    }
}
