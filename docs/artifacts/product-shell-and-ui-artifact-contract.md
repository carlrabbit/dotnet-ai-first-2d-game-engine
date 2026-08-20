# Product Shell and UI Artifact Contract

## Root

```text
artifacts/application/M037/
```

## Required artifacts

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
platform/linux/structural-report.json
platform/linux/graphical-report.json
platform/windows/structural-report.json
platform/windows/graphical-report.json
review-pack/review-manifest.json
review-pack/evidence-index.json
review-pack/navigation-and-client-separation.md
review-pack/save-and-autosave-flow.md
review-pack/settings-display-and-safe-mode.md
review-pack/input-rebinding.md
review-pack/accessibility-baseline.md
review-pack/graphical-evidence-index.md
review-pack/limitations.md
m037-completion-audit.json
diagnostics.json
```

## Required evidence

Supported resolution/UI-scale structural layout cases; modal/focus/text-entry isolation; player/diagnostics dependency isolation; Continue fallback past invalid saves; editable default save naming; per-WorldId autosave rotation/retention; settings corruption/safe-mode recovery; display timeout/next-start rollback; binding capture/conflicts/reset/fallback; repeated world/menu lifecycle/disposal; Linux/Windows structural proof; graphical shell startup/navigation on both platforms; review pack linked to structural identities.

## Graphical evidence

Human-review evidence includes main menu, New Game, Tutorial entry, manual save naming, load browser, options, display confirmation, input rebinding, and pause/destructive-transition confirmation.

Screenshots/video are review evidence, not semantic authority.

## Completion audit

`m037-completion-audit.json` enumerates applicable M037 obligations and records satisfied/unsatisfied state with final terminal outcome `COMPLETE`, `AWAITING HUMAN REVIEW`, or `BLOCKED`.

## Boundedness/privacy

No unbounded UI event traces, save-catalog logs, or input-capture logs. Do not record usernames, absolute home paths, secrets, or arbitrary user-entered text beyond bounded fixtures.
