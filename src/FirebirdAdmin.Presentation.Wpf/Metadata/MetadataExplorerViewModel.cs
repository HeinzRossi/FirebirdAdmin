using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Metadata;

namespace FirebirdAdmin.Presentation.Wpf.Metadata;

public sealed partial class MetadataExplorerViewModel(IMetadataCatalogService catalogService) : ObservableObject, IDisposable
{
    private readonly List<MetadataObjectReference> backStack = [];
    private readonly List<MetadataObjectReference> forwardStack = [];
    private ConnectionContext? activeConnection;
    private CredentialSecret? password;
    private MetadataCatalog? catalog;
    private bool suppressHistory;
    private bool disposed;

    [ObservableProperty]
    private MetadataCacheState state = MetadataCacheState.Empty;

    [ObservableProperty]
    private string message = "Conecte um banco para carregar o catálogo.";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private MetadataObjectRowViewModel? selectedObject;

    [ObservableProperty]
    private MetadataObjectDetails? selectedDetails;

    public ObservableCollection<MetadataObjectRowViewModel> Objects { get; } = [];
    public string StateText => State.ToString();
    public string SummaryText => catalog is null ? "0 objeto(s)" : $"{catalog.Objects.Count} objeto(s) no catálogo.";
    public string SelectedOverview => SelectedDetails is null
        ? "-"
        : $"{SelectedDetails.Summary.Reference.Kind}: {SelectedDetails.Summary.DisplayName}";
    public string SelectedColumns => FormatRows(SelectedDetails?.Columns.Select(column =>
        $"{column.Position}. {column.Name} {column.DataType}{(column.IsNullable ? string.Empty : " NOT NULL")} {column.DefaultSource}".Trim()));
    public string SelectedParameters => FormatRows(SelectedDetails?.Parameters.Select(parameter =>
        $"{parameter.Position}. {parameter.Direction} {parameter.Name} {parameter.DataType}"));
    public string SelectedIndexes => FormatRows(SelectedDetails?.Indexes.Select(index =>
        $"{index.Name} {(index.IsUnique ? "UNIQUE" : "INDEX")} ({string.Join(", ", index.Columns)})"));
    public string SelectedConstraints => FormatRows(SelectedDetails?.Constraints.Select(constraint =>
        $"{constraint.Name} {constraint.Type} ({string.Join(", ", constraint.Columns)})"));
    public string SelectedTriggers => FormatRows(SelectedDetails?.Triggers.Select(trigger =>
        $"{trigger.Name} {(trigger.IsActive ? "ACTIVE" : "INACTIVE")}"));
    public string SelectedDependencies => FormatRows(SelectedDetails?.Dependencies.Select(dependency =>
        $"{dependency.Direction}: {dependency.Reference.Kind} {dependency.Reference.Name}"));
    public string SelectedSource => SelectedDetails?.Source ?? "-";
    public string SelectedDdl => SelectedDetails?.Ddl ?? "-";
    public string SelectedError => SelectedDetails?.Error ?? "-";
    public bool CanNavigateBack => backStack.Count > 0;
    public bool CanNavigateForward => forwardStack.Count > 0;

    public void SetConnection(ConnectionContext connection, CredentialSecret? credential)
    {
        activeConnection = connection;
        password?.Dispose();
        password = credential is null ? null : CredentialSecret.FromBytes(credential.CopyBytes());
        State = MetadataCacheState.Empty;
        Message = "Conexão ativa. Catálogo pronto para carregar.";
    }

    public void MarkStale()
    {
        catalogService.MarkCacheStale();
        catalog = catalogService.GetCachedCatalog();
        State = catalog?.State ?? MetadataCacheState.Empty;
        Message = catalog is null
            ? "Sem cache de metadata."
            : "Cache de metadata mantido como stale após desconexão.";
        OnStateChanged();
    }

    public async Task LoadCatalogAsync(CancellationToken cancellationToken = default)
    {
        if (activeConnection is null)
        {
            catalog = catalogService.GetCachedCatalog();
            ApplyCatalog();
            Message = catalog is null ? "Sem conexão ativa." : "Cache de metadata disponível em modo stale.";
            return;
        }

        Message = "Carregando catálogo de metadata...";
        OnStateChanged();
        try
        {
            catalog = await catalogService.LoadCatalogAsync(activeConnection, password, cancellationToken);
            ApplyCatalog();
            Message = $"Catálogo carregado: {catalog.Objects.Count} objeto(s).";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
            OnStateChanged();
        }
    }

    public Task RefreshCatalogAsync(CancellationToken cancellationToken = default)
    {
        return LoadCatalogAsync(cancellationToken);
    }

