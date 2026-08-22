# ADR-003 — Persistência

**Status:** Aceita

## Decisão

Usar SQLite com EF Core + Dapper.

## Contexto e justificativa

EF Core simplifica migrations/CRUD; Dapper atende hot paths.

## Trade-offs

Dois mecanismos de acesso exigem boundaries e convenções claras.
