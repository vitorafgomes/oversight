namespace Oversight;

internal enum SqlHealthSeverity
{
    Low,
    Medium,
    High,
}

/// <summary>A single health observation. SuggestedScript is advisory text only — never executed.</summary>
internal sealed record SqlHealthFinding(
    string Collector,
    SqlHealthSeverity Severity,
    string Title,
    string? SuggestedScript);
