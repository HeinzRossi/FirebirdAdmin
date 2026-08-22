# Estrutura da Solução

```text
src/
├── FirebirdAdmin.Domain/
├── FirebirdAdmin.Application/
├── FirebirdAdmin.Infrastructure/
├── FirebirdAdmin.Presentation.Wpf/
└── FirebirdAdmin.Bootstrapper/

tests/
├── FirebirdAdmin.Domain.Tests/
├── FirebirdAdmin.Application.Tests/
├── FirebirdAdmin.Infrastructure.Tests/
├── FirebirdAdmin.IntegrationTests/
├── FirebirdAdmin.Tooling.Tests/
└── FirebirdAdmin.Ui.Tests/
```

## Organização por feature

Exemplo em Application:

```text
Application/
├── Connections/
├── Monitoring/
├── Profiling/
├── Diagnostics/
├── Metadata/
├── Maintenance/
├── Security/
└── History/
```

## Restrições

- Domain não referencia Application/Infrastructure/Presentation.
- Application não referencia WPF.
- Presentation não usa `FbConnection`, `DbContext`, `SqliteConnection`, Dapper ou `Process` diretamente.
- Infrastructure não define regras de UX.
