using System.Diagnostics;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Infrastructure.Security;

namespace FirebirdAdmin.Infrastructure.Connections;

public sealed class FirebirdToolsetDiscoveryService : IFirebirdToolsetDiscoveryService
{
    private static readonly IReadOnlyDictionary<FirebirdToolKind, string[]> ToolNames = new Dictionary<FirebirdToolKind, string[]>
    {
        [FirebirdToolKind.ClientLibrary] = ["fbclient.dll"],
        [FirebirdToolKind.Backup] = ["gbak.exe", "gbak"],
        [FirebirdToolKind.Fix] = ["gfix.exe", "gfix"],
        [FirebirdToolKind.TraceManager] = ["fbtracemgr.exe", "fbtracemgr"]
    };

    public async Task<EffectiveToolset> DiscoverAsync(CancellationToken cancellationToken)
    {
        var candidates = new List<ToolsetCandidate>();
        var roots = GetSearchRoots();

        foreach (var (kind, names) in ToolNames)
        {
            var path = FindFirst(roots, names);

            if (path is null)
            {
                candidates.Add(new ToolsetCandidate(kind, string.Empty, null, IsAvailable: false));
                continue;
            }

            var version = kind == FirebirdToolKind.ClientLibrary
                ? null
                : await TryGetVersionAsync(path, cancellationToken);

            candidates.Add(new ToolsetCandidate(kind, path, version, IsAvailable: true));
        }

        return new EffectiveToolset(candidates);
    }

    private static IReadOnlyList<string> GetSearchRoots()
    {
        var roots = new List<string>();
        AddIfExists(roots, Environment.GetEnvironmentVariable("FIREBIRD_HOME"));

        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathValue))
        {
            foreach (var pathRoot in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                AddIfExists(roots, pathRoot);
            }
        }

        AddFirebirdInstallRoots(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        AddFirebirdInstallRoots(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));

        return roots.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void AddFirebirdInstallRoots(List<string> roots, string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        var firebirdRoot = Path.Combine(root, "Firebird");
        if (!Directory.Exists(firebirdRoot))
        {
            return;
        }

        AddIfExists(roots, firebirdRoot);

        foreach (var directory in Directory.EnumerateDirectories(firebirdRoot))
        {
            AddIfExists(roots, directory);
            AddIfExists(roots, Path.Combine(directory, "bin"));
        }
    }

    private static void AddIfExists(List<string> roots, string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            roots.Add(path);
            AddIfExistsNoRecurse(roots, Path.Combine(path, "bin"));
        }
    }

    private static void AddIfExistsNoRecurse(List<string> roots, string path)
    {
        if (Directory.Exists(path))
        {
            roots.Add(path);
        }
    }

    private static string? FindFirst(IEnumerable<string> roots, IEnumerable<string> names)
    {
        foreach (var root in roots)
        {
            foreach (var name in names)
            {
                var candidate = Path.Combine(root, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static async Task<string?> TryGetVersionAsync(string executablePath, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "-z",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);

            var combined = $"{await outputTask} {await errorTask}".Trim();
            return string.IsNullOrWhiteSpace(combined) ? null : SecretMasker.MaskSecrets(combined);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
