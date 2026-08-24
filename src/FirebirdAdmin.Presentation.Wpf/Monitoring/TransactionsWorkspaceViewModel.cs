using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Monitoring;

namespace FirebirdAdmin.Presentation.Wpf.Monitoring;

public sealed partial class TransactionsWorkspaceViewModel : ObservableObject
{
    private readonly Dictionary<long, TransactionRowViewModel> rowsById = [];

    [ObservableProperty]
    private TransactionsWorkspaceState state = TransactionsWorkspaceState.Disconnected;

    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private TransactionRowViewModel? selectedTransaction;

    [ObservableProperty]
    private DateTimeOffset? lastUpdatedAt;

    [ObservableProperty]
    private string message = "Conecte a um banco para iniciar monitoramento.";

    public ObservableCollection<TransactionRowViewModel> Transactions { get; } = [];

    public int TransactionCount => Transactions.Count;

    public int ActiveTransactionCount => Transactions.Count(row => row.State is not "0");

    public void ApplySnapshot(MonitoringSnapshot snapshot)
    {
        State = TransactionsWorkspaceState.Ready;
        LastUpdatedAt = snapshot.CapturedAt;
        Message = "Snapshots MON$ aplicados.";

        var incomingIds = snapshot.Transactions.Select(transaction => transaction.TransactionId).ToHashSet();

        foreach (var existingId in rowsById.Keys.Except(incomingIds).ToArray())
        {
            var row = rowsById[existingId];
            rowsById.Remove(existingId);
            Transactions.Remove(row);
        }

        foreach (var transaction in snapshot.Transactions)
        {
            if (rowsById.TryGetValue(transaction.TransactionId, out var existing))
            {
                existing.Apply(transaction);
            }
            else
            {
                var row = new TransactionRowViewModel(transaction);
                rowsById.Add(transaction.TransactionId, row);
                Transactions.Add(row);
            }
        }

        if (SelectedTransaction is not null &&
            rowsById.TryGetValue(SelectedTransaction.TransactionId, out var selected))
        {
            SelectedTransaction = selected;
        }

        OnPropertyChanged(nameof(TransactionCount));
        OnPropertyChanged(nameof(ActiveTransactionCount));
    }

    public void SetLoading()
    {
        State = TransactionsWorkspaceState.Loading;
        Message = "Coletando MON$...";
    }

    public void SetDisconnected()
    {
        State = TransactionsWorkspaceState.Disconnected;
        Message = "Conecte a um banco para iniciar monitoramento.";
    }

    public void SetError(string message)
    {
        State = TransactionsWorkspaceState.Error;
        Message = message;
    }
}
