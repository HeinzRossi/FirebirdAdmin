# Estratégia de Testes

## Pirâmide

### Unitários
- Domain;
- regras;
- parsers;
- normalizers;
- capabilities;
- policies;
- funções puras.

### Application
- use cases;
- validação;
- orquestração;
- cancelamento;
- falhas de adapters.

### Infrastructure
- SQLite;
- masking;
- toolset discovery;
- adapters.

### Integração
Ambientes Firebird reais para 2.5, 3.x, 4.x e 5.x.

Validar:

- conexão;
- MON$;
- metadata;
- Trace;
- backup/restore;
- validation/sweep;
- diferenças de capabilities.

### UI
Priorizar ViewModels e máquinas de estado. End-to-end somente nos fluxos críticos.

## Performance

Cenários obrigatórios:

- Trace com alta taxa;
- grid com janela grande;
- persistência em batch;
- filtros históricos;
- troca Follow/Inspect;
- polling sem congelamento.

## UX & Performance — Sprint 11

Critérios de aceite:

- o Shell renderiza somente o workspace ativo, evitando custo inicial de todos os grids e do ScottPlot;
- grids usam virtualização de linha e coluna com reciclagem;
- troca de workspace preserva estado dos ViewModels carregados;
- troca de workspace não dispara consultas Firebird fora dos fluxos já existentes de conexão, refresh ou ação explícita;
- Histórico continua paginado e não carrega o SQLite completo em memória;
- atalhos `Ctrl+1..9`, `F5` e `Esc` funcionam sem bloquear a UI;
- campos e ações principais expõem nomes de acessibilidade e ordem de tabulação estável.
