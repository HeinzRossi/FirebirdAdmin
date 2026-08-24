# Matriz de Compatibilidade Firebird

> Deve ser preenchida e mantida por testes de integração. `A validar` não significa suporte confirmado.

| Recurso | 2.5 | 3.x | 4.x | 5.x |
|---|---|---|---|---|
| Conexão | Integração opcional | Integração opcional | Integração opcional | Integração opcional |
| Capabilities | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy |
| MON$ Attachments | Integração opcional | Integração opcional | Integração opcional | Integração opcional |
| MON$ Transactions | Integração opcional | Integração opcional | Integração opcional | Integração opcional |
| MON$ Statements | Integração opcional | Integração opcional | Integração opcional | Integração opcional |
| Trace config | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy |
| Trace execução | A validar em ambiente real | A validar em ambiente real | A validar em ambiente real | A validar em ambiente real |
| Metadata catalog | Integração opcional | Integração opcional | Integração opcional | Integração opcional |
| Security read-only | Integração opcional | Integração opcional | Integração opcional | Integração opcional |
| Packages | N/A esperado | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy |
| Standalone Functions | N/A esperado | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy |
| Identity Columns | N/A esperado | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy |
| Backup preflight | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy |
| Restore preflight | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy |
| Validation preflight | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy |
| Sweep preflight | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy | Coberto por teste unitário/strategy |

## Estados permitidos

- `Suportado`
- `Parcial`
- `Não suportado`
- `N/A`
- `A validar`
- `Integração opcional`
- `Coberto por teste unitário/strategy`
- `A validar em ambiente real`

Nunca converter uma suposição de versão em suporte confirmado sem teste/documentação.

## Execução local opcional

```powershell
.\scripts\firebird-matrix-up.ps1
.\scripts\firebird-matrix-test.ps1
.\scripts\firebird-matrix-down.ps1
```

O teste padrão da solution não exige Docker nem servidores Firebird reais. Os testes de integração só executam contra versões que tiverem env vars `FIREBIRDADMIN_FB25_*`, `FIREBIRDADMIN_FB30_*`, `FIREBIRDADMIN_FB40_*` ou `FIREBIRDADMIN_FB50_*` configuradas.
