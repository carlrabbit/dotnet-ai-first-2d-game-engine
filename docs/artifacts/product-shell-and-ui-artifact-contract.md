# Product Shell and UI Artifact Contract

## Root

```text
artifacts/application/M037/
```

## Required artifact families

```text
m037-manifest.json
authority-normalization-report.json
ui-control-catalog.json
ui-layout-cases.json
ui-focus-input-cases.json
application-state-transitions.json
client-dependency-report.json
main-menu-projection.json
pause-menu-projection.json
new-game-cases.json
world-configuration-validation.json
save-catalog.json
save-naming-cases.json
autosave-schedule-cases.json
autosave-retention-cases.json
settings-validation-report.json
display-preview-rollback-report.json
safe-mode-report.json
input-action-registry.json
input-binding-cases.json
input-context-cases.json
world-lifecycle-resource-report.json
current-regression-report.json

platform/<platform>/structural-report.json
platform/<platform>/graphical-report.json

review-pack/
  review-manifest.json
  evidence-index.json
  navigation-and-client-separation.md
  save-and-autosave-flow.md
  settings-display-and-safe-mode.md
  input-rebinding.md
  accessibility-baseline.md
  graphical-evidence-index.md
  limitations.md

m037-completion-audit.json
diagnostics.json
```

## Platform-state semantics

Platform report status must be one of the declared meaningful states, including:

```text
passed
failed
deferred-inactive-platform
not-executed-active-platform
```

An inactive platform may have structural/native reports solely to record deferred state.

Such reports are **not** passing platform evidence.

Portable M037 structural semantics are validated on the active development platform and remain platform-neutral by contract.

The active development platform must have passing native/graphical evidence when required.

## Required evidence for M037 completion

- UI/layout/focus/modal/text-entry structural proof;
- player/diagnostics dependency isolation;
- Continue/save/autosave/settings/input semantics;
- repeated world/menu lifecycle/disposal;
- active-platform structural/native integration;
- active-platform graphical shell startup/navigation;
- explicit inactive-platform debt state;
- review pack linked to structural identities.

M037 does not require inactive-platform graphical execution during the current Windows epoch.

## Graphical evidence

Human-review evidence includes main menu, New Game, Tutorial entry, save naming, load browser, options, display confirmation, input rebinding, and pause/destructive-transition confirmation.

Human review occurs on the active platform unless the milestone explicitly says otherwise.

Screenshots/video are review evidence, not semantic authority.

## Completion audit

`m037-completion-audit.json` enumerates applicable M037 obligations.

It may report `COMPLETE` while inactive-platform obligations remain explicitly deferred, provided:

- all portable obligations pass;
- active-platform native/graphics obligations pass;
- M037 blocking human review is approved;
- deferred obligations are recorded in `eng/platform-verification.json`;
- no other agent-resolvable gap remains.

Terminal outcomes remain:

```text
COMPLETE
AWAITING HUMAN REVIEW
BLOCKED
```

## Boundedness/privacy

No unbounded UI-event traces, save-catalog logs, or input-capture logs.

Do not record usernames, absolute home paths, secrets, or arbitrary user-entered text beyond bounded fixtures.
