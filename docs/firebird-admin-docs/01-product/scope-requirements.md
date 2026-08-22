# Escopo e Requisitos

## Requisitos funcionais

### RF-001 — Conexão
Conectar a um banco Firebird e detectar versão/capabilities.

### RF-002 — Monitoramento
Exibir attachments, transações, statements e métricas disponíveis via mecanismos suportados.

### RF-003 — SQL Profiler
Capturar eventos SQL via estratégia de Trace compatível com a versão.

### RF-004 — Histórico
Persistir eventos e métricas relevantes em SQLite.

### RF-005 — Diagnóstico
Avaliar regras configuráveis e gerar alertas com evidências.

### RF-006 — Metadata
Explorar objetos, estrutura, código e dependências em modo read-only.

### RF-007 — Manutenção
Executar Backup, Restore, Validation e Sweep com preflight e histórico.

### RF-008 — Exportação
Exportar dados selecionados ou históricos em CSV/JSON.

## Requisitos não funcionais

- UI responsiva durante coleta intensa.
- Sem segredos em logs/SQLite.
- Compatibilidade explícita Firebird 2.5–5.x.
- Cancelamento cooperativo.
- Virtualização em grids grandes.
- Acessibilidade e navegação por teclado.
- Light/Dark e densidades Comfortable/Compact.
- Localização por resources.
