# Metadata Explorer

## Objetivo

Navegar por estrutura, código e dependências em modo read-only.

## Árvore

Categorias conforme capability:

- Tables;
- Views;
- Procedures;
- Functions;
- Packages;
- Triggers;
- Sequences/Generators;
- Domains;
- Exceptions;
- Roles.

Objetos de sistema ficam ocultos por padrão.

## Detalhes

- Overview;
- Columns/Parameters;
- Indexes;
- Constraints;
- Triggers;
- Dependencies;
- Source;
- DDL.

## Regras

- catálogo inicial leve;
- detalhes lazy-loaded;
- busca global;
- dependências `Depende de` e `Usado por`;
- Back/Forward interno;
- quoted identifiers preservados;
- refresh por objeto ou catálogo;
- cache pode permanecer disponível como `Stale` após desconexão;
- nenhuma edição/execução de DDL no MVP.