    public async Task RefreshSelectedAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedObject is not null)
        {
            await LoadDetailsAsync(SelectedObject.Reference, trackHistory: false, cancellationToken);
        }
    }

    public async Task BackAsync(CancellationToken cancellationToken = default)
    {
        if (backStack.Count == 0 || SelectedObject is null)
        {
            return;
        }

        var current = SelectedObject.Reference;
        var previous = backStack[^1];
        backStack.RemoveAt(backStack.Count - 1);
        forwardStack.Add(current);
        await NavigateToAsync(previous, cancellationToken);
    }

    public async Task ForwardAsync(CancellationToken cancellationToken = default)
    {
        if (forwardStack.Count == 0 || SelectedObject is null)
        {
            return;
        }

        var current = SelectedObject.Reference;
        var next = forwardStack[^1];
        forwardStack.RemoveAt(forwardStack.Count - 1);
        backStack.Add(current);
        await NavigateToAsync(next, cancellationToken);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyCatalog();
    }

    partial void OnSelectedObjectChanged(MetadataObjectRowViewModel? value)
    {
        if (value is null || suppressHistory)
        {
            return;
        }

        _ = LoadDetailsSafelyAsync(value.Reference, trackHistory: true, CancellationToken.None);
    }

    partial void OnSelectedDetailsChanged(MetadataObjectDetails? value)
    {
        OnPropertyChanged(nameof(SelectedOverview));
        OnPropertyChanged(nameof(SelectedColumns));
        OnPropertyChanged(nameof(SelectedParameters));
        OnPropertyChanged(nameof(SelectedIndexes));
        OnPropertyChanged(nameof(SelectedConstraints));
        OnPropertyChanged(nameof(SelectedTriggers));
        OnPropertyChanged(nameof(SelectedDependencies));
        OnPropertyChanged(nameof(SelectedSource));
        OnPropertyChanged(nameof(SelectedDdl));
        OnPropertyChanged(nameof(SelectedError));
    }

    partial void OnStateChanged(MetadataCacheState value)
    {
        OnStateChanged();
    }

    private async Task LoadDetailsAsync(MetadataObjectReference reference, bool trackHistory, CancellationToken cancellationToken)
    {
        if (activeConnection is null)
        {
            Message = "Detalhes exigem conexão ativa. Cache permanece visível.";
            return;
        }

        if (trackHistory && SelectedDetails?.Summary.Reference is { } current && current != reference)
        {
            backStack.Add(current);
            forwardStack.Clear();
            OnHistoryChanged();
        }

        Message = $"Carregando detalhes de {reference.Kind} {reference.Name}...";
        SelectedDetails = await catalogService.LoadDetailsAsync(activeConnection, password, reference, cancellationToken);
        Message = SelectedDetails.Error is null ? "Detalhes carregados." : $"Falha no detalhe: {SelectedDetails.Error}";
        OnHistoryChanged();
    }

    private async Task LoadDetailsSafelyAsync(MetadataObjectReference reference, bool trackHistory, CancellationToken cancellationToken)
    {
        try
        {
            await LoadDetailsAsync(reference, trackHistory, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException) when (disposed)
        {
        }
        catch (Exception ex)
        {
            if (!disposed)
            {
                Message = ex.Message;
            }
        }
    }

    private async Task NavigateToAsync(MetadataObjectReference reference, CancellationToken cancellationToken)
    {
        var row = Objects.FirstOrDefault(item => item.Reference == reference);
        if (row is null)
        {
            return;
        }

        suppressHistory = true;
        SelectedObject = row;
        suppressHistory = false;
        await LoadDetailsAsync(reference, trackHistory: false, cancellationToken);
    }

    private void ApplyCatalog()
    {
        Objects.Clear();
        if (catalog is null)
        {
            State = MetadataCacheState.Empty;
            OnStateChanged();
            return;
        }

        foreach (var item in catalog.Search(SearchText).OrderBy(item => item.Reference.Kind).ThenBy(item => item.DisplayName))
        {
            Objects.Add(new MetadataObjectRowViewModel(item));
        }

        State = catalog.State;
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(SummaryText));
    }

    private void OnHistoryChanged()
    {
        OnPropertyChanged(nameof(CanNavigateBack));
        OnPropertyChanged(nameof(CanNavigateForward));
    }

    private static string FormatRows(IEnumerable<string>? rows)
    {
        if (rows is null)
        {
            return "-";
        }

        var builder = new StringBuilder();
        foreach (var row in rows.Where(row => !string.IsNullOrWhiteSpace(row)))
        {
            builder.AppendLine(row);
        }

        return builder.Length == 0 ? "-" : builder.ToString();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        password?.Dispose();
        disposed = true;
    }
}
