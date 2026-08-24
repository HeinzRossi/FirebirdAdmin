using FirebirdAdmin.Application.Metadata;

namespace FirebirdAdmin.Presentation.Wpf.Metadata;

public sealed class MetadataObjectRowViewModel(MetadataObjectSummary summary)
{
    public MetadataObjectSummary Summary { get; } = summary;
    public MetadataObjectReference Reference => Summary.Reference;
    public string Name => Summary.DisplayName;
    public string Kind => Summary.Reference.Kind.ToString();
    public string Description => Summary.Description ?? string.Empty;
}
