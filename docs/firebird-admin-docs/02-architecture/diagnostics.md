# Diagnóstico e Alertas

## Pipeline

```mermaid
flowchart TD
    E[Normalized Event/Snapshot] --> C[Diagnostic Context]
    C --> R[Rules]
    R --> DR[Diagnostic Result]
    DR --> AC[Alert Correlator]
    AC --> STORE[Alert Store]
    STORE --> DB[(SQLite)]
    STORE --> UI[Central de Alertas]
    STORE --> NOTIF[Notification Dispatcher]
```

## Regras

- determinísticas;
- configuráveis;
- presets Conservador/Normal/Agressivo/Personalizado;
- evidências estruturadas;
- sem dependência de WPF.

## Ciclo de vida

```text
Active → Acknowledged → Resolved
```

Reconhecimento não significa resolução.

## Deduplicação

Identidade lógica aproximada:

```text
RuleId + DatabaseSession + TargetType + TargetId
```

Preservar `FirstSeen`, `LastSeen` e `Occurrences`.

## Notificações

`INotificationChannel` permite:

- in-app;
- Windows no primeiro release;
- e-mail/webhook/Teams futuramente.

Cooldown impede spam.
