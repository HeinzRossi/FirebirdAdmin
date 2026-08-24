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

## Release local

O artefato oficial do MVP e um pacote Windows `win-x64` self-contained em `.zip`.

```powershell
.\scripts\release-publish.ps1
.\scripts\release-smoke.ps1
```

O pacote e gerado em `artifacts\releases\FirebirdAdmin-v1.0.0-win-x64.zip`. Para instalar manualmente, extraia o `.zip` em uma pasta local e execute `FirebirdAdmin.Bootstrapper.exe`.

Smoke manual recomendado:

- abrir o app com `dotnet run --project src/FirebirdAdmin.Bootstrapper --no-build`;
- navegar com `Ctrl+1` a `Ctrl+9`;
- usar `F5` para atualizar o workspace ativo;
- usar `Esc` para cancelar manutencao quando uma operacao estiver em execucao;
- validar que a tela minima nao corta botoes principais nem status bar.

Dados locais:

- SQLite: `%LocalAppData%\FirebirdAdmin\firebird-admin.db`;
- logs: `%LocalAppData%\FirebirdAdmin\Logs`;
- backups pre-migration: `%LocalAppData%\FirebirdAdmin\Backups`;
- exports: `%LocalAppData%\FirebirdAdmin\Exports`.

Limites do MVP:

- sem auto-update silencioso;
- sem MSIX/assinatura Authenticode nesta sprint;
- seguranca permanece read-only;
- restore overwrite permanece bloqueado.

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
