# Contratos Application

Os contratos abaixo são direcionais; assinaturas podem evoluir sem alterar os boundaries.

```csharp
public interface IProfilerSessionService
{
    Task<ProfilerSession> StartAsync(
        ProfilerOptions options,
        CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

public interface IProfilerEventStream
{
    IAsyncEnumerable<ProfilerEvent> ReadAllAsync(
        CancellationToken cancellationToken);
}

public interface IMetadataCatalogService
{
    Task<MetadataCatalog> LoadCatalogAsync(
        CancellationToken cancellationToken);

    Task<MetadataObjectDetails> LoadDetailsAsync(
        MetadataObjectReference reference,
        CancellationToken cancellationToken);
}

public interface IFirebirdToolRunner
{
    Task<ToolExecutionResult> ExecuteAsync(
        FirebirdToolCommand command,
        IProgress<ToolOutput>? progress,
        CancellationToken cancellationToken);
}

public interface IBackupService
{
    Task<BackupResult> ExecuteAsync(
        BackupRequest request,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken);
}
```

## Regras

- `CancellationToken` em I/O e operações longas.
- Requests e snapshots preferencialmente imutáveis.
- Nenhum contrato Application referencia WPF.
- Senhas não fazem parte de modelos persistidos ou logs.
