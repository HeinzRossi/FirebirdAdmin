# Shell e Dashboard

## Shell

O menu principal inicia **expandido** e pode recolher.

```text
┌──────────────────────┬─────────────────────────────────────┐
│ Navigation           │ Connection Context                  │
│ Expanded by default  ├─────────────────────────────────────┤
│                      │ Workspace                           │
│                      │                                     │
├──────────────────────┴─────────────────────────────────────┤
│ Status Bar                                                 │
└────────────────────────────────────────────────────────────┘
```

### Estados da navegação

- Expanded;
- Compact;
- Auto, futuramente se necessário.

A preferência é persistida.

## Dashboard

Objetivo: responder rapidamente se o banco está saudável.

Componentes:

- Database Health;
- cards de métricas;
- atividade;
- queries/s;
- transações;
- alertas;
- gráficos ScottPlot;
- timestamp de atualização.

Cards relevantes são clicáveis e levam ao workspace correspondente.
