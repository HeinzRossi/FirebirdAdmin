# Plano por Sprints

## Sprint 0 — Foundation & Design System
**Objetivo:** criar a base executável.

**Tasks:** solution/projects; DI/Host; logging; resources; Design System; Gallery; Shell.

**Entregável:** aplicação inicia com Shell, tema e navegação.

**Validação:** referências entre projetos e estilos centralizados.

## Sprint 1 — Connection & Capabilities
**Objetivo:** conexão segura e detecção do ambiente.

**Tasks:** perfis; credential store; version detection; capabilities; toolset discovery.

**Entregável:** conectar e exibir contexto real do banco.

## Sprint 2 — Monitoring Engine
**Objetivo:** pipeline MON$ multi-versão.

**Tasks:** strategies; polling; snapshots; channels; transactions workspace.

**Entregável:** monitoramento incremental sem travar UI.

## Sprint 3 — Dashboard
**Objetivo:** visão operacional.

**Tasks:** health; metric cards; ScottPlot; stale/disconnected states.

## Sprint 4 — SQL Profiler
**Objetivo:** captura Trace robusta.

**Tasks:** trace strategy; parser; normalizer; buffers; Follow/Inspect; details.

## Sprint 5 — History & Analytics
**Objetivo:** consolidar SQLite.

**Tasks:** migrations; Dapper batch writers; queries; retention; export.

## Sprint 6 — Diagnostics & Alerts
**Objetivo:** diagnóstico orientado por evidências.

**Tasks:** rules; presets; correlator; Alerts Center; notifications.

## Sprint 7 — Metadata Explorer
**Objetivo:** metadata read-only.

**Tasks:** catalog; lazy details; search; dependencies; DDL/source.

## Sprint 8 — Safe Maintenance
**Objetivo:** operações administrativas controladas.

**Tasks:** tool runner; preflight; backup; restore; validation; sweep; history.

## Sprint 9 — Security
**Objetivo:** usuários, roles e grants dentro do escopo seguro definido.

## Sprint 10 — Multi-version Hardening
**Objetivo:** fechar matriz 2.5/3/4/5.

## Sprint 11 — UX & Performance
**Objetivo:** acessibilidade, High DPI, keyboard e carga.

## Sprint 12 — Release
**Objetivo:** packaging, smoke tests e documentação final.

**Tasks:** versionamento, publish win-x64 self-contained, zip reproduzível, smoke automatizado, workflow manual de release e checklist final.

**Entregável:** pacote MVP sem exigir Firebird real, Docker ou credenciais no fluxo padrão.
