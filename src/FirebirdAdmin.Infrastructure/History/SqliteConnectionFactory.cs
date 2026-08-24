using Microsoft.Data.Sqlite;
using FirebirdAdmin.Infrastructure.Persistence;

namespace FirebirdAdmin.Infrastructure.History;

public sealed class SqliteConnectionFactory(ApplicationDataPaths paths)
{
    public SqliteConnection Create()
    {
        return new SqliteConnection($"Data Source={paths.DatabasePath};Pooling=False");
    }
}
