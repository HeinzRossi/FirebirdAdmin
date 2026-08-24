namespace FirebirdAdmin.Application.Metadata;

public interface IMetadataDdlBuilder
{
    string Build(MetadataObjectDetails details);
}
