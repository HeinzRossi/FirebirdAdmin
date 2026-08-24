using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Presentation.Wpf.Profiler;

public sealed partial class ProfilerWorkspaceViewModel(
    IProfilerSessionService profilerSessionService,
    IHistoryWriter historyWriter) : ObservableObject
{
    private readonly ProfilerBuffer buffer = new();
    private CancellationTokenSource? readCts;
    private bool suppressInspectSwitch;
    private Guid? activeConnectionProfileId;

    [ObservableProperty]
    private ProfilerState state = ProfilerState.Disconnected;

    [ObservableProperty]
    private string message = "Conecte a um banco para iniciar o SQL Profiler.";

    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private bool isFollowing = true;

    [ObservableProperty]
    private ProfilerEventRowViewModel? selectedEvent;

    public ObservableCollection<ProfilerEventRowViewModel> Events { get; } = [];
    public event EventHandler<ProfilerEvent>? ProfilerEventReceived;

    public string StateText => State.ToString();
    public int EventCount => Events.Count;
    public int BufferedCount => buffer.Events.Count;
    public string SelectedSql => SelectedEvent?.Event.Sql ?? "-";
    public string SelectedPerformance => SelectedEvent is null
        ? "-"
        : $"Duration: {SelectedEvent.Duration} ms | Reads: {SelectedEvent.Reads} | Writes: {SelectedEvent.Writes} | Fetches: {SelectedEvent.Fetches}";
    public string SelectedContext => SelectedEvent is null
        ? "-"
        : $"User: {SelectedEvent.UserName ?? "-"} | Attachment: {SelectedEvent.AttachmentId?.ToString() ?? "-"} | Transaction: {SelectedEvent.TransactionId?.ToString() ?? "-"}";
    public string SelectedPlan => SelectedEvent?.Plan ?? "-";
    public string SelectedRawTrace => SelectedEvent?.RawTrace ?? "-";

    public async Task StartAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken)
    {
        if (!connection.Capabilities.SupportsTrace)
        {
            SetFailed("Trace não suportado pela versão detectada.");
            return;
        }

        if (password is null)
        {
            SetFailed("A senha da sessão não está disponível. Reconecte ou salve o perfil com senha lembrada.");
            return;
        }

        State = ProfilerState.Starting;
        Message = "Iniciando sessão Trace...";
        OnPropertyChanged(nameof(StateText));

        var options = new ProfilerOptions(connection, $"FirebirdAdmin-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}");
        activeConnectionProfileId = connection.ProfileId;
        await profilerSessionService.StartAsync(options, password, cancellationToken);

        State = ProfilerState.Running;
        Message = "Captura Trace em execução.";
        IsFollowing = true;
        OnPropertyChanged(nameof(StateText));

        await (readCts?.CancelAsync() ?? Task.CompletedTask);
        readCts = new CancellationTokenSource();
        _ = ReadEventsAsync(readCts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        State = ProfilerState.Stopping;
        Message = "Encerrando captura Trace...";
        OnPropertyChanged(nameof(StateText));

        await (readCts?.CancelAsync() ?? Task.CompletedTask);
        await profilerSessionService.StopAsync(cancellationToken);

        State = ProfilerState.Ready;
        Message = "Captura Trace encerrada.";
        OnPropertyChanged(nameof(StateText));
    }

    public void PauseView()
    {
        if (State is not ProfilerState.Running)
        {
            return;
        }

        State = ProfilerState.PausedView;
        IsFollowing = false;
        Message = "Visualização pausada. Captura continua em buffer.";
        OnPropertyChanged(nameof(StateText));
    }

    public void ResumeFollow()
    {
        IsFollowing = true;
        if (State is ProfilerState.PausedView)
        {
            State = ProfilerState.Running;
        }

        ApplyFilter();
        SelectLast();
        Message = "Follow ativo.";
        OnPropertyChanged(nameof(StateText));
    }

    public void Clear()
    {
        buffer.Clear();
        Events.Clear();
        SelectedEvent = null;
        OnCountsChanged();
    }

    public void SetReady()
    {
        if (State is ProfilerState.Disconnected)
        {
            State = ProfilerState.Ready;
            Message = "SQL Profiler pronto.";
            OnPropertyChanged(nameof(StateText));
        }
    }

    public void SetFailed(string message)
    {
        State = ProfilerState.Failed;
        Message = message;
        OnPropertyChanged(nameof(StateText));
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedEventChanged(ProfilerEventRowViewModel? value)
    {
        if (!suppressInspectSwitch && value is not null && State is ProfilerState.Running)
        {
            IsFollowing = false;
        }

        OnPropertyChanged(nameof(SelectedSql));
        OnPropertyChanged(nameof(SelectedPerformance));
        OnPropertyChanged(nameof(SelectedContext));
        OnPropertyChanged(nameof(SelectedPlan));
        OnPropertyChanged(nameof(SelectedRawTrace));
    }

    private async Task ReadEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var profilerEvent in profilerSessionService.ReadAllAsync(cancellationToken))
            {
                buffer.Add(profilerEvent);
                ProfilerEventReceived?.Invoke(this, profilerEvent);
                _ = PersistProfilerEventAsync(profilerEvent);

                if (State is not ProfilerState.PausedView)
                {
                    AddIfVisible(profilerEvent);
                }

                OnCountsChanged();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetFailed(ex.Message);
        }
    }

    private void ApplyFilter()
    {
        Events.Clear();
        foreach (var profilerEvent in buffer.ApplyFilter(CreateFilter()))
        {
            Events.Add(new ProfilerEventRowViewModel(profilerEvent));
        }

        if (IsFollowing)
        {
            SelectLast();
        }

        OnCountsChanged();
    }

    private void AddIfVisible(ProfilerEvent profilerEvent)
    {
        if (!CreateFilter().Matches(profilerEvent))
        {
            return;
        }

        Events.Add(new ProfilerEventRowViewModel(profilerEvent));
        if (IsFollowing)
        {
            SelectLast();
        }
    }

    private ProfilerFilter CreateFilter()
    {
        return new ProfilerFilter(SqlText: FilterText);
    }

    private void SelectLast()
    {
        if (Events.Count == 0)
        {
            return;
        }

        suppressInspectSwitch = true;
        SelectedEvent = Events[^1];
        suppressInspectSwitch = false;
    }

    private void OnCountsChanged()
    {
        OnPropertyChanged(nameof(EventCount));
        OnPropertyChanged(nameof(BufferedCount));
    }

    private async Task PersistProfilerEventAsync(ProfilerEvent profilerEvent)
    {
        try
        {
            await historyWriter.WriteProfilerEventsAsync(activeConnectionProfileId, [profilerEvent], CancellationToken.None);
        }
        catch (Exception ex)
        {
            Message = $"Falha ao persistir histórico do profiler: {ex.Message}";
        }
    }
}
