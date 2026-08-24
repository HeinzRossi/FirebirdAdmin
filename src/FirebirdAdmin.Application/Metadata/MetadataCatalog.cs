namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataCatalog(
    IReadOnlyList<MetadataObjectSummary> Objects,
    DateTimeOffset LoadedAt,
    MetadataCacheState State)
{
    public IReadOnlyDictionary<MetadataObjectKind, IReadOnlyList<MetadataObjectSummary>> GroupByKind()
    {
        return Objects
            .GroupBy(item => item.Reference.Kind)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<MetadataObjectSummary>)group.OrderBy(item => item.DisplayName).ToArray());
    }

    public IReadOnlyList<MetadataObjectSummary> Search(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Objects;
        }

        return Objects.Where(item =>
            item.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            item.Reference.Kind.ToString().Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
