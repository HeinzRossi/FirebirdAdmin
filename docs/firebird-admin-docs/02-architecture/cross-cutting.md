# Segurança, Logging e Localização

## Credenciais

- senha não é gravada em texto claro;
- usuário decide se deseja lembrar;
- credencial persistida usa armazenamento seguro do Windows;
- sem persistência: somente memória;
- logs e outputs são sanitizados.

## Logging

- `Microsoft.Extensions.Logging`;
- Serilog como provider;
- rolling files;
- logs estruturados;
- correlação por SessionId/OperationId/TraceSessionId;
- tela interna de logs.

Firebird Trace e Application Logs são conceitos distintos.

## Localização

- pt-BR no primeiro release;
- strings visíveis sempre via resources;
- RuleIds e IDs internos não são traduzidos;
- arquitetura preparada para idiomas adicionais.
