using Oversight;
using Shouldly;
using Xunit;

namespace Oversight.Core.Tests;

public class PathGlobMatcherTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/HEALTH")]
    [InlineData("/health/")]
    public void Matches_exact_paths_case_insensitively_ignoring_trailing_slash(string path) =>
        new PathGlobMatcher(new[] { "/health" }).IsExcluded(path).ShouldBeTrue();

    [Theory]
    [InlineData("/internal/jobs")]
    [InlineData("/internal/jobs/123")]
    public void Star_glob_matches_any_suffix_including_nested_segments(string path) =>
        new PathGlobMatcher(new[] { "/internal/*" }).IsExcluded(path).ShouldBeTrue();

    [Theory]
    [InlineData("/healthz")]
    [InlineData("/api/health")]
    [InlineData("/internal")]
    public void Does_not_match_paths_outside_the_glob(string path) =>
        new PathGlobMatcher(new[] { "/health", "/internal/*" }).IsExcluded(path).ShouldBeFalse();

    [Fact]
    public void Matches_any_of_multiple_globs() =>
        new PathGlobMatcher(new[] { "/health", "/metrics" }).IsExcluded("/metrics").ShouldBeTrue();

    [Fact]
    public void Empty_glob_list_excludes_nothing() =>
        new PathGlobMatcher(Array.Empty<string>()).IsExcluded("/health").ShouldBeFalse();
}
