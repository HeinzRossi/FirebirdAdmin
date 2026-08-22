namespace FirebirdAdmin.Application.Connections;

public enum FirebirdToolKind
{
    ClientLibrary,
    Backup,
    Fix,
    TraceManager
}

public sealed record ToolsetCandidate(
    FirebirdToolKind Kind,
    string Path,
    string? Version,
    bool IsAvailable);

public sealed record EffectiveToolset(IReadOnlyList<ToolsetCandidate> Candidates)
{
    public static EffectiveToolset Empty { get; } = new([]);
}
