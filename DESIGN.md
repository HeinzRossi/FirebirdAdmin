---
name: Firebird Admin
description: Product UI design system for a dense, calm Firebird database administration workspace.
colors:
  neutral-0: "#FCFCFD"
  neutral-50: "#F7F8FA"
  neutral-100: "#E9ECF1"
  neutral-500: "#667085"
  neutral-700: "#344054"
  neutral-900: "#101828"
  accent-600: "#0F766E"
  accent-700: "#115E59"
  success-600: "#15803D"
  warning-600: "#A16207"
  error-600: "#B42318"
  info-600: "#2563EB"
  focus-500: "#0EA5A4"
typography:
  display:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "28px"
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: "normal"
  headline:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "20px"
    fontWeight: 600
    lineHeight: 1.25
    letterSpacing: "normal"
  title:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "18px"
    fontWeight: 600
    lineHeight: 1.25
    letterSpacing: "normal"
  body:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.45
    letterSpacing: "normal"
  label:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "12px"
    fontWeight: 600
    lineHeight: 1.35
    letterSpacing: "normal"
rounded:
  sm: "4px"
  md: "6px"
  lg: "8px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "24px"
  xxl: "32px"
components:
  button-default:
    backgroundColor: "{colors.neutral-0}"
    textColor: "{colors.neutral-900}"
    rounded: "{rounded.sm}"
    padding: "5px 12px"
    height: "30px"
  input-default:
    backgroundColor: "{colors.neutral-0}"
    textColor: "{colors.neutral-900}"
    rounded: "{rounded.sm}"
    padding: "4px 7px"
    height: "30px"
  badge-neutral:
    backgroundColor: "{colors.neutral-50}"
    textColor: "{colors.neutral-900}"
    rounded: "{rounded.sm}"
    padding: "5px 10px"
  navigation-panel:
    backgroundColor: "{colors.neutral-0}"
    textColor: "{colors.neutral-900}"
    width: "248px"
  status-bar:
    backgroundColor: "{colors.neutral-0}"
    textColor: "{colors.neutral-500}"
    height: "32px"
---

# Design System: Firebird Admin

## 1. Overview

**Creative North Star: "Painel de Controle"**

Firebird Admin is a dense and calm product interface for database administration. It should feel like a trustworthy control panel: measured, legible, and ready for repeated technical work without theatrical visual noise.

The system favors predictable navigation, compact controls, virtualized data grids, and restrained use of color. The visual job is to help a DBA read state, inspect evidence, and act carefully. Decoration is subordinate to operational clarity.

Formal `PRODUCT.md` context has not been created yet. Until it exists, preserve the current product posture: task-first, Portuguese-first, Windows-native, and safe by default.

**Key Characteristics:**
- Dense operational surfaces with clear scan paths.
- Segoe UI throughout, with no display-font flourish.
- Cool tinted neutrals with a rare teal accent for focus and primary state.
- Tables, badges, forms, and status bars over marketing-style composition.
- State clarity before visual novelty.

## 2. Colors

The palette is a restrained control-room palette: cool neutrals carry nearly all surfaces, while teal appears only for focus, selection, and purposeful emphasis.

### Primary
- **Operational Teal** (`accent-600`): the main accent for active state, selection, and primary affordances.
- **Deep Operational Teal** (`accent-700`): stronger accent for pressed or high-emphasis states.
- **Focus Teal** (`focus-500`): keyboard focus ring and accessibility-visible interaction feedback.

### Secondary
- **Success Green** (`success-600`): successful or healthy states only.
- **Warning Amber** (`warning-600`): caution states and preflight warnings.
- **Error Red** (`error-600`): failed, blocked, destructive, or unsafe states.
- **Info Blue** (`info-600`): neutral information and non-blocking diagnostics.

### Neutral
- **Workspace White** (`neutral-0`): raised panels, navigation, status bar, and input surfaces.
- **Application Mist** (`neutral-50`): app background and neutral badges.
- **Subtle Border** (`neutral-100`): dividers, table borders, field borders, and quiet separation.
- **Muted Text** (`neutral-500`): helper copy, status bar copy, labels, and secondary metadata.
- **Panel Text** (`neutral-700`): medium emphasis text when needed.
- **Primary Ink** (`neutral-900`): titles, body text, and critical labels.

### Named Rules

**The Ten Percent Accent Rule.** Teal is rare. If teal starts decorating the screen, the interface has stopped being a control panel.

**The Semantic Color Rule.** Green, amber, red, and blue are states, not decoration. Do not use them for visual variety.

## 3. Typography

**Display Font:** Segoe UI, system-ui, sans-serif  
**Body Font:** Segoe UI, system-ui, sans-serif  
**Label/Mono Font:** Segoe UI, system-ui, sans-serif

