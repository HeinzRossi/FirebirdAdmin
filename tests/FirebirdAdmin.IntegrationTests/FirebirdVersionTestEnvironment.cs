namespace FirebirdAdmin.IntegrationTests;

internal static class FirebirdVersionTestEnvironment
{
    public static IReadOnlyList<FirebirdVersionCase> ReadConfiguredCases()
    {
        return new[]
            {
                Read("FB25", 2),
                Read("FB30", 3),
                Read("FB40", 4),
                Read("FB50", 5)
            }
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();
    }

    private static FirebirdVersionCase? Read(string key, int expectedMajor)
    {
        var prefix = $"FIREBIRDADMIN_{key}_";
        var host = Environment.GetEnvironmentVariable($"{prefix}HOST");
        var database = Environment.GetEnvironmentVariable($"{prefix}DATABASE");
        var user = Environment.GetEnvironmentVariable($"{prefix}USER");
        var password = Environment.GetEnvironmentVariable($"{prefix}PASSWORD");

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(database) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var port = int.TryParse(Environment.GetEnvironmentVariable($"{prefix}PORT"), out var parsedPort)
            ? parsedPort
            : 3050;

        return new FirebirdVersionCase(key, expectedMajor, host, port, database, user, password);
    }
}
