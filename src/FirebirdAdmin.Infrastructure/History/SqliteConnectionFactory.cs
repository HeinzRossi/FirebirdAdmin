using Microsoft.Data.Sqlite;
using FirebirdAdmin.Infrastructure.Persistence;

namespace FirebirdAdmin.Infrastructure.History;

public sealed class SqliteConnectionFactory(ApplicationDataPaths paths)
{
    public SqliteConnection Create()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = paths.DatabasePath,
            Pooling = false,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        return new SqliteConnection(connectionString);
    }
}
