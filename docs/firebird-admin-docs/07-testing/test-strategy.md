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
