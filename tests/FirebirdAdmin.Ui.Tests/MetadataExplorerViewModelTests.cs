using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Metadata;
using FirebirdAdmin.Presentation.Wpf.Metadata;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class MetadataExplorerViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartEmptyDisconnected()
    {
        var viewModel = new MetadataExplorerViewModel(new FakeMetadataCatalogService());

        viewModel.State.Should().Be(MetadataCacheState.Empty);
        viewModel.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadCatalogAsync_ShouldLoadAndFilterObjects()
    {
        var viewModel = new MetadataExplorerViewModel(new FakeMetadataCatalogService());
        viewModel.SetConnection(CreateConnection(), CredentialSecret.FromPlainText("masterkey"));

        await viewModel.LoadCatalogAsync();
        viewModel.SearchText = "CUSTOMER";

        viewModel.State.Should().Be(MetadataCacheState.Fresh);
        viewModel.Objects.Should().ContainSingle(item => item.Name == "CUSTOMERS");
    }

    [Fact]
    public async Task SelectingObject_ShouldLazyLoadDetailsAndPreserveNavigation()
    {
        var viewModel = new MetadataExplorerViewModel(new FakeMetadataCatalogService());
        viewModel.SetConnection(CreateConnection(), null);
        await viewModel.LoadCatalogAsync();

        viewModel.SelectedObject = viewModel.Objects[0];
        await WaitUntilAsync(() => viewModel.SelectedDetails is not null);
        viewModel.SelectedObject = viewModel.Objects[1];
        await WaitUntilAsync(() => viewModel.CanNavigateBack);

        viewModel.SelectedDetails!.Summary.Reference.Name.Should().Be("SP_TOTAL");
        await viewModel.BackAsync();
        viewModel.SelectedDetails!.Summary.Reference.Name.Should().Be("CUSTOMERS");
        viewModel.CanNavigateForward.Should().BeTrue();
    }

    [Fact]
    public async Task MarkStale_ShouldKeepCachedCatalogVisible()
    {
        var service = new FakeMetadataCatalogService();
        var viewModel = new MetadataExplorerViewModel(service);
        viewModel.SetConnection(CreateConnection(), null);
        await viewModel.LoadCatalogAsync();

        viewModel.MarkStale();

        viewModel.State.Should().Be(MetadataCacheState.Stale);
        viewModel.Objects.Should().HaveCount(2);
    }

    private static ConnectionContext CreateConnection()
    {
        return new ConnectionContext(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "employee.fdb",
            "SYSDBA",
            FirebirdServerVersion.Parse("5.0.0"),
            new FirebirdCapabilities(true, true, true, true, true, "test"),
            new EffectiveToolset([]),
            DateTimeOffset.UtcNow);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    private sealed class FakeMetadataCatalogService : IMetadataCatalogService
    {
        private MetadataCatalog? catalog;

        public Task<MetadataCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken)
        {
            catalog = new MetadataCatalog(
                [
                    new MetadataObjectSummary(new MetadataObjectReference(MetadataObjectKind.Table, "CUSTOMERS"), "CUSTOMERS"),
                    new MetadataObjectSummary(new MetadataObjectReference(MetadataObjectKind.Procedure, "SP_TOTAL"), "SP_TOTAL")
                ],
                DateTimeOffset.UtcNow,
                MetadataCacheState.Fresh);
            return Task.FromResult(catalog);
        }

        public Task<MetadataObjectDetails> LoadDetailsAsync(
            ConnectionContext connection,
            CredentialSecret? password,
            MetadataObjectReference reference,
            CancellationToken cancellationToken)
        {
            var summary = new MetadataObjectSummary(reference, reference.Name);
            var details = new MetadataObjectDetails(
                summary,
                [new MetadataColumn("ID", "INTEGER", false, 0)],
                [],
                [],
                [],
                [],
                [],
                "source",
                "ddl");
            return Task.FromResult(details);
        }

        public MetadataCatalog? GetCachedCatalog() => catalog;

        public void MarkCacheStale()
        {
            if (catalog is not null)
            {
                catalog = catalog with { State = MetadataCacheState.Stale };
            }
        }
    }
}
