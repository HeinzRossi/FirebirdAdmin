using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Metadata;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdAdmin.Infrastructure.Metadata;

public sealed class FirebirdMetadataQueryStrategy(IMetadataDdlBuilder ddlBuilder) : IMetadataQueryStrategy
{
    public static IReadOnlyList<MetadataObjectKind> GetSupportedKinds(FirebirdCapabilities capabilities)
    {
        var kinds = new List<MetadataObjectKind>
        {
            MetadataObjectKind.Table,
            MetadataObjectKind.View,
            MetadataObjectKind.Procedure,
            MetadataObjectKind.Trigger,
            MetadataObjectKind.Sequence,
            MetadataObjectKind.Domain,
            MetadataObjectKind.Exception,
            MetadataObjectKind.Role
        };

        if (capabilities.SupportsPackages)
        {
            kinds.Add(MetadataObjectKind.Package);
        }

        if (capabilities.SupportsStandaloneFunctions)
        {
            kinds.Add(MetadataObjectKind.Function);
        }

        return kinds;
    }

    public async Task<IReadOnlyList<MetadataObjectSummary>> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken)
    {
        await using var fbConnection = new FbConnection(BuildConnectionString(connection, password));
        await fbConnection.OpenAsync(cancellationToken);

        var objects = new List<MetadataObjectSummary>();
        objects.AddRange(await LoadRelationSummariesAsync(fbConnection, cancellationToken));
        objects.AddRange(await LoadNamedSummariesAsync(fbConnection, MetadataObjectKind.Procedure, "select trim(rdb$procedure_name) from rdb$procedures where coalesce(rdb$system_flag, 0) = 0 order by 1", cancellationToken));
        objects.AddRange(await LoadNamedSummariesAsync(fbConnection, MetadataObjectKind.Trigger, "select trim(rdb$trigger_name) from rdb$triggers where coalesce(rdb$system_flag, 0) = 0 order by 1", cancellationToken));
        objects.AddRange(await LoadNamedSummariesAsync(fbConnection, MetadataObjectKind.Sequence, "select trim(rdb$generator_name) from rdb$generators where coalesce(rdb$system_flag, 0) = 0 order by 1", cancellationToken));
        objects.AddRange(await LoadNamedSummariesAsync(fbConnection, MetadataObjectKind.Domain, "select trim(rdb$field_name) from rdb$fields where coalesce(rdb$system_flag, 0) = 0 order by 1", cancellationToken));
        objects.AddRange(await LoadNamedSummariesAsync(fbConnection, MetadataObjectKind.Exception, "select trim(rdb$exception_name) from rdb$exceptions where coalesce(rdb$system_flag, 0) = 0 order by 1", cancellationToken));
        objects.AddRange(await LoadNamedSummariesAsync(fbConnection, MetadataObjectKind.Role, "select trim(rdb$role_name) from rdb$roles where coalesce(rdb$system_flag, 0) = 0 order by 1", cancellationToken));

        if (connection.Capabilities.SupportsPackages)
        {
            objects.AddRange(await LoadNamedSummariesAsync(fbConnection, MetadataObjectKind.Package, "select trim(rdb$package_name) from rdb$packages where coalesce(rdb$system_flag, 0) = 0 order by 1", cancellationToken));
        }

        if (connection.Capabilities.SupportsStandaloneFunctions)
        {
            objects.AddRange(await LoadNamedSummariesAsync(fbConnection, MetadataObjectKind.Function, "select trim(rdb$function_name) from rdb$functions where coalesce(rdb$system_flag, 0) = 0 order by 1", cancellationToken));
        }

        return objects;
    }

    public async Task<MetadataObjectDetails> LoadDetailsAsync(ConnectionContext connection, CredentialSecret? password, MetadataObjectReference reference, CancellationToken cancellationToken)
    {
        var summary = new MetadataObjectSummary(reference, reference.Name);
        try
        {
            await using var fbConnection = new FbConnection(BuildConnectionString(connection, password));
            await fbConnection.OpenAsync(cancellationToken);

            var columns = reference.Kind is MetadataObjectKind.Table or MetadataObjectKind.View
                ? await LoadColumnsAsync(fbConnection, reference.Name, cancellationToken)
                : reference.Kind is MetadataObjectKind.Domain
                    ? [new MetadataColumn(reference.Name, await LoadDomainTypeAsync(fbConnection, reference.Name, cancellationToken), true, 0)]
                    : [];

            var parameters = reference.Kind is MetadataObjectKind.Procedure or MetadataObjectKind.Function
                ? await LoadParametersAsync(fbConnection, reference.Name, cancellationToken)
                : [];

            var indexes = reference.Kind is MetadataObjectKind.Table
                ? await LoadIndexesAsync(fbConnection, reference.Name, cancellationToken)
                : [];

            var constraints = reference.Kind is MetadataObjectKind.Table
                ? await LoadConstraintsAsync(fbConnection, reference.Name, cancellationToken)
                : [];

            var triggers = reference.Kind is MetadataObjectKind.Table or MetadataObjectKind.View
                ? await LoadTriggersAsync(fbConnection, reference.Name, cancellationToken)
                : reference.Kind is MetadataObjectKind.Trigger
                    ? [new MetadataTrigger(reference.Name, true, await LoadSourceAsync(fbConnection, reference, cancellationToken))]
                    : [];

            var dependencies = await LoadDependenciesAsync(fbConnection, reference, cancellationToken);
            var source = await LoadSourceAsync(fbConnection, reference, cancellationToken);
            var details = new MetadataObjectDetails(summary, columns, parameters, indexes, constraints, triggers, dependencies, source, null);
            return details with { Ddl = ddlBuilder.Build(details) };
        }
        catch (Exception ex)
        {
            return new MetadataObjectDetails(summary, [], [], [], [], [], [], null, null, ex.Message);
        }
    }

    private static string BuildConnectionString(ConnectionContext connection, CredentialSecret? password)
    {
        return new FbConnectionStringBuilder
        {
            DataSource = connection.Host,
            Port = connection.Port,
            Database = connection.Database,
            UserID = connection.UserName,
            Password = password?.RevealAsString() ?? string.Empty,
            Pooling = false
        }.ToString();
    }

    private static async Task<IReadOnlyList<MetadataObjectSummary>> LoadRelationSummariesAsync(FbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select trim(rdb$relation_name), case when rdb$view_blr is null then 0 else 1 end
            from rdb$relations 
            where coalesce(rdb$system_flag, 0) = 0
            order by 1
            """;

        var results = new List<MetadataObjectSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0).Trim();
            var kind = Convert.ToInt32(reader.GetValue(1)) == 0 ? MetadataObjectKind.Table : MetadataObjectKind.View;
            results.Add(new MetadataObjectSummary(new MetadataObjectReference(kind, name), name));
        }

        return results;
    }

    private static async Task<IReadOnlyList<MetadataObjectSummary>> LoadNamedSummariesAsync(FbConnection connection, MetadataObjectKind kind, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var results = new List<MetadataObjectSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0).Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                results.Add(new MetadataObjectSummary(new MetadataObjectReference(kind, name), name));
            }
        }

        return results;
    }

    private static async Task<IReadOnlyList<MetadataColumn>> LoadColumnsAsync(FbConnection connection, string relationName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select trim(rf.rdb$field_name), f.rdb$field_type, coalesce(rf.rdb$null_flag, 0), rf.rdb$field_position, rf.rdb$default_source
            from rdb$relation_fields rf
            join rdb$fields f on f.rdb$field_name = rf.rdb$field_source
            where rf.rdb$relation_name = @name
            order by rf.rdb$field_position
            """;
        command.Parameters.Add("@name", FbDbType.VarChar).Value = relationName;

        var results = new List<MetadataColumn>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MetadataColumn(
                reader.GetString(0).Trim(),
                MapFieldType(reader.GetInt16(1)),
                reader.GetInt16(2) == 0,
                Convert.ToInt32(reader.GetValue(3)),
                reader.IsDBNull(4) ? null : reader.GetString(4).Trim()));
        }

        return results;
    }

    private static async Task<string> LoadDomainTypeAsync(FbConnection connection, string domainName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select rdb$field_type from rdb$fields where rdb$field_name = @name";
        command.Parameters.Add("@name", FbDbType.VarChar).Value = domainName;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null ? "UNKNOWN" : MapFieldType(Convert.ToInt16(value));
    }

    private static async Task<IReadOnlyList<MetadataParameter>> LoadParametersAsync(FbConnection connection, string name, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select trim(pp.rdb$parameter_name), f.rdb$field_type, pp.rdb$parameter_number, pp.rdb$parameter_type
            from rdb$procedure_parameters pp
            join rdb$fields f on f.rdb$field_name = pp.rdb$field_source
            where pp.rdb$procedure_name = @name
            order by pp.rdb$parameter_type, pp.rdb$parameter_number
            """;
        command.Parameters.Add("@name", FbDbType.VarChar).Value = name;

        var results = new List<MetadataParameter>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MetadataParameter(
                reader.GetString(0).Trim(),
                MapFieldType(reader.GetInt16(1)),
                Convert.ToInt32(reader.GetValue(2)),
                Convert.ToInt32(reader.GetValue(3)) == 0 ? "Input" : "Output"));
        }

        return results;
    }

    private static async Task<IReadOnlyList<MetadataIndex>> LoadIndexesAsync(FbConnection connection, string relationName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select trim(i.rdb$index_name), coalesce(i.rdb$unique_flag, 0), trim(s.rdb$field_name)
            from rdb$indices i
            left join rdb$index_segments s on s.rdb$index_name = i.rdb$index_name
            where i.rdb$relation_name = @name
            order by i.rdb$index_name, s.rdb$field_position
            """;
        command.Parameters.Add("@name", FbDbType.VarChar).Value = relationName;

        var rows = new List<(string Name, bool Unique, string Column)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetString(0).Trim(), Convert.ToInt32(reader.GetValue(1)) == 1, reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim()));
        }

        return rows.GroupBy(row => row.Name)
            .Select(group => new MetadataIndex(group.Key, group.First().Unique, group.Select(row => row.Column).Where(column => column.Length > 0).ToArray()))
            .ToArray();
    }

    private static async Task<IReadOnlyList<MetadataConstraint>> LoadConstraintsAsync(FbConnection connection, string relationName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select trim(rc.rdb$constraint_name), trim(rc.rdb$constraint_type), trim(s.rdb$field_name)
            from rdb$relation_constraints rc
            left join rdb$index_segments s on s.rdb$index_name = rc.rdb$index_name
            where rc.rdb$relation_name = @name
            order by rc.rdb$constraint_name, s.rdb$field_position
            """;
        command.Parameters.Add("@name", FbDbType.VarChar).Value = relationName;

        var rows = new List<(string Name, string Type, string Column)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetString(0).Trim(), reader.GetString(1).Trim(), reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim()));
        }

        return rows.GroupBy(row => row.Name)
            .Select(group => new MetadataConstraint(group.Key, group.First().Type, group.Select(row => row.Column).Where(column => column.Length > 0).ToArray()))
            .ToArray();
    }

    private static async Task<IReadOnlyList<MetadataTrigger>> LoadTriggersAsync(FbConnection connection, string relationName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select trim(rdb$trigger_name), coalesce(rdb$trigger_inactive, 0), rdb$trigger_source from rdb$triggers where rdb$relation_name = @name order by rdb$trigger_name";
        command.Parameters.Add("@name", FbDbType.VarChar).Value = relationName;

        var results = new List<MetadataTrigger>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MetadataTrigger(
                reader.GetString(0).Trim(),
                Convert.ToInt32(reader.GetValue(1)) == 0,
                reader.IsDBNull(2) ? null : reader.GetString(2).Trim()));
        }

        return results;
    }

    private static async Task<IReadOnlyList<MetadataDependency>> LoadDependenciesAsync(FbConnection connection, MetadataObjectReference reference, CancellationToken cancellationToken)
    {
        var results = new List<MetadataDependency>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select trim(rdb$depended_on_name), trim(rdb$dependent_name)
            from rdb$dependencies
            where rdb$dependent_name = @name or rdb$depended_on_name = @name
            """;
        command.Parameters.Add("@name", FbDbType.VarChar).Value = reference.Name;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var dependsOn = reader.GetString(0).Trim();
            var usedBy = reader.GetString(1).Trim();
            if (!string.Equals(dependsOn, reference.Name, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new MetadataDependency(new MetadataObjectReference(MetadataObjectKind.Table, dependsOn), "DependsOn"));
            }

            if (!string.Equals(usedBy, reference.Name, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new MetadataDependency(new MetadataObjectReference(MetadataObjectKind.Table, usedBy), "UsedBy"));
            }
        }

        return results;
    }

    private static async Task<string?> LoadSourceAsync(FbConnection connection, MetadataObjectReference reference, CancellationToken cancellationToken)
    {
        var (table, nameColumn, sourceColumn) = reference.Kind switch
        {
            MetadataObjectKind.View => ("rdb$relations", "rdb$relation_name", "rdb$view_source"),
            MetadataObjectKind.Procedure => ("rdb$procedures", "rdb$procedure_name", "rdb$procedure_source"),
            MetadataObjectKind.Trigger => ("rdb$triggers", "rdb$trigger_name", "rdb$trigger_source"),
            MetadataObjectKind.Function => ("rdb$functions", "rdb$function_name", "rdb$function_source"),
            _ => (null, null, null)
        };

        if (table is null)
        {
            return null;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"select {sourceColumn} from {table} where {nameColumn} = @name";
        command.Parameters.Add("@name", FbDbType.VarChar).Value = reference.Name;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value?.ToString()?.Trim();
    }

    private static string MapFieldType(short type)
    {
        return type switch
        {
            7 => "SMALLINT",
            8 => "INTEGER",
            10 => "FLOAT",
            12 => "DATE",
            13 => "TIME",
            14 => "CHAR",
            16 => "BIGINT",
            27 => "DOUBLE PRECISION",
            35 => "TIMESTAMP",
            37 => "VARCHAR",
            261 => "BLOB",
            _ => $"TYPE {type}"
        };
    }
}
