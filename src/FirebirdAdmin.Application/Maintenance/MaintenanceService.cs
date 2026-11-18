using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Maintenance;

public sealed class MaintenanceService(
    IMaintenancePreflightService preflightService,
    IFirebirdToolRunner toolRunner,
    IMaintenanceHistoryStore historyStore) : IMaintenanceService
{
    private readonly SemaphoreSlim operationGate = new(1, 1);

    public MaintenanceOperation? ActiveOperation { get; private set; }
    public event EventHandler<MaintenanceProgress>? ProgressChanged;
    public event EventHandler<MaintenanceLogLine>? LogReceived;

    public Task<MaintenancePreflightResult> ValidateAsync(MaintenanceRequest request, CancellationToken cancellationToken)
    {
        return preflightService.ValidateAsync(request, cancellationToken);
    }

    public async Task<MaintenanceResult> ExecuteAsync(MaintenanceRequest request, CredentialSecret? password, CancellationToken cancellationToken)
    {
        if (!await operationGate.WaitAsync(0, cancellationToken))
        {
            throw new InvalidOperationException("Já existe uma operação de manutenção em execução.");
        }

        var operationId = Guid.NewGuid();
        var logs = new List<MaintenanceLogLine>();
        var started = DateTimeOffset.UtcNow;
        ActiveOperation = new MaintenanceOperation(
            operationId,
            request.Connection.ProfileId,
            request.Type,
            MaintenanceOperationStatus.Running,
            request.Source,
            request.Target,
            started,
            null,
            0,
            "Operação em execução.");

        try
        {
            var preflight = await preflightService.ValidateAsync(request, cancellationToken);
            if (!preflight.CanExecute || !request.Confirmed)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, preflight.Errors.Count > 0 ? preflight.Errors : ["Confirmação obrigatória."]));
            }

            await historyStore.SaveOperationAsync(ActiveOperation, cancellationToken);
            RaiseProgress(operationId, "Executando", null, "Operação iniciada.");

            var command = BuildCommand(request, password);
            var result = await toolRunner.ExecuteAsync(
                operationId,
                command,
                new Progress<MaintenanceLogLine>(line =>
                {
                    logs.Add(line);
                    LogReceived?.Invoke(this, line);
                    _ = historyStore.SaveLogAsync(line, CancellationToken.None);
                }),
                cancellationToken);

            var status = result.ExitCode == 0 ? MaintenanceOperationStatus.Succeeded : MaintenanceOperationStatus.Failed;
            ActiveOperation = ActiveOperation with
            {
                Status = status,
                FinishedAt = DateTimeOffset.UtcNow,
                ExitCode = result.ExitCode,
                Message = status is MaintenanceOperationStatus.Succeeded ? "Operação concluída." : $"Operação falhou com código de saída {result.ExitCode}."
            };
            await historyStore.SaveOperationAsync(ActiveOperation, CancellationToken.None);
            RaiseProgress(operationId, "Resultado", 1, ActiveOperation.Message);
            return new MaintenanceResult(ActiveOperation, logs);
        }
        catch (OperationCanceledException)
        {
            ActiveOperation = ActiveOperation with
            {
                Status = MaintenanceOperationStatus.Cancelled,
                FinishedAt = DateTimeOffset.UtcNow,
                Message = "Operação cancelada."
            };
            await historyStore.SaveOperationAsync(ActiveOperation, CancellationToken.None);
            RaiseProgress(operationId, "Cancelado", null, ActiveOperation.Message);
            return new MaintenanceResult(ActiveOperation, logs);
        }
        catch (Exception ex)
        {
            ActiveOperation = ActiveOperation with
            {
                Status = MaintenanceOperationStatus.Failed,
                FinishedAt = DateTimeOffset.UtcNow,
                Message = ex.Message
            };
            await historyStore.SaveOperationAsync(ActiveOperation, CancellationToken.None);
            RaiseProgress(operationId, "Falha", null, ex.Message);
            return new MaintenanceResult(ActiveOperation, logs);
        }
        finally
        {
            ActiveOperation = null;
            operationGate.Release();
        }
    }

    private void RaiseProgress(Guid operationId, string stage, double? percent, string message)
    {
        ProgressChanged?.Invoke(this, new MaintenanceProgress(operationId, stage, percent, message, DateTimeOffset.UtcNow));
    }

    private static FirebirdToolCommand BuildCommand(MaintenanceRequest request, CredentialSecret? password)
    {
        var toolKind = request.Type is MaintenanceOperationType.Backup or MaintenanceOperationType.Restore
            ? FirebirdToolKind.Backup
            : FirebirdToolKind.Fix;
        var tool = request.Connection.Toolset.Candidates.First(candidate => candidate.Kind == toolKind && candidate.IsAvailable);
        var args = request.Type switch
        {
            MaintenanceOperationType.Backup => BuildBackupArguments(request, password),
            MaintenanceOperationType.Restore => BuildRestoreArguments(request, password),
            MaintenanceOperationType.Validation => BuildValidationArguments(request, password),
            MaintenanceOperationType.Sweep => BuildSweepArguments(request, password),
            _ => throw new InvalidOperationException("Operação não suportada.")
        };

        return new FirebirdToolCommand(toolKind, tool.Path, args, Path.GetDirectoryName(tool.Path) ?? Environment.CurrentDirectory, BuildToolEnvironment(password));
    }

    private static IReadOnlyList<string> BuildBackupArguments(MaintenanceRequest request, CredentialSecret? password)
    {
        return BuildAuthenticatedArguments(request.Connection, password, ["-b", request.Source, request.Target!]);
    }

    private static IReadOnlyList<string> BuildRestoreArguments(MaintenanceRequest request, CredentialSecret? password)
    {
        return BuildAuthenticatedArguments(request.Connection, password, ["-c", request.Source, request.Target!]);
    }

    private static IReadOnlyList<string> BuildValidationArguments(MaintenanceRequest request, CredentialSecret? password)
    {
        return BuildAuthenticatedArguments(request.Connection, password, ["-v", "-full", request.Source]);
    }

    private static IReadOnlyList<string> BuildSweepArguments(MaintenanceRequest request, CredentialSecret? password)
    {
        return BuildAuthenticatedArguments(request.Connection, password, ["-sweep", request.Source]);
    }

    private static IReadOnlyList<string> BuildAuthenticatedArguments(ConnectionContext connection, CredentialSecret? password, IReadOnlyList<string> tail)
    {
        var args = new List<string> { "-user", connection.UserName };
        args.AddRange(tail);
        return args;
    }

    private static IReadOnlyDictionary<string, string> BuildToolEnvironment(CredentialSecret? password)
    {
        return password is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["ISC_PASSWORD"] = password.RevealAsString() };
    }
}
