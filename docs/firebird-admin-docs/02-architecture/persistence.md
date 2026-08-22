# Persistência Local

## Tecnologia

SQLite com EF Core + Dapper.

### EF Core
- migrations;
- settings;
- entidades administrativas;
- CRUD não crítico.

### Dapper
- ingestão em lote;
- consultas analíticas;
- hot paths.

## Entidades principais

```text
MonitoringSession
TraceEvent
StatementExecution
PerformanceSnapshot
AttachmentSnapshot
TransactionSnapshot
AlertEvent
MaintenanceOperation
ApplicationSetting
```

## Retenção

Política híbrida:

- período padrão proposto: 30 dias;
- limite padrão proposto: 5 GB;
- sessões protegidas não são removidas automaticamente;
- limpeza em background e lotes.

## Regra

O grid nunca representa o armazenamento completo. A UI mantém uma janela limitada; histórico antigo é consultado no SQLite.
