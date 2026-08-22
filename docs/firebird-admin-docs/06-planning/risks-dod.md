# Riscos e Definition of Done

## Riscos

| Risco | Impacto | Mitigação |
|---|---|---|
| Diferenças Firebird 2.5–5.x | Alto | Capabilities + strategies + integração real |
| Trace de alto volume | Alto | Channels, batching, UI window |
| Crescimento do SQLite | Médio | Retention híbrida |
| Toolset incompatível | Alto | Discovery + validação + fallback |
| Vazamento de credenciais | Alto | Masking e testes |
| UI congelar | Alto | Async, virtualização, batching |
| Falso positivo diagnóstico | Médio | Evidência + thresholds configuráveis |
| Acoplamento WPF UI | Médio | Design System + boundaries |

## Definition of Done

Uma Task está concluída quando, conforme aplicável:

- compila sem warnings críticos;
- testes unitários passam;
- integração multi-versão relevante passa;
- UI não acessa Infrastructure diretamente;
- Loading/Empty/Error/Disconnected tratados;
- strings visíveis usam resources;
- não há segredos em logs/persistência;
- keyboard/foco validados;
- performance da feature validada;
- ADR/documentação atualizados.
