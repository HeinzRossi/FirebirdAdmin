# Release Checklist

## Pré-requisitos

- Windows com .NET SDK 10 instalado para gerar o pacote.
- Nenhum segredo no repositório ou nos scripts.
- Docker e Firebird real são opcionais; o fluxo padrão deve passar sem ambos.

## Validação padrão

```powershell
dotnet restore FirebirdAdmin.sln
dotnet build FirebirdAdmin.sln --no-restore
dotnet test FirebirdAdmin.sln --no-build
.\scripts\release-publish.ps1
.\scripts\release-smoke.ps1
```

Critérios:

- build com 0 warnings;
- testes sem exigir Firebird real, Docker ou credenciais;
- pacote criado em `artifacts\releases`;
- smoke inicia o executável publicado e confirma criação/uso de logs em `%LocalAppData%\FirebirdAdmin\Logs`.

## Smoke manual WPF

- Executar `FirebirdAdmin.Bootstrapper.exe` do pacote extraído.
- Navegar por `Ctrl+1` a `Ctrl+9`.
- Abrir Dashboard, Monitoramento, SQL Profiler, Histórico, Diagnóstico, Metadata, Segurança, Manutenção e Configurações.
- Confirmar que o app abre sem conexão Firebird.
- Confirmar que Configurações permite preencher perfil sem gravar senha em log.
- Confirmar que Segurança é read-only e restore overwrite segue bloqueado.

## Matriz Firebird opcional

```powershell
.\scripts\firebird-matrix-up.ps1
.\scripts\firebird-matrix-test.ps1
.\scripts\firebird-matrix-down.ps1
```

Também é possível apontar a suíte de integração para servidores existentes usando `FIREBIRDADMIN_FB25_*`, `FIREBIRDADMIN_FB30_*`, `FIREBIRDADMIN_FB40_*` e `FIREBIRDADMIN_FB50_*`.

## Distribuição

- Artefato MVP: `.zip` self-contained `win-x64`.
- Instalação: extrair o `.zip` em pasta local e executar `FirebirdAdmin.Bootstrapper.exe`.
- Dados locais ficam em `%LocalAppData%\FirebirdAdmin`.
- SQLite local recebe migrations no startup e cria backup em `%LocalAppData%\FirebirdAdmin\Backups` antes de alterações de schema.

## Pós-MVP

- MSIX ou instalador corporativo deve ser decidido com base no ambiente de distribuição.
- Assinatura Authenticode exige certificado válido antes de distribuição ampla.
- Auto-update silencioso fica fora do MVP; atualização deve ser manual/controlada.
