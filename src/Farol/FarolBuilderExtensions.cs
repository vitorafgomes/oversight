using Microsoft.Extensions.Hosting;

namespace Farol;

/// <summary>One-call Farol setup for ASP.NET Core hosts: core + web + EF Core telemetry.</summary>
public static class FarolBuilderExtensions
{
    public static IHostApplicationBuilder AddFarol(this IHostApplicationBuilder builder) =>
        builder.AddFarol(static _ => { });

    public static IHostApplicationBuilder AddFarol(
        this IHostApplicationBuilder builder,
        Action<FarolOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        FarolOptionsSetup.EnsureRegistered(builder, configure);
        var snapshot = FarolOptionsSetup.ResolveSnapshot(builder.Configuration, configure);

        FarolCoreBuilderExtensions.AddFarolCoreInternal(builder);
        FarolAspNetCoreBuilderExtensions.AddFarolAspNetCoreInternal(builder, snapshot);
        FarolEntityFrameworkCoreBuilderExtensions.AddFarolEntityFrameworkCoreInternal(builder, snapshot);

        return builder;
    }
}
