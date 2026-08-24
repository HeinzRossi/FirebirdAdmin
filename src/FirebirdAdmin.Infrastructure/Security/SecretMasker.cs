using System.Text.RegularExpressions;

namespace FirebirdAdmin.Infrastructure.Security;

public static partial class SecretMasker
{
    public const string Mask = "***";

    public static string MaskSecrets(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var masked = PasswordKeyRegex().Replace(value, match => $"{match.Groups["key"].Value}{Mask}");
        return UserPasswordRegex().Replace(masked, match => $"{match.Groups["prefix"].Value}{Mask}");
    }

    [GeneratedRegex(@"(?<key>\b(?:password|pwd|pass|senha)\s*=\s*)[^;,\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordKeyRegex();

    [GeneratedRegex(@"(?<prefix>-(?:password|pwd|pass)\s+)[^;,\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex UserPasswordRegex();
}
