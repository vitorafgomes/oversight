using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Farol;

/// <summary>Host-agnostic Farol entry point: OTel pipeline, OTLP export, resource identity.</summary>
public static class FarolCoreBuilderExtensions
{
    public static IHostApplicationBuilder AddFarolCore(
        this IHostApplicationBuilder builder,
        Action<FarolOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        FarolOptionsSetup.EnsureRegistered(builder, configure);
        AddFarolCoreInternal(builder);
        return builder;
    }

    internal static void AddFarolCoreInternal(IHostApplicationBuilder builder)
    {
        if (builder.Services.Any(d => d.ServiceType == typeof(FarolCoreMarker)))
            return;
        builder.Services.AddSingleton<FarolCoreMarker>();
    }
}

internal sealed class FarolCoreMarker { }
