using System.Reflection;

namespace Farol;

internal static class FarolResource
{
    internal static IReadOnlyList<KeyValuePair<string, object>> BuildFallbackAttributes(
        string? otelServiceName,
        string? otelResourceAttributes,
        string environmentName,
        Assembly? entryAssembly)
    {
        var attributes = new List<KeyValuePair<string, object>>();

        if (string.IsNullOrWhiteSpace(otelServiceName)
            && !ContainsKey(otelResourceAttributes, "service.name"))
        {
            var serviceName = entryAssembly?.GetName().Name;
            if (!string.IsNullOrWhiteSpace(serviceName))
                attributes.Add(new("service.name", serviceName));
        }

        if (!ContainsKey(otelResourceAttributes, "service.version"))
        {
            var version = entryAssembly
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(version))
                attributes.Add(new("service.version", version));
        }

        if (!ContainsKey(otelResourceAttributes, "deployment.environment")
            && !string.IsNullOrWhiteSpace(environmentName))
        {
            attributes.Add(new("deployment.environment", environmentName));
        }

        return attributes;
    }

    private static bool ContainsKey(string? resourceAttributes, string key) =>
        resourceAttributes?
            .Split(',')
            .Any(pair => pair.Split('=')[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
        == true;
}
