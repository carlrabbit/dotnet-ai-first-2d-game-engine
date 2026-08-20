# Application Shell and Client Contract

## Authority

Authoritative for M037 application lifecycle, player/diagnostics composition, screens, navigation, destructive-transition confirmation, and world replacement.

## Composition

```text
shared application foundation
├── player client
└── diagnostics client
```

Player composition does not depend on diagnostics-only UI, fault injection, raw engineering artifacts, or internal terminology.

## States

```text
starting
main-menu
loading-world
playing
paused/menu-overlay
saving
unloading-world
shutting-down
failed-recoverable
```

Transitions are explicit and headless-testable.

## Player screens

Main menu: Continue, New Game, Load Game, Tutorial, Options, Credits, Quit.

Pause menu: Resume, Save, Load, Options, Return to Main Menu, Quit.

No Statistics item.

## Destructive transitions

When a world is active, load/return-to-menu/quit always asks save-and-continue, continue-without-saving, or cancel. No dirty-state shortcut removes this question.

## Continue

Resolve newest valid save by canonical saved-at metadata and stable tie-break. Skip invalid candidates and present fallback/recovery notice.

## Errors

Player errors describe actionable application/game concepts. Diagnostics may expose deeper IDs/artifacts separately.

## World replacement

```text
confirm
-> optional save
-> stop advancement/input ownership
-> detach projections/callbacks
-> dispose old world/native resources
-> load/validate candidate
-> transfer ownership atomically
-> rebuild player projections/screens
```

Failure leaves a coherent recoverable state.
