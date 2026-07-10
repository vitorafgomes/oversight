using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Oversight;

internal static class OversightOptionsSetup
{
    internal static void EnsureRegistered(IHostApplicationBuilder builder, Action<OversightOptions>? configure)
    {
        if (!builder.Services.Any(d => d.ServiceType == typeof(OversightOptionsMarker)))
        {
            builder.Services.AddSingleton<OversightOptionsMarker>();
            builder.Services.AddOptions<OversightOptions>()
                .BindConfiguration(OversightOptions.SectionName)
                .Validate(
                    static options => options.NoiseReduction.ExcludedPaths
                        .All(static p => !string.IsNullOrWhiteSpace(p) && p.StartsWith('/')),
                    "Oversight: every NoiseReduction:ExcludedPaths entry must be a non-empty path glob starting with '/'.")
                .ValidateOnStart();
        }

        if (configure is not null)
            builder.Services.Configure(configure);
    }

    internal static OversightOptions ResolveSnapshot(IConfiguration configuration, Action<OversightOptions>? configure)
    {
        var snapshot = new OversightOptions();
        configuration.GetSection(OversightOptions.SectionName).Bind(snapshot);
        configure?.Invoke(snapshot);
        return snapshot;
    }
}

internal sealed class OversightOptionsMarker { }
