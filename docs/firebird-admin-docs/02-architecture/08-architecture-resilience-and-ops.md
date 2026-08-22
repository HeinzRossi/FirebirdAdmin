# Adendo — Segurança de Credenciais, Resiliência, Testes Multi-versão, Empacotamento e Versionamento de Schema

> **Status:** Proposto
> **Relaciona-se com:** `02-architecture/cross-cutting.md`, `02-architecture/monitoring-profiler.md`, `02-architecture/firebird-integration.md`, `02-architecture/persistence.md`, `06-planning/sprints.md`, `07-testing/*`
> **Objetivo:** preencher lacunas identificadas na revisão do baseline v1 antes do início do Sprint 0/1, evitando decisões estruturais tardias e caras de reverter.

---

## 1. Segurança de credenciais — mecanismo concreto

O baseline define *que* a senha não é gravada em texto claro, mas não *como*. Isso precisa estar fechado antes do Sprint 1 (Credential Store depende disso).

### 1.1 Mecanismo de armazenamento

- Usar **Windows Data Protection API (DPAPI)** via `ProtectedData.Protect`/`Unprotect`, com escopo `CurrentUser`.
- Não usar `SecureString` — está obsoleto no .NET moderno e não deve ser adotado em código novo.
- Alternativa a `SecureString` para reduzir exposição em memória: manter a senha como `byte[]` protegido, decifrar somente no momento do uso (abertura de conexão / execução de tool), e zerar o buffer (`Array.Clear`) logo após o uso, em bloco `finally`.
- Persistir no SQLite apenas o **blob protegido pelo DPAPI**, nunca o valor em claro nem um blob reversível fora do escopo da máquina/usuário.

### 1.2 Export/import de perfis de conexão

- Perfis exportados (para backup ou migração de máquina) **nunca** incluem a credencial protegida — o blob DPAPI é vinculado à máquina/usuário e não é portável.
- Export contém apenas: host, porta, caminho/alias do banco, opções de conexão, preferências de toolset. Campo de senha fica vazio no arquivo exportado, exigindo nova digitação no import.
- Import detecta blob incompatível (ex.: perfil de outra máquina) e degrada graciosamente pedindo a senha novamente, em vez de falhar silenciosamente ou expor erro de descriptografia.

### 1.3 Regra adicional

- Nenhuma credencial, mesmo protegida, deve trafegar para logs, exceções ou eventos de diagnóstico. Sanitização de logs (já prevista) deve ter teste automatizado que garanta isso (ver seção 6).

---

## 2. Trace/fbtracemgr — parser e normalizer por versão

Hoje tratado genericamente como "parser + normalizer" e relegado a "Próximos artefatos". Este é provavelmente o componente de maior risco técnico do projeto, porque:

- O formato de saída textual do trace **muda entre 2.5, 3.x, 4.x e 5.x** (campos adicionados, formatação de timestamps, presença/ausência de plano de execução, etc.).
- A sintaxe de configuração da sessão de trace (arquivo `.fbtrace.conf` / parâmetros de `fbtracemgr`) também varia.

### 2.1 O que precisa existir antes do Sprint 4

- Um documento próprio (`02-architecture/trace-parser-strategy.md`) com **amostras reais** de output de trace capturadas em cada versão suportada (2.5, 3.x, 4.x, 5.x), não apenas descrição textual.
- Especificação de qual subconjunto de eventos é comum a todas as versões (baseline seguro) e quais são específicos de versão (tratados via `TraceStrategy` por capability, conforme já previsto na arquitetura).
- Estratégia de parsing resiliente a formato inesperado: uma linha não reconhecida não deve interromper a sessão de captura — deve gerar um evento de "linha não normalizada" preservado no log técnico, nunca descartado silenciosamente.

### 2.2 Testes obrigatórios

- Suite de testes de parser baseada em fixtures de texto real por versão (golden files), não apenas mocks sintéticos.
- Teste explícito de degradação: trace com uma linha corrompida/truncada não deve travar o pipeline nem perder os eventos subsequentes.

---

## 3. Reconexão e resiliência de sessão

Não abordado no baseline atual. Cenários que precisam de resposta arquitetural definida:

### 3.1 Cenários

