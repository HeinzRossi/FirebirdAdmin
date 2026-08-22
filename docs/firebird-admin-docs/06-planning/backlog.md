# Backlog Técnico

| ID | Task | Resultado esperado | Dependências | Prioridade |
|---|---|---|---|---|
| S0-T01 | Criar solution/projetos | Build e referências corretas | — | Crítica |
| S0-T02 | DI/Host | Composition root funcional | S0-T01 | Crítica |
| S0-T03 | Design System | Tokens/temas/estilos carregados | S0-T01 | Crítica |
| S0-T04 | DesignSystemGallery | Controles e estados validados | S0-T03 | Alta |
| S0-T05 | Shell | Expanded default + Compact persistido | S0-T03 | Crítica |
| S1-T01 | Connection profiles | CRUD sem senha no SQLite | S0-T02 | Crítica |
| S1-T02 | Credential store | Senha opcional protegida | S1-T01 | Crítica |
| S1-T03 | Version detection | Versão detectada | S1-T01 | Crítica |
| S1-T04 | Capabilities resolver | Application independente de versão | S1-T03 | Crítica |
| S1-T05 | Toolset discovery | Auto + fallback manual | S1-T03 | Alta |
| S2-T01 | MON$ strategies | Queries testadas por versão | S1-T04 | Crítica |
| S2-T02 | Polling engine | Adaptativo/cancelável | S2-T01 | Crítica |
| S2-T03 | Snapshot pipeline | Imutável + channels | S2-T02 | Crítica |
| S2-T04 | Transactions UI | Grid/detalhes incrementais | S2-T03 | Alta |
| S3-T01 | Dashboard | Health e métricas | S2-T03 | Alta |
| S4-T01 | Trace strategy | Start/stop multi-versão | S1-T04 | Crítica |
| S4-T02 | Trace parser | Normalização testada | S4-T01 | Crítica |
| S4-T03 | Profiler buffer | UI/persistence desacoplados | S4-T02 | Crítica |
| S4-T04 | Follow/Inspect | Investigação sem perda de contexto | S4-T03 | Crítica |
| S5-T01 | SQLite migrations | Schema versionado | S0-T02 | Crítica |
| S5-T02 | Batch writers | Hot paths eficientes | S5-T01 | Crítica |
| S5-T03 | Retention | Limites por tempo/tamanho | S5-T01 | Alta |
| S6-T01 | Diagnostic rules | Resultados + evidence | S2-T03 | Crítica |
| S6-T02 | Alert correlator | Deduplicação/lifecycle | S6-T01 | Crítica |
| S6-T03 | Alerts Center | Triagem e deep links | S6-T02 | Alta |
| S7-T01 | Metadata catalog | Lazy/multi-versão | S1-T04 | Crítica |
| S7-T02 | Explorer | Tree/details/dependencies | S7-T01 | Alta |
| S8-T01 | Tool runner | Execução segura + masking | S1-T05 | Crítica |
| S8-T02 | Backup | Fluxo completo | S8-T01 | Crítica |
| S8-T03 | Restore | Fluxo seguro | S8-T01 | Crítica |
| S8-T04 | Validation/Sweep | Fluxos completos | S8-T01 | Alta |
| S10-T01 | Compatibility matrix | Firebird 2.5–5.x validado | módulos | Crítica |
| S11-T01 | Accessibility | Keyboard/focus/contrast | UI | Alta |
| S11-T02 | Performance | Sem freezes sob carga alvo | Profiler/History | Crítica |
