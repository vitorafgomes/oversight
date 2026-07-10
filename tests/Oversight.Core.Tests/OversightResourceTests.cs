using System.Reflection;
using Oversight;
using Shouldly;
using Xunit;

namespace Oversight.Core.Tests;

public class OversightResourceTests
{
    private static readonly Assembly TestAssembly = typeof(OversightResourceTests).Assembly;

    [Fact]
    public void Falls_back_to_entry_assembly_name_for_service_name()
    {
        var attributes = OversightResource.BuildFallbackAttributes(null, null, "Production", TestAssembly);

        attributes.ShouldContain(a => a.Key == "service.name" && (string)a.Value == "Oversight.Core.Tests");
    }

    [Fact]
    public void Does_not_set_service_name_when_otel_service_name_is_present()
    {
        var attributes = OversightResource.BuildFallbackAttributes("my-service", null, "Production", TestAssembly);

        attributes.ShouldNotContain(a => a.Key == "service.name");
    }

    [Fact]
    public void Does_not_set_service_name_when_resource_attributes_already_carry_it()
    {
        var attributes = OversightResource.BuildFallbackAttributes(
            null, "service.name=my-svc", "Production", TestAssembly);

        attributes.ShouldNotContain(a => a.Key == "service.name");
    }

    [Fact]
    public void Adds_service_version_from_informational_version()
    {
        var expected = TestAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        var attributes = OversightResource.BuildFallbackAttributes(null, null, "Production", TestAssembly);

        attributes.ShouldContain(a => a.Key == "service.version" && (string)a.Value == expected);
    }

    [Fact]
    public void Does_not_set_service_version_when_resource_attributes_already_carry_it()
    {
        var attributes = OversightResource.BuildFallbackAttributes(
            null, "service.version=9.9.9,team=core", "Production", TestAssembly);

        attributes.ShouldNotContain(a => a.Key == "service.version");
    }

    [Fact]
    public void Adds_deployment_environment_from_host_environment()
    {
        var attributes = OversightResource.BuildFallbackAttributes(null, null, "Staging", TestAssembly);

        attributes.ShouldContain(a => a.Key == "deployment.environment" && (string)a.Value == "Staging");
    }

    [Fact]
    public void Does_not_set_deployment_environment_when_resource_attributes_already_carry_it()
    {
        var attributes = OversightResource.BuildFallbackAttributes(
            null, "deployment.environment=prod", "Staging", TestAssembly);

        attributes.ShouldNotContain(a => a.Key == "deployment.environment");
    }

    [Fact]
    public void Null_entry_assembly_produces_no_service_name()
    {
        var attributes = OversightResource.BuildFallbackAttributes(null, null, "Production", null);

        attributes.ShouldNotContain(a => a.Key == "service.name");
    }

    [Theory]
    [InlineData("service.version = 1.0 , team=core", false)]  // whitespace around key is trimmed
    [InlineData("SERVICE.VERSION=1.0", false)]                // key match is case-insensitive
    [InlineData("team=a=b,service.version=2", false)]         // value containing '=' does not confuse key parsing
    [InlineData("team=core,", true)]                          // trailing comma yields an empty pair, no key match
    public void Detects_service_version_key_across_parse_edges(string resourceAttributes, bool expectServiceVersionAdded)
    {
        var attributes = OversightResource.BuildFallbackAttributes(null, resourceAttributes, "Production", TestAssembly);

        attributes.Any(a => a.Key == "service.version").ShouldBe(expectServiceVersionAdded);
    }
}
