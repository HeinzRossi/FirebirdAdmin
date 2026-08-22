# Firebird Admin

Aplicacao desktop Windows para administracao segura, monitoramento, profiling SQL, diagnostico, metadata read-only e manutencao controlada de bancos Firebird.

## Desenvolvimento local

Requisitos:

- .NET SDK 10
- Windows Desktop runtime 10

Comandos:

```powershell
dotnet restore FirebirdAdmin.sln
dotnet build FirebirdAdmin.sln
dotnet test FirebirdAdmin.sln
dotnet run --project src/FirebirdAdmin.Bootstrapper
```

## Sprint 0

Esta base entrega solution, camadas, DI/Host, logging, resources pt-BR, Design System minimo e Shell inicial. Conexao Firebird, SQLite e operacoes administrativas entram em sprints posteriores.
