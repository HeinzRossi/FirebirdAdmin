# Firebird Admin — Documentação do Projeto

> **Status:** Baseline v1  
> **Fonte oficial:** este diretório `/docs` em Markdown  
> **Plataforma:** C# + .NET + WPF  
> **Banco alvo:** Firebird 2.5, 3.x, 4.x e 5.x  
> **Idioma inicial:** pt-BR

## Visão geral

Firebird Admin é uma aplicação desktop profissional para administração segura, monitoramento em tempo real, profiling SQL, diagnóstico, exploração de metadata e manutenção controlada de um único banco Firebird por sessão.

A arquitetura é completa desde o início, mas a implementação será incremental. O primeiro ciclo prioriza observabilidade, investigação, histórico e operações administrativas seguras.

## Decisões principais

| Área | Decisão |
|---|---|
| Plataforma | C# + .NET + WPF |
| MVVM | CommunityToolkit.Mvvm |
| UI | WPF UI + Design System próprio |
| Charts | ScottPlot |
| Firebird | Provider base + capabilities + strategies |
| Integração | Híbrida: provider .NET + tooling + nativo quando necessário |
| Persistência | SQLite |
| Acesso SQLite | EF Core + Dapper |
| Polling | Configurável + adaptativo |
| Logging | `Microsoft.Extensions.Logging` + Serilog |
| Diagnóstico | Motor de regras simples + presets |
| Notificações | Arquitetura extensível |
| Localização | Preparada; pt-BR no primeiro release |
| Testes | Pirâmide completa |
| Menu principal | Expandido por padrão, recolhível |
| Administração | Segurança primeiro |

## Índice

### Produto
- [Visão](01-product/vision.md)
- [Escopo e requisitos](01-product/scope-requirements.md)
- [Roadmap](01-product/roadmap.md)

### Arquitetura
- [Visão arquitetural](02-architecture/architecture-overview.md)
- [Estrutura da solução](02-architecture/solution-structure.md)
- [Integração Firebird e multi-versão](02-architecture/firebird-integration.md)
- [Monitoring e Profiler](02-architecture/monitoring-profiler.md)
- [Persistência](02-architecture/persistence.md)
- [Diagnóstico e alertas](02-architecture/diagnostics.md)
- [Segurança, logging e localização](02-architecture/cross-cutting.md)

### UI/UX
- [Arquitetura UI e Design System](03-ui-ux/ui-design-system.md)
- [Mapa de navegação](03-ui-ux/navigation-map.md)
- [Shell e Dashboard](03-ui-ux/shell-dashboard.md)
- [Monitoramento — Transações](03-ui-ux/monitoring-transactions.md)
- [SQL Profiler](03-ui-ux/sql-profiler.md)
- [Central de Alertas](03-ui-ux/alerts-center.md)
- [Metadata Explorer](03-ui-ux/metadata-explorer.md)
- [Manutenção](03-ui-ux/maintenance.md)

### Decisões
- [Índice de ADRs](04-adr/README.md)

### Contratos
- [Contratos Application](05-contracts/application-contracts.md)
- [Modelos de domínio](05-contracts/domain-models.md)
- [Capabilities](05-contracts/firebird-capabilities.md)

### Planejamento
- [Sprints](06-planning/sprints.md)
- [Backlog](06-planning/backlog.md)
- [Riscos e Definition of Done](06-planning/risks-dod.md)

### Testes
- [Estratégia de testes](07-testing/test-strategy.md)
- [Matriz Firebird](07-testing/firebird-version-matrix.md)

## Princípios

1. Segurança operacional antes de conveniência.
2. UI nunca acessa Firebird, SQLite ou processos externos diretamente.
3. Diferenças de versão ficam na Infrastructure.
4. Captura e persistência têm prioridade sobre renderização.
5. Diagnósticos precisam apresentar evidências.
6. Estados de UI são explícitos.
7. Componentes visuais reutilizáveis pertencem ao Design System.
8. Código e contratos devem favorecer testabilidade, imutabilidade e efeitos colaterais nas bordas.
