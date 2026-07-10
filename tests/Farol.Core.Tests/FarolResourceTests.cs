using System.Reflection;
using Farol;
using Shouldly;
using Xunit;

namespace Farol.Core.Tests;

public class FarolResourceTests
{
    private static readonly Assembly TestAssembly = typeof(FarolResourceTests).Assembly;

    [Fact]
    public void Falls_back_to_entry_assembly_name_for_service_name()
    {
        var attributes = FarolResource.BuildFallbackAttributes(null, null, "Production", TestAssembly);

        attributes.ShouldContain(a => a.Key == "service.name" && (string)a.Value == "Farol.Core.Tests");
    }

    [Fact]
    public void Does_not_set_service_name_when_otel_service_name_is_present()
    {
        var attributes = FarolResource.BuildFallbackAttributes("my-service", null, "Production", TestAssembly);

        attributes.ShouldNotContain(a => a.Key == "service.name");
    }

    [Fact]
    public void Adds_service_version_from_informational_version()
    {
        var expected = TestAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        var attributes = FarolResource.BuildFallbackAttributes(null, null, "Production", TestAssembly);

        attributes.ShouldContain(a => a.Key == "service.version" && (string)a.Value == expected);
    }

    [Fact]
    public void Does_not_set_service_version_when_resource_attributes_already_carry_it()
    {
        var attributes = FarolResource.BuildFallbackAttributes(
            null, "service.version=9.9.9,team=core", "Production", TestAssembly);

        attributes.ShouldNotContain(a => a.Key == "service.version");
    }

    [Fact]
    public void Adds_deployment_environment_from_host_environment()
    {
        var attributes = FarolResource.BuildFallbackAttributes(null, null, "Staging", TestAssembly);

        attributes.ShouldContain(a => a.Key == "deployment.environment" && (string)a.Value == "Staging");
    }

    [Fact]
    public void Does_not_set_deployment_environment_when_resource_attributes_already_carry_it()
    {
        var attributes = FarolResource.BuildFallbackAttributes(
            null, "deployment.environment=prod", "Staging", TestAssembly);

        attributes.ShouldNotContain(a => a.Key == "deployment.environment");
    }

    [Fact]
    public void Null_entry_assembly_produces_no_service_name()
    {
        var attributes = FarolResource.BuildFallbackAttributes(null, null, "Production", null);

        attributes.ShouldNotContain(a => a.Key == "service.name");
    }
}
