using FirebirdAdmin.Application.Metadata;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class MetadataTests
{
    [Fact]
    public void Catalog_ShouldGroupObjectsByKind()
    {
        var catalog = CreateCatalog();

        var groups = catalog.GroupByKind();

        groups[MetadataObjectKind.Table].Should().ContainSingle(item => item.DisplayName == "CUSTOMERS");
        groups[MetadataObjectKind.Procedure].Should().ContainSingle(item => item.DisplayName == "SP_TOTAL");
    }

    [Fact]
    public void CatalogSearch_ShouldFilterByNameOrKind()
    {
        var catalog = CreateCatalog();

        catalog.Search("customer").Should().ContainSingle(item => item.DisplayName == "CUSTOMERS");
        catalog.Search("procedure").Single().Reference.Kind.Should().Be(MetadataObjectKind.Procedure);
    }

    [Fact]
    public void Cache_ShouldBecomeStaleWithoutLosingCatalog()
    {
        var cache = new MetadataCache();
        cache.Store(CreateCatalog());

        cache.MarkStale();

        cache.Current.Should().NotBeNull();
        cache.Current!.State.Should().Be(MetadataCacheState.Stale);
        cache.Current.Objects.Should().HaveCount(2);
    }

    [Fact]
    public void DdlBuilder_ShouldQuoteIdentifiersAndPreserveEmbeddedQuotes()
    {
        var summary = new MetadataObjectSummary(new MetadataObjectReference(MetadataObjectKind.Table, "Order Details"), "Order Details");
        var details = new MetadataObjectDetails(
            summary,
            [new MetadataColumn("Unit \"Price\"", "NUMERIC(15,2)", false, 0)],
            [],
            [],
            [],
            [],
            [],
            null,
            null);

        var ddl = new MetadataDdlBuilder().Build(details);

        ddl.Should().Contain("\"Order Details\"");
        ddl.Should().Contain("\"Unit \"\"Price\"\"\"");
    }

    private static MetadataCatalog CreateCatalog()
    {
        return new MetadataCatalog(
            [
                new MetadataObjectSummary(new MetadataObjectReference(MetadataObjectKind.Table, "CUSTOMERS"), "CUSTOMERS"),
                new MetadataObjectSummary(new MetadataObjectReference(MetadataObjectKind.Procedure, "SP_TOTAL"), "SP_TOTAL")
            ],
            DateTimeOffset.UtcNow,
            MetadataCacheState.Fresh);
    }
}
