# Visão do Produto

## Problema

Administradores Firebird precisam correlacionar conexões, transações, SQL, metadata, saúde do banco e operações administrativas sem depender de diversas ferramentas desconectadas.

## Proposta

Criar uma ferramenta Windows para um banco por sessão que combine:

- monitoramento;
- SQL Profiler;
- diagnóstico orientado por evidências;
- metadata read-only;
- histórico local;
- backup, restore, validation e sweep seguros;
- administração de segurança em evolução posterior.

## Objetivos

- detectar condições que exigem atenção;
- investigar causa e contexto rapidamente;
- funcionar em várias versões Firebird;
- minimizar impacto da própria observabilidade;
- impedir alterações destrutivas acidentais;
- produzir uma base sustentável para evolução.

## Não objetivos iniciais

- IDE SQL completa;
- editor genérico de dados;
- editor visual de schema;
- execução arbitrária de DDL/DML;
- múltiplos bancos simultâneos;
- agente distribuído.
