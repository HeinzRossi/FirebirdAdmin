# Manutenção — Backup / Restore / Validation / Sweep

## Fluxo padrão

```text
Configurar
→ Validar
→ Revisar
→ Confirmar
→ Executar
→ Acompanhar
→ Resultado
→ Histórico
```

## Backup

- destino sugerido e editável;
- preflight;
- toolset visível;
- execução assíncrona;
- background;
- cancelamento seguro;
- resultado e histórico.

## Restore

Restore para **novo banco** é o padrão.

Overwrite exige confirmação reforçada, incluindo identificação explícita do destino.

## Validation

- opções conforme versão/toolset;
- aviso de carga;
- saída estruturada;
- saída original preservada.

## Sweep

Antes da operação mostrar contexto disponível, como OIT/OAT/Next e conexões ativas.

## Regra de concorrência

No primeiro release: uma operação administrativa pesada por banco/sessão de cada vez.

## Progresso

Percentual somente quando confiável. Caso contrário, stage + elapsed + progresso indeterminado.
