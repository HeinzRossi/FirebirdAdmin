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

## Matriz Firebird opcional

Os testes padrão não exigem Docker. Para validar Firebird 2.5/3/4/5 localmente:

```powershell
.\scripts\firebird-matrix-up.ps1
.\scripts\firebird-matrix-test.ps1
.\scripts\firebird-matrix-down.ps1
```

Também é possível apontar `tests/FirebirdAdmin.IntegrationTests` para servidores já existentes usando `FIREBIRDADMIN_FB25_*`, `FIREBIRDADMIN_FB30_*`, `FIREBIRDADMIN_FB40_*` e `FIREBIRDADMIN_FB50_*`.

## Sprint 0

Esta base entrega solution, camadas, DI/Host, logging, resources pt-BR, Design System minimo e Shell inicial. Conexao Firebird, SQLite e operacoes administrativas entram em sprints posteriores.
