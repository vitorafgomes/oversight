using Farol;
using Shouldly;
using Xunit;

namespace Farol.Core.Tests;

public class FarolOptionsTests
{
    [Fact]
    public void Prometheus_is_disabled_by_default() =>
        new FarolOptions().Prometheus.Enabled.ShouldBeFalse();

    [Fact]
    public void Noise_reduction_excludes_common_infrastructure_paths_by_default() =>
        new FarolOptions().NoiseReduction.ExcludedPaths.ShouldBe(
            new[] { "/health", "/healthz", "/alive", "/ready", "/metrics" });

    [Fact]
    public void Entity_framework_instrumentation_is_enabled_by_default() =>
        new FarolOptions().EntityFrameworkCore.Enabled.ShouldBeTrue();

    [Fact]
    public void Query_text_capture_is_opt_in() =>
        new FarolOptions().EntityFrameworkCore.CaptureQueryText.ShouldBeFalse();

    [Fact]
    public void Section_name_is_farol() =>
        FarolOptions.SectionName.ShouldBe("Farol");
}
