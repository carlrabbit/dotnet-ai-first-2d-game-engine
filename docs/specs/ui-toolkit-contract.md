# UI Toolkit Contract

## Authority

Authoritative for the M037 engine-owned retained UI tree, layout, focus, input capture, theme, scaling, lifecycle, and structural projection.

## Purpose

Provide a small game/application UI toolkit comparable in concept to Godot Controls without data binding or editor tooling.

## Tree and controls

```text
UiNode
├── UiControl
├── UiContainer
├── UiScreen
└── UiModal
```

Required controls: label, button, toggle, slider, text field, select/dropdown, list, scroll view, modal/confirmation dialog, separator, busy/progress indicator, and stack/row/column/grid/margin containers.

Nodes expose parent/children, visible/enabled state, lifecycle, minimum/preferred size, and deterministic layout inputs.

## Layout

Containers own child arrangement. Screen roots use bounded anchors/margins or equivalent placement. Equivalent viewport, UI scale, theme, and content produce equivalent structural layout.

## Input and focus

Priority:

```text
modal
-> focused control
-> screen
-> lower application/game contexts
```

Hidden/disabled controls do not activate. Text entry captures printable input and prevents gameplay leakage. Mouse hit testing, keyboard focus traversal, confirm/cancel, pointer activation, clipping, and scrolling are required.

## State

State refresh and callbacks are explicit. Do not add automatic data binding, reflection-driven view models, reactive expression languages, browser layout engines, or arbitrary script evaluation.

## Rendering

Toolkit emits backend-neutral structural/drawing projection. Native adapters translate it. Structural projection is semantic/headless evidence; screenshots are human-review evidence.

## Theme and accessibility baseline

Theme resources define typography roles, spacing, borders, focus, disabled state, selection, and control visuals. UI scale is a user setting and does not alter world geometry.

Baseline: visible focus, pointer-operable primary controls, meaningful icon text/description, non-color-only critical state, UI scaling, reduced animation/flashing hook where applicable.

## Lifecycle

Removing/replacing screens detaches callbacks and releases adapter resources. Repeated replacement remains bounded.

## Cross-platform

Structural layout/focus semantics are platform-neutral. Native text/window/input differences remain adapter concerns and must satisfy this contract on Linux and Windows.