| Cenário | Comportamento esperado |
|---|---|
| Servidor Firebird reinicia durante polling MON$ | Detectar falha de conexão, tentar reconexão com backoff exponencial, UI muda para estado `Disconnected` (já previsto como estado de UI, mas sem gatilho definido) |
| Servidor reinicia durante sessão de Trace ativa | Trace sessions **não sobrevivem a restart do servidor** — a sessão deve ser detectada como perdida e o usuário notificado explicitamente, com opção de reiniciar a captura, nunca falha silenciosa |
| Rede instável (timeouts intermitentes) | Distinguir erro transitório de erro definitivo antes de aplicar backoff; não tratar todo timeout como desconexão completa |
| Reconexão bem-sucedida após queda | Snapshots/eventos perdidos durante o gap não são reconstruídos retroativamente — o histórico SQLite mostra uma lacuna explícita, não uma interpolação |

### 3.2 Regras

- Política de backoff (ex.: 1s, 2s, 5s, 10s, capado) deve ser configurável, seguindo o mesmo espírito de "Polling configurável + adaptativo" já adotado para monitoramento.
- Reconexão automática nunca deve mascarar o problema: o Status Bar e o Dashboard devem refletir claramente `Disconnected` → `Reconnecting` → `Connected`, com timestamp da última falha.
- Operações de manutenção (Backup/Restore/Validation/Sweep) em andamento durante uma queda de conexão **não** são reiniciadas automaticamente — exigem revisão manual do usuário no retorno, dado o princípio de segurança-primeiro já adotado (ADR-008).

---

## 4. Ambiente de testes de integração multi-versão

O baseline exige testes reais contra Firebird 2.5/3.x/4.x/5.x, mas concentra a validação formal apenas no Sprint 10 (Multi-version Hardening). Isso é um risco de cronograma: problemas de compatibilidade descobertos só no fim do projeto são caros de corrigir.

### 4.1 Infraestrutura proposta

- Containers Docker por versão (imagens oficiais ou mantidas pela comunidade Firebird), orquestrados via `docker-compose`, um serviço por versão suportada.
- Pipeline de CI capaz de subir os 4 containers e rodar a suíte de `IntegrationTests` contra cada um, com resultado por versão (não um único "passou/falhou" agregado).

### 4.2 Mudança de processo — validar incrementalmente, não só no final

- Cada strategy versionada (Monitoring, Trace, Metadata, Maintenance) deve rodar sua suíte de integração **na sprint em que é implementada**, contra as 4 versões, não esperar o Sprint 10.
- O Sprint 10 passa a ser dedicado a fechar exceções e casos-limite remanescentes, não a fazer a primeira validação real do sistema inteiro.
- A `07-testing/firebird-version-matrix.md` deve ser atualizada incrementalmente a cada sprint (não apenas no final), evitando que "A validar" fique acumulado até o Sprint 10.

---

## 5. Escopo do Sprint 9 — Segurança (Users/Roles/Grants)

Atualmente é o único item do plano sem escopo funcional definido, o que é uma inconsistência frente ao restante do backlog (todos os outros sprints têm objetivo e tasks claras).

### 5.1 Proposta de escopo para o primeiro release

Alinhado ao princípio "read-only primeiro" do ADR-008:

- **Incluído no MVP:** visualização de usuários, roles e grants existentes (read-only), navegável a partir do Metadata Explorer e com deep-link a partir de objetos relacionados.
- **Fora do MVP:** criação, alteração ou remoção de usuários/roles/grants. Essas operações ficam para um release posterior, exigindo preflight e confirmação reforçada equivalentes às operações de Manutenção.

### 5.2 Justificativa

Operações de segurança administrativa (CREATE/ALTER/DROP USER, GRANT/REVOKE) têm risco operacional comparável a Backup/Restore, mas o baseline atual não define preflight/confirmation para elas. Até essa especificação existir, o escopo seguro é read-only.

---

## 6. Metas de performance quantificadas

O baseline usa apenas linguagem qualitativa ("sem travar", "sem congelar", "UI responsiva"). Isso dificulta decisões de design de buffer/batch desde o Sprint 2. Propõe-se metas iniciais (ajustáveis após medição real, mas necessárias como ponto de partida):

