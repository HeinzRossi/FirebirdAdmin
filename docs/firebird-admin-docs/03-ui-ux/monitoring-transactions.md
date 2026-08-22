# Monitoramento — Transações

## Objetivo

Triar transações ativas sem perder seleção durante atualizações.

## Layout

```text
Page Header
Summary
FilterBar
DataGrid
Splitter
Transaction Details
```

## Detalhes

Tabs propostas:

- Overview;
- Statements;
- Statistics;
- Garbage Collection;
- Timeline.

## Regras

- diff incremental em vez de recriar coleção;
- seleção persistente por ID;
- refresh não rouba scroll;
- grid virtualizado;
- column chooser;
- Comfortable/Compact;
- ações destrutivas não entram no MVP.
