namespace FirebirdAdmin.Application.Security;

public sealed record SecurityPrivilege(string Code, string Name)
{
    public static SecurityPrivilege FromCode(string? code)
    {
        var normalized = string.IsNullOrWhiteSpace(code) ? "?" : code.Trim().ToUpperInvariant();
        return normalized switch
        {
            "S" => new SecurityPrivilege("S", "SELECT"),
            "I" => new SecurityPrivilege("I", "INSERT"),
            "U" => new SecurityPrivilege("U", "UPDATE"),
            "D" => new SecurityPrivilege("D", "DELETE"),
            "R" => new SecurityPrivilege("R", "REFERENCES"),
            "X" => new SecurityPrivilege("X", "EXECUTE"),
            "A" => new SecurityPrivilege("A", "ALL"),
            "G" => new SecurityPrivilege("G", "USAGE"),
            "M" => new SecurityPrivilege("M", "MEMBER OF"),
            "C" => new SecurityPrivilege("C", "CREATE"),
            "L" => new SecurityPrivilege("L", "ALTER"),
            "O" => new SecurityPrivilege("O", "DROP"),
            _ => new SecurityPrivilege(normalized, $"UNKNOWN ({normalized})")
        };
    }
}
