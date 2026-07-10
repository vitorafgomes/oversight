using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Farol;

internal static class FarolOptionsSetup
{
    internal static void EnsureRegistered(IHostApplicationBuilder builder, Action<FarolOptions>? configure)
    {
        if (!builder.Services.Any(d => d.ServiceType == typeof(FarolOptionsMarker)))
        {
            builder.Services.AddSingleton<FarolOptionsMarker>();
            builder.Services.AddOptions<FarolOptions>()
                .BindConfiguration(FarolOptions.SectionName)
                .Validate(
                    static options => options.NoiseReduction.ExcludedPaths
                        .All(static p => !string.IsNullOrWhiteSpace(p) && p.StartsWith('/')),
                    "Farol: every NoiseReduction:ExcludedPaths entry must be a non-empty path glob starting with '/'.")
                .ValidateOnStart();
        }

        if (configure is not null)
            builder.Services.Configure(configure);
    }

    internal static FarolOptions ResolveSnapshot(IConfiguration configuration, Action<FarolOptions>? configure)
    {
        var snapshot = new FarolOptions();
        configuration.GetSection(FarolOptions.SectionName).Bind(snapshot);
        configure?.Invoke(snapshot);
        return snapshot;
    }
}

internal sealed class FarolOptionsMarker { }
