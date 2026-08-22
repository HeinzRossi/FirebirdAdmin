# SQL Profiler

## Objetivo

Capturar, filtrar e investigar eventos SQL em tempo real.

## Layout

```text
Trace Session Banner
Toolbar
FilterBar
Follow / Inspect
Profiler Grid
Splitter
Event Details
├── SQL
├── Performance
├── Contexto
├── Plano
└── Trace
```

## Toolbar

- Iniciar;
- Pausar visualização;
- Encerrar captura;
- Limpar visualização;
- Exportar.

**Pausar visualização não pausa Trace.**

## Filtros

- busca SQL;
- tipo;
- usuário;
- duração;
- attachment;
- transaction;
- queries lentas;
- filtros avançados.

## Performance

A UI recebe eventos em batches. O histórico completo fica no SQLite; a janela em memória é limitada.

## Critérios essenciais

- Follow/Inspect;
- não perder eventos por lentidão visual;
- SQL e parâmetros somente quando realmente disponíveis;
- capabilities controlam métricas;
- plano quando disponível;
- CSV/JSON;
- navegação para conexão/transação/diagnóstico.
