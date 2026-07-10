using System.Text.RegularExpressions;

namespace Oversight;

internal sealed class PathGlobMatcher
{
    private readonly Regex[] _patterns;

    internal PathGlobMatcher(IEnumerable<string> globs)
    {
        // Globs compile once at construction so misconfiguration surfaces at startup,
        // never per-request.
        _patterns = globs
            .Select(static glob => new Regex(
                "^" + Regex.Escape(glob.TrimEnd('/')).Replace(@"\*", ".*", StringComparison.Ordinal) + "$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant))
            .ToArray();
    }

    internal bool IsExcluded(string path)
    {
        var normalized = path.Length > 1 ? path.TrimEnd('/') : path;
        return _patterns.Any(pattern => pattern.IsMatch(normalized));
    }
}
