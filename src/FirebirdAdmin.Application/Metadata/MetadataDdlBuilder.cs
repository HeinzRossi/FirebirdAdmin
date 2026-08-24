using System.Text;

namespace FirebirdAdmin.Application.Metadata;

public sealed class MetadataDdlBuilder : IMetadataDdlBuilder
{
    public string Build(MetadataObjectDetails details)
    {
        return details.Summary.Reference.Kind switch
        {
            MetadataObjectKind.Table => BuildTable(details),
            MetadataObjectKind.View => $"CREATE VIEW {Quote(details.Summary.Reference.Name)} AS{Environment.NewLine}{details.Source ?? "/* source unavailable */"}",
            MetadataObjectKind.Procedure => $"CREATE PROCEDURE {Quote(details.Summary.Reference.Name)} AS{Environment.NewLine}{details.Source ?? "/* source unavailable */"}",
            MetadataObjectKind.Function => $"CREATE FUNCTION {Quote(details.Summary.Reference.Name)} AS{Environment.NewLine}{details.Source ?? "/* source unavailable */"}",
            MetadataObjectKind.Trigger => $"CREATE TRIGGER {Quote(details.Summary.Reference.Name)} AS{Environment.NewLine}{details.Source ?? "/* source unavailable */"}",
            MetadataObjectKind.Sequence => $"CREATE SEQUENCE {Quote(details.Summary.Reference.Name)};",
            MetadataObjectKind.Domain => $"CREATE DOMAIN {Quote(details.Summary.Reference.Name)} AS {details.Columns.FirstOrDefault()?.DataType ?? "UNKNOWN"};",
            MetadataObjectKind.Role => $"CREATE ROLE {Quote(details.Summary.Reference.Name)};",
            _ => $"/* DDL unavailable for {details.Summary.Reference.Kind} {details.Summary.Reference.Name} */"
        };
    }

    private static string BuildTable(MetadataObjectDetails details)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"CREATE TABLE {Quote(details.Summary.Reference.Name)} (");
        for (var index = 0; index < details.Columns.Count; index++)
        {
            var column = details.Columns[index];
            var suffix = index == details.Columns.Count - 1 ? string.Empty : ",";
            builder.AppendLine($"  {Quote(column.Name)} {column.DataType}{(column.IsNullable ? string.Empty : " NOT NULL")}{suffix}");
        }

        builder.Append(");");
        return builder.ToString();
    }

    public static string Quote(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}
