# Firebird Capabilities

## Objetivo

Representar o que a conexão atual realmente suporta sem espalhar comparações de versão.

Exemplo conceitual:

```csharp
public sealed record FirebirdCapabilities(
    bool SupportsTrace,
    bool SupportsPackages,
    bool SupportsStandaloneFunctions,
    bool SupportsIdentityColumns,
    bool SupportsSqlSecurity,
    MetadataCapabilities Metadata,
    MonitoringCapabilities Monitoring);
```

## Regras

- resolver capabilities na conexão;
- manter modelos imutáveis;
- UI consome capacidades via Application/ViewModel;
- recursos indisponíveis ficam ocultos/desabilitados com explicação;
- nunca simular uma métrica inexistente.
