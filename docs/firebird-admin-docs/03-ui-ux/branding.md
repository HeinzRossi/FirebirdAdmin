# Identidade Visual — Logo e Ícone

## Status
**Aprovado para o baseline visual do projeto.**

## Ativos oficiais

| Ativo | Arquivo | Uso |
|---|---|---|
| Logo | `FirebirdAdmin_Logo.png` | Splash, About, documentação e comunicação |
| Ícone master | `FirebirdAdmin_Icon_1024.png` | Fonte raster de alta resolução |
| Ícone Windows | `FirebirdAdmin.ico` | Executável, janela e distribuição |

## Logo
![Logo Firebird Admin](../../../src/FirebirdAdmin.Presentation.Wpf/Assets/Branding/FirebirdAdmin_Logo.png)

## Ícone
![Ícone Firebird Admin](../../../src/FirebirdAdmin.Presentation.Wpf/Assets/Branding/FirebirdAdmin_Icon_1024.png)

## Conceito
A identidade combina a fênix/Firebird, o banco de dados e o escudo de segurança. Isso representa diretamente os três pilares do produto: ecossistema Firebird, administração de dados e operação segura.

## Diretrizes
- Não distorcer as proporções.
- Não alterar cores, tipografia ou composição sem revisão do Design System.
- Usar a logo completa em superfícies institucionais.
- Usar o símbolo em superfícies compactas.
- Usar `FirebirdAdmin.ico` no executável e nas janelas WPF.
- Manter os assets centralizados e versionados.
- Validar legibilidade do ícone nos tamanhos pequenos do Windows.

## Estrutura WPF recomendada

```text
FirebirdAdmin.Presentation.Wpf/
└── Assets/
    └── Branding/
        ├── FirebirdAdmin.ico
        ├── FirebirdAdmin_Icon_1024.png
        └── FirebirdAdmin_Logo.png
```

## Configuração do executável

```xml
<PropertyGroup>
  <ApplicationIcon>..\FirebirdAdmin.Presentation.Wpf\Assets\Branding\FirebirdAdmin.ico</ApplicationIcon>
</PropertyGroup>
```

## Aplicação

| Superfície | Ativo |
|---|---|
| Executável | `FirebirdAdmin.ico` |
| Janela principal | `FirebirdAdmin.ico` |
| Splash | `FirebirdAdmin_Logo.png` |
| About | `FirebirdAdmin_Logo.png` |
| Navigation compacta | símbolo |
| Documentação | logo |
| Installer | `.ico` / master conforme necessidade |

## Acessibilidade
Quando a marca for decorativa e o nome do produto já estiver visível, não deve gerar informação redundante para tecnologias assistivas. Quando o símbolo for o único identificador, deve existir nome acessível textual.

## UI-BRAND-001
**Descrição:** integrar a identidade visual oficial ao projeto.

**Resultado esperado:** executável, Shell, Splash/About e documentação utilizam os ativos oficiais.

**Critérios de aceite:**
- `.ico` configurado no WPF;
- logo sem deformação;
- assets em `Assets/Branding`;
- nenhum caminho absoluto;
- validação em Light/Dark;
- validação do ícone em tamanhos pequenos;
- documentação usa os assets oficiais.

**Dependências:** Sprint 0 — Design System e Shell.  
**Prioridade:** Alta.
