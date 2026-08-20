# Software-Defined Input Binding Contract

## Authority

Authoritative for M037 explicitly registered bindable actions, default/user layers, contexts, conflict handling, recoverability, and binding-editor behavior. Existing input-action-map authority remains authoritative for semantic action types and backend-neutral physical controls.

## Registry

Actions are explicitly registered. Assembly scanning and arbitrary unknown IDs are prohibited.

Definition fields: action ID, display/localization key, category, description, action type, allowed input classes, default bindings, rebindable flag, multiple-binding policy, conflict policy, context, recoverable-action marker.

Duplicate incompatible registration fails.

## Layers

```text
registered defaults
+ validated user overrides
-> effective bindings
```

A corrupt override falls back only for that action.

## Contexts

```text
global
menu
gameplay
map-tool
text-entry
diagnostics
```

Priority: modal -> focused text -> active tool -> current screen -> gameplay -> global.

## Binding editor

Supports grouped action list, current/default display, key or supported mouse capture, conventional modifiers, conflict explanation, replace/cancel/retain where allowed, remove override, reset one, reset all.

## Recoverability

Cancel/back, confirm, options/input access, safe mode, and reset-bindings remain recoverable through defaults or pointer UI.

## Replay

Bindings affect physical-to-semantic mapping only. Semantic recording/replay remains action-ID/value based.

## RDP

Submitted-text/RDP input remains optional client-specific infrastructure and is not required for player gameplay.

## Exclusions

No macros, arbitrary scripts, multistep sequences, cloud profiles, per-save bindings, or mandatory gamepad expansion.