**Character:** Native Windows, calm, and task-focused. The type system is intentionally narrow because this is a product surface, not a campaign.

### Hierarchy
- **Display** (600, 28px, 1.2): rare page-level titles and documentation-scale headings.
- **Headline** (600, 20px, 1.25): workspace titles, empty-state titles, and major panel headers.
- **Title** (600, 18px, 1.25): app title and compact high-emphasis labels.
- **Body** (400, 14px, 1.45): forms, grid-adjacent text, details, and readable messages.
- **Label** (600, 12px, 1.35): badges, navigation section labels, status bar, compact metadata.

### Named Rules

**The Native Tool Rule.** Use Segoe UI everywhere. Do not introduce display fonts, brand fonts, or decorative type into product controls.

**The Fixed Scale Rule.** Do not use viewport-scaled typography. Product UI must remain stable across DPI and workspace changes.

## 4. Elevation

Firebird Admin is flat by default. Depth comes from tonal layering, borders, and layout structure rather than heavy shadows. The legacy card shadow exists as a low ambient token, but workspace-level surfaces should remain unframed unless a repeated item or detail panel needs containment.

### Shadow Vocabulary
- **Card Ambient** (`BlurRadius 18, ShadowDepth 2, Opacity 0.08`): reserved for isolated repeated cards or transient surfaces. Do not apply it to the whole workspace.

### Named Rules

**The Flat Workspace Rule.** A workspace is not a card. Use full-page structure for work areas and reserve cards for repeated metrics, details, and modals.

## 5. Components

### Buttons
- **Shape:** gently squared product corners (`4px` focus radius, default system control contour).
- **Default:** neutral surface on primary ink, 1px subtle border, 30px minimum height, `5px 12px` padding.
- **Hover / Focus:** focus is a visible teal 2px ring. Hover must stay restrained and must not use saturation for inactive controls.
- **Primary:** use the same shape, with teal only when the action is the clear primary action.

### Badges
- **Style:** neutral background, subtle border, 4px radius, 12px semi-bold label text.
- **Role:** compact status and counters, not decorative chips.
- **State:** semantic badges may use success/warning/error/info color only when the state itself requires it.

### Cards / Containers
- **Corner Style:** 6px to 8px for metric cards, detail panels, and repeated items.
- **Background:** neutral-0 or transparent depending on containment need.
- **Shadow Strategy:** flat by default. Ambient shadow is exceptional.
- **Border:** 1px subtle border for contained repeated items and details.
- **Internal Padding:** 12px, 16px, or 24px from the spacing scale.

### Inputs / Fields
- **Style:** neutral surface, subtle 1px border, 30px minimum height, compact padding.
- **Focus:** teal 2px ring, visible outside the control.
- **Error / Disabled:** use semantic tokens for error; disabled must be visibly muted and non-interactive.

### Navigation
- **Style:** fixed 248px sidebar, neutral raised surface, subtle right divider.
- **Typography:** 12px section title, 14px semi-bold navigation items.
- **State:** selected item should be obvious by position, weight, and restrained accent. Do not turn the sidebar into a color rail.

### Data Grids
- **Style:** dense rows, 28px row height, 30px column header height.
- **Performance:** row and column virtualization are required.
- **Structure:** horizontal grid lines only. Avoid heavy cell boxing.
- **Use:** primary inspection surface for monitoring, profiler, history, alerts, metadata, security, and maintenance.

### Status, Empty, Loading, and Error States
- **Status:** compact badges and status bar text, never oversized hero metrics.
- **Empty:** explain the next action in one short sentence.
- **Loading:** prefer inline state text and skeleton-like structure over centered spinners.
- **Error:** show the cause and a recovery path. Do not expose secrets or raw command lines.

## 6. Do's and Don'ts

### Do:
- **Do** keep product screens dense, calm, and predictable.
- **Do** use the spacing scale: 4, 8, 12, 16, 24, 32.
- **Do** keep teal rare and functional.
- **Do** preserve DataGrid virtualization whenever grids can grow.
- **Do** use visible keyboard focus on every interactive control.
- **Do** prefer full-width workspace structure over floating page cards.
- **Do** keep copy Portuguese-first until a localization plan says otherwise.

### Don't:
- **Don't** use hero metrics, marketing layouts, or landing-page composition inside the app.
- **Don't** use glassmorphism, gradient text, decorative gradients, or blurred cards.
- **Don't** use colored side stripes as status indicators.
- **Don't** nest cards inside cards.
- **Don't** introduce display fonts or decorative icons into operational UI.
- **Don't** use semantic colors for decoration.
- **Don't** hide dangerous or administrative state behind vague copy.
