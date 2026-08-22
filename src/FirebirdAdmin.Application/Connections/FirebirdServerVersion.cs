using System.Globalization;
using System.Text.RegularExpressions;

namespace FirebirdAdmin.Application.Connections;

public sealed partial record FirebirdServerVersion(int Major, int Minor, int Patch, string Raw)
{
    public static FirebirdServerVersion Unknown(string raw)
    {
        return new FirebirdServerVersion(0, 0, 0, raw);
    }

    public static FirebirdServerVersion Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Unknown(string.Empty);
        }

        var match = VersionRegex().Match(raw);
        if (!match.Success)
        {
            return Unknown(raw);
        }

        return new FirebirdServerVersion(
            int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture),
            match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture) : 0,
            raw);
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Raw) ? "Unknown" : Raw;
    }

    [GeneratedRegex(@"(?<major>\d+)\.(?<minor>\d+)(?:\.(?<patch>\d+))?")]
    private static partial Regex VersionRegex();
}
