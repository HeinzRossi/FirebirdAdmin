# Diagnóstico — Central de Alertas

## Modelo mental

```text
Sinal → Regra → Alerta → Evidência → Entidade → Investigação
```

## Layout

```text
Summary Cards
FilterBar
Alert List
Alert Details
├── Resumo
├── Evidências
├── Contexto
├── Timeline
└── Regra
```

## Estados

- Active;
- Acknowledged;
- Resolved.

## Severidade

Info, Low, Medium, High, Critical. Cor nunca é o único indicador.

## Comportamentos

- deduplicação;
- FirstSeen/LastSeen/Occurrences;
- reconhecimento com nota opcional;
- resolução automática;
- deep-link para entidade;
- notificações;
- origem do alerta visível;
- histórico SQLite.