| Métrica | Meta inicial proposta |
|---|---|
| Taxa de eventos de Trace sustentada sem drop | ≥ 500 eventos/s em carga normal, com degradação graciosa (não perda) acima disso |
| Latência polling → atualização de UI | ≤ 1s no preset Normal, sob carga típica |
| Tamanho da janela de eventos em memória (Profiler Grid) | Configurável, default proposto: últimos 5.000 eventos ou 10 minutos, o que ocorrer primeiro |
| Tempo de resposta da UI durante persistência em batch | Sem bloqueio perceptível (> 100ms de freeze é considerado falha) |
| Filtros sobre histórico SQLite (janelas de até 30 dias) | ≤ 2s para retornar primeira página de resultados |

Essas metas alimentam diretamente os "Cenários obrigatórios" já listados em `07-testing/test-strategy.md` (Performance), dando a eles critérios de aceite mensuráveis em vez de apenas qualitativos.

---

## 7. Empacotamento, assinatura e atualização

O backlog menciona apenas "packaging" no Sprint 12, sem decisão. Como é uma ferramenta administrativa que deve evoluir com frequência (novas capabilities, correções de parser de trace, etc.), a estratégia de distribuição precisa estar definida bem antes do Sprint 12.

### 7.1 Decisões necessárias

- **Formato de instalador:** avaliar MSIX (integração nativa com Windows, atualização facilitada) vs. instalador tradicional (MSI/Squirrel) — MSIX é o caminho recomendado por padrão para apps WPF modernos, salvo restrição de ambiente corporativo (ex.: cartórios com políticas de instalação restritivas que não suportam MSIX).
- **Assinatura de código:** certificado de assinatura Authenticode é necessário para evitar bloqueios de SmartScreen/Defender em ambiente corporativo — isso deve ser adquirido com antecedência, não deixado para o Sprint 12.
- **Mecanismo de atualização:** definir se haverá auto-update (ex.: verificação de nova versão no startup, com changelog visível) ou distribuição manual controlada pela equipe de TI do cartório. Dado o contexto de uso (ferramenta administrativa sensível), atualização manual/controlada é provavelmente mais adequada que auto-update silencioso.

---

## 8. Versionamento de schema SQLite entre releases do aplicativo

O backlog cita "Schema versionado" (S5-T01) apenas como resultado esperado, sem estratégia. Isso é diferente de lidar com diferenças entre versões do **Firebird monitorado** — aqui trata-se do schema do **próprio SQLite local** evoluindo entre releases do Firebird Admin.

### 8.1 Estratégia proposta

- Migrations do EF Core versionadas e aplicadas automaticamente no startup, com log explícito de qual migration foi aplicada.
- Backup automático do arquivo SQLite local antes de aplicar uma migration, com retenção curta (ex.: últimas 3 versões), permitindo rollback manual em caso de falha de migration.
- Migrations que alteram ou removem colunas usadas por dados históricos (ex.: `TraceEvent`, `PerformanceSnapshot`) precisam de plano explícito de compatibilidade — dados antigos não podem se tornar ilegíveis silenciosamente após um update do aplicativo.
- Falha ao aplicar migration bloqueia o startup com mensagem clara, em vez de permitir que o app rode contra um schema inconsistente.

---

## Resumo de impacto no planejamento

| Item | Sprint afetado | Ação recomendada |
|---|---|---|
| Credenciais (DPAPI) | Sprint 1 | Fechar decisão antes de S1-T02 |
| Trace parser/normalizer | Sprint 4 | Levantar fixtures reais antes de S4-T02 |
| Reconexão/resiliência | Sprint 2 e 4 | Incluir nas tasks de Polling Engine e Trace Strategy |
| Testes multi-versão incrementais | Todos, não só Sprint 10 | Rodar suíte de integração a cada strategy implementada |
| Escopo Sprint 9 (Segurança) | Sprint 9 | Definir como read-only no MVP |
| Metas de performance | Sprint 2, 4, 5, 11 | Usar como critério de aceite desde já |
| Empacotamento | Antes do Sprint 12 | Decidir formato/assinatura com antecedência, não no sprint final |
| Versionamento de schema SQLite | Sprint 5 | Detalhar estratégia de migration em S5-T01 |
