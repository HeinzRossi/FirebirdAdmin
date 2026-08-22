using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Monitoring;

namespace FirebirdAdmin.Presentation.Wpf.Monitoring;

public sealed partial class TransactionRowViewModel : ObservableObject
{
    [ObservableProperty]
    private long transactionId;

    [ObservableProperty]
    private long? attachmentId;

    [ObservableProperty]
    private string? state;

    [ObservableProperty]
    private DateTimeOffset? startedAt;

    [ObservableProperty]
    private long? oldestTransaction;

    [ObservableProperty]
    private long? oldestActive;

    [ObservableProperty]
    private long? isolationMode;

    [ObservableProperty]
    private long? lockTimeout;

    public TransactionRowViewModel(TransactionSnapshot snapshot)
    {
        Apply(snapshot);
    }

    public void Apply(TransactionSnapshot snapshot)
    {
        TransactionId = snapshot.TransactionId;
        AttachmentId = snapshot.AttachmentId;
        State = snapshot.State;
        StartedAt = snapshot.StartedAt;
        OldestTransaction = snapshot.OldestTransaction;
        OldestActive = snapshot.OldestActive;
        IsolationMode = snapshot.IsolationMode;
        LockTimeout = snapshot.LockTimeout;
    }
}
