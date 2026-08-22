# Arquitetura UI e Design System

## Stack

- WPF;
- WPF UI;
- CommunityToolkit.Mvvm;
- ScottPlot;
- Design System próprio.

## Estrutura

```text
Shared/DesignSystem/
├── Tokens/
│   ├── Colors.xaml
│   ├── Brushes.xaml
│   ├── Typography.xaml
│   ├── Spacing.xaml
│   ├── Sizes.xaml
│   ├── Radius.xaml
│   └── Shadows.xaml
├── Themes/
│   ├── Light.xaml
│   └── Dark.xaml
├── Styles/
│   ├── Buttons.xaml
│   ├── Labels.xaml
│   ├── TextBoxes.xaml
│   ├── ComboBoxes.xaml
│   ├── DataGrids.xaml
│   ├── Navigation.xaml
│   ├── Cards.xaml
│   ├── Badges.xaml
│   └── Dialogs.xaml
└── Components/
```

## Regras

- estilos de Buttons, Labels, Grids etc. permanecem separados;
- sem cores/fontes arbitrárias nas Views;
- estilos são semânticos;
- Light/Dark alteram tokens;
- Comfortable é a densidade padrão;
- Compact é opcional;
- `DesignSystemGallery` valida controles e estados.

## Componentes reutilizáveis

`PageHeader`, `MetricCard`, `StatusBadge`, `SeverityBadge`, `FilterBar`, `PropertyPanel`, `CodeViewer`, `EmptyState`, `ErrorState`, `LoadingState`, `ConfirmationDialog`, `OperationProgress`.

## Estados obrigatórios

`Loading`, `Empty`, `Ready`, `Error`, `Disconnected`, `Stale`, quando aplicáveis.
