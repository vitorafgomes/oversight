using Oversight;
using Shouldly;
using Xunit;

namespace Oversight.Core.Tests;

public class OversightOptionsTests
{
    [Fact]
    public void Prometheus_is_disabled_by_default() =>
        new OversightOptions().Prometheus.Enabled.ShouldBeFalse();

    [Fact]
    public void Noise_reduction_excludes_common_infrastructure_paths_by_default() =>
        new OversightOptions().NoiseReduction.ExcludedPaths.ShouldBe(
            new[] { "/health", "/healthz", "/alive", "/ready", "/metrics" });

    [Fact]
    public void Entity_framework_instrumentation_is_enabled_by_default() =>
        new OversightOptions().EntityFrameworkCore.Enabled.ShouldBeTrue();

    [Fact]
    public void Query_text_capture_is_opt_in() =>
        new OversightOptions().EntityFrameworkCore.CaptureQueryText.ShouldBeFalse();

    [Fact]
    public void Section_name_is_oversight() =>
        OversightOptions.SectionName.ShouldBe("Oversight");
}
