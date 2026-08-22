# Modelos de Domínio

## Profiler

```csharp
public sealed record ProfilerEvent(
    long Sequence,
    DateTimeOffset Timestamp,
    TraceEventType Type,
    TimeSpan? Duration,
    string? UserName,
    long? AttachmentId,
    long? TransactionId,
    string? Sql,
    ProfilerMetrics Metrics);
```

## Diagnóstico

```csharp
public sealed record DiagnosticEvidence(
    string Key,
    object? Value,
    string? Unit = null);

public sealed record DiagnosticResult(
    string RuleId,
    DiagnosticSeverity Severity,
    string MessageKey,
    IReadOnlyList<DiagnosticEvidence> Evidence);
```

## Alertas

```csharp
public sealed record Alert(
    Guid Id,
    string RuleId,
    DiagnosticSeverity Severity,
    AlertStatus Status,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    int Occurrences,
    DiagnosticTarget Target,
    IReadOnlyList<DiagnosticEvidence> Evidence);
```

## Maintenance

```csharp
public sealed record OperationProgress(
    string Stage,
    double? Percent,
    string? CurrentObject,
    string? Message);
```

`Percent = null` significa progresso indeterminado.

## Metadata

Identificadores quoted devem ser preservados semanticamente, não apenas como strings normalizadas.
