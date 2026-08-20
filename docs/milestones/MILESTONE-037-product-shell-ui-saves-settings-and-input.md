# Milestone 037 — Product Shell, UI Foundation, Saves, Settings, and Input

## 1. Goal

Turn the M031–M035 settlement simulation and M036 cross-platform engineering foundation into a coherent player-facing endless-game application shell without changing the settlement gameplay or prematurely implementing later terrain, audiovisual, tutorial-content, statistics, population-departure, or distribution work.

Target flow:

```text
startup
-> safe user-configuration load
-> main menu
-> Continue / New Game / Load Game / Tutorial / Options / Credits
-> player game client
-> pause menu / save / load / options
-> return to menu or quit with explicit save choice
```

Primary acceptance question:

> Can a player on both supported development platforms launch the application, create or continue an endless world, navigate the shell, save/load safely, use rotating autosaves, recover from invalid settings/display changes, and remap explicitly registered controls without encountering diagnostics-only concepts or weakening headless simulation authority?

## 2. Repository role and maturity assumptions

```text
repository role: capability-provider
bounded dogfood: player-facing shell around the endless settlement game
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation
current maturity: implementation-ready, artifact-first
M035: implemented heavy-internal-testing capability
M036: guide-system v0.7.2 and cross-platform engineering implementation merged
execution profile: ai-executed-broad
baseline implementation model: GPT-5.6 Luna
target maturity: player-shell-capable and ready for terrain/map presentation work
```

M036 merge evidence records Windows core/graphics success and a Linux/semantic-comparison gap at merge time. M037 does not rewrite M036 history; M037 must produce its own Linux and Windows product-shell evidence before claiming cross-platform completion.

Two stale M036-era authority statements currently contradict the newer cross-platform project truth: `AGENTS.md` and a lower section of `docs/ENGINEERING.md` still say Windows/PowerShell is unsupported. Normalize them as supporting authority cleanup before feature work relies on them. Do not rewrite the completed M036 milestone.

## 3. Execution mode

`ai-executed-broad`

Implement as six coherent transformations:

1. normalize inherited M036 platform-authority contradictions;
2. add an engine-owned retained UI toolkit and shared application foundation;
3. compose separate player and diagnostics clients and implement product navigation;
4. implement save catalog, naming, Continue, manual save operations, wall-clock autosave scheduling, and configurable retention;
5. implement versioned user settings, display safety, safe mode, and explicitly registered software-defined input bindings with a basic rebinding UI;
6. generate structural/graphical evidence on Linux and Windows, complete blocking human review, run the aggregate verifier and v0.7.2 completion audit.

Focus areas are transformation families, not edit allowlists. Perform supporting repository-local work necessary to satisfy the milestone without adding unrelated product scope.

## 4. Scope

### 4.1 Inherited M036 authority normalization

Before relying on active engineering docs:

- remove the stale native-Windows-unsupported sentence in `AGENTS.md`;
- remove/correct the stale Linux-only baseline statement in `docs/ENGINEERING.md`;
- retain M036's current Linux/Bash + Windows/PowerShell-7 model in README, command contract, validation tiers, and cross-platform launcher policy;
- preserve M036 history unchanged.

M037 validation fails if active project authority remains contradictory.

### 4.2 Shared application foundation

Provide reusable boundaries for process/window lifecycle, client composition, screen navigation, UI hosting, semantic input routing, renderer/audio adapter access, user settings, world-configuration discovery, save catalog/persistence orchestration, world load/unload/replacement, camera integration, and player-safe errors.

### 4.3 Player and diagnostics clients

Locked architecture:

```text
shared application/UI/input/rendering/audio/settings/save foundations
├── player-facing game client
└── engineering diagnostics client
```

Separate executables are preferred. An explicit mode is acceptable initially only if player startup does not require diagnostics-only services/artifacts and dependency direction permits later physical separation without a rewrite.

The existing Raylib debug client may feed or become the diagnostics composition. Its engineering inspection role remains available.

### 4.4 Engine-owned retained UI toolkit

Create a small Godot-like retained tree. Required concepts and behavior are authoritative in `docs/specs/ui-toolkit-contract.md`.

Required controls include label, button, toggle, slider, text field, select, list, scroll view, modal/confirmation dialog, separators, busy indicator, and stack/row/column/grid/margin containers.

State synchronization is explicit, e.g. `screen.Refresh(projection)` and explicit callbacks. No automatic data binding, reflection-driven view models, reactive expression language, browser layout engine, or general editor tooling.

### 4.5 Accessibility baseline

Provide a bounded baseline: visible keyboard focus, pointer-operable primary shell controls, UI scaling, non-color-only critical state, textual description for meaningful icons, reduced animation/flashing hook where applicable, and appropriate camera/edge-scroll preferences. This is not certification.

### 4.6 Main menu

Required entries:

```text
Continue
New Game
Load Game
Tutorial
Options
Credits
Quit
```

No Statistics item and no scenario selector.

`Continue` displays its target when available. Resolve newest valid save by canonical successful save timestamp with stable tie-break; skip corrupt/incompatible candidates and show a recovery/fallback notice.

### 4.7 Pause menu and destructive transitions

Required entries:

```text
Resume
Save
Load
Options
Return to Main Menu
Quit
```

Quick save and quick load are out of scope.

Every load of another save, return to main menu, or quit while a world is active asks every time:

```text
Save and <operation>
<operation> Without Saving
Cancel
```

Do not use dirty-state heuristics to remove this question.

### 4.8 New Game and world configuration

`New Game` creates the same endless sandbox product.

Initial inputs:

```text
world configuration
world seed: random or player-entered
world title: pre-filled editable
Start
```

World configurations are authored documented JSON game resources, not normal user options. Bundled IDs include at least `relaxed`, `standard`, `demanding`, and `stress-test`.

M037 provides no player-facing world-rule editor. The effective selected configuration is retained canonically in the save so later resource edits do not silently change an existing world.

### 4.9 Tutorial entry

`Tutorial` starts a fixed validated seed + fixed standard world configuration + tutorial-guidance-enabled marker. M037 owns entry/persistence only. Full tutorial guidance is deferred. The tutorial remains an endless world and does not introduce objectives, scenarios, campaigns, victory, or completion state.

### 4.10 Save model

A save is the player-visible unit and contains a complete authoritative world snapshot. There is no player-visible world/save hierarchy.

Metadata includes `SaveId`, internal provenance `WorldId`, world title, save title, seed, world-config identity/schema/fingerprint, simulation day/instant, population, created/saved times, manual/autosave type, game/build version, save schema, compatibility/recovery status, and optional lightweight preview metadata.

### 4.11 Manual save naming

Locked default:

```text
<World Title> — Day <Simulation Day>
```

Pre-filled and editable. Renaming changes only the save title, never `WorldId`, world title, seed, configuration, or provenance.

Autosave titles are generated and non-editable:

```text
Autosave — <World Title> — Day <Simulation Day>
```

### 4.12 Save browser

Show save/world title, simulation day/instant, population, saved time, manual/autosave type, version/schema, and valid/recoverable/incompatible/corrupt status.

Operations: load, create manual save, overwrite with confirmation, rename title, delete with confirmation, and inspect compatibility/recovery. Deleting one save deletes only that save and derived preview/cache data. No delete-world operation.

### 4.13 Autosave

User options:

```text
enabled/disabled
wall-clock interval
retained autosave count
```

Default retained count: `5`. Initial UI choices: `1`, `3`, `5`, `10` (a validated bounded integer is acceptable if simpler).

Scheduling:

```text
wall-clock interval expires while world active
-> autosave request pending
-> wait for canonical save preconditions
-> execute normal transactional save
-> reset schedule only after success
```

Use injected monotonic time; tests never sleep. Timer runs while a world is loaded including simulation pause/planning time, stops on main menu/application suspension/no world, and never starts a second save while another is active. Wall time never becomes simulation authority.

Autosaves rotate independently by internal `WorldId`. Reducing retention never deletes manual saves.

### 4.14 Save execution

Use M035 save compatibility/recovery authority. Synchronous save with visible busy/paused presentation is acceptable. Snapshot-based async serialization is allowed only if existing world snapshot authority makes it safe/testable. Serializing a concurrently mutating live world without a safe snapshot contract is prohibited.

### 4.15 User settings

Mutable local application settings, not world authority.

Audio: master/music/effects volume and optional mute. Music content remains deferred.

Display: windowed, borderless-windowed, fullscreen, native/desktop resolution, common resolutions (1280x720, 1600x900, 1920x1080, 2560x1440), UI scale; show only supported host modes.

General/gameplay preferences: autosave enable/interval/retention, pause on focus loss, pause on configured critical event/alert, existing camera/edge-scroll preferences, accessibility-baseline preferences.

### 4.16 Versioned settings and recovery

Require schema version, validation, bounded migration, atomic write, per-setting fallback where safe, rejected-input backup, reset defaults, and startup despite corruption.

Required startup options:

```text
--safe-mode
--reset-user-settings
```

Safe mode uses safe windowed display/resolution, default UI scale/theme/bindings, and optional audio disable. It never modifies saves or world configuration.

### 4.17 Display preview and rollback

Risky display changes are provisional:

```text
record previous known-good
-> persist provisional marker
-> apply candidate
-> 15-second confirmation countdown
-> Keep Changes or Revert
-> automatic revert on timeout, failed apply, or next-start recovery
```

Previous-known-good state survives process termination during preview.

### 4.18 Software-defined bindable input

Extend existing semantic input maps with explicit registration. A bindable action definition has stable action ID, display/localization key, category/description, action type, allowed input classes, defaults, rebindability, multiple-binding policy, conflict policy, context, and recoverability marker.

No assembly scanning, arbitrary unknown IDs, or gameplay dependency on backend key enums.

Required contexts:

```text
global
menu
gameplay
map-tool
text-entry
diagnostics
```

Dispatch priority:

```text
active modal
-> focused text control
-> active tool
-> current screen
-> gameplay
-> global
```

Text entry suppresses printable gameplay actions.

### 4.19 Binding editor

Support grouped action list, current/default bindings, capture mode, keyboard keys, supported mouse buttons, conventional modifiers, conflict explanation, replace/cancel/retain where policy allows, remove override, reset one, reset all.

Out of scope: macros, multi-step sequences, arbitrary scripts, cloud profiles, per-save bindings, mandatory gamepad expansion.

Corrupt overrides fall back per action. Semantic recording/replay remains action-ID/value based.

### 4.20 RDP/workbench policy

Player gameplay does not guarantee the submitted-text/RDP compatibility path built for workbenches. Normal menus are pointer-operable. Existing workbench RDP support remains opt-in/client-specific unless inspection proves shared architectural leakage worth removing.

### 4.21 Credits

Provide a basic credits screen/data boundary for current product/repository credits and already-required third-party notices. Final audiovisual licensing closure remains deferred.

## 5. Non-goals

Do not implement quick save/load, Statistics menu, lifetime statistics, telemetry collection/export, world-rule editor, runtime world-rule mutation, scenario/objective/campaign/victory systems, full tutorial content, procedural generation, terrain transitions, map presentation overhaul, growth/condition presentation, animation overhaul, final asset/audio/music integration, audiovisual legal closure, game-wide RDP compatibility, mandatory gamepad expansion, cloud saves/accounts, packaging/distribution, Windows export, new simulation mechanics, population departure/abandonment, broad M035/M036 readiness reruns, guide migration, TBPs, or issue templates.

## 6. Focus areas

### Focus Area 0 — Normalize inherited platform authority

Correct stale active M036 support statements. Do not change M036 historical docs.

### Focus Area 1 — UI toolkit and shared application foundation

Deliver retained backend-neutral UI, layout/focus/modal/text capture/theme/scaling, structural projection, shared lifecycle, and deterministic cleanup.

Blocking examples: actions fire through UI; hidden controls activate; modal focus leaks; text fields dispatch gameplay shortcuts; layout overflows supported viewports/UI scales; callbacks/native resources leak after screen replacement.

### Focus Area 2 — Player/diagnostics composition and product lifecycle

Deliver isolated player/diagnostics compositions, main/pause menus, Continue, new game, tutorial entry, credits, and explicit save/continue/cancel destructive transitions.

### Focus Area 3 — Save catalog and autosave

Deliver metadata catalog, locked naming, browser operations, internal `WorldId`, wall-clock scheduler, retention default five, and M035 recovery integration.

### Focus Area 4 — Settings, display safety, safe mode, and binding registry

Deliver versioned settings, recovery, display rollback, safe mode, explicit action registry, contexts/conflicts/reset, and pointer-operable binding editor.

### Focus Area 5 — Cross-platform evidence, review, and completion audit

Deliver structural proof on both platforms, graphical product-shell proof on both platforms, one complete human review pack, affected current regressions, M037 resumable suite, blocking review, and v0.7.2 completion audit.

Historical M035/M036 completion evidence is not a mutable current gate.

## 7. Implementation constraints

Use current M031–M036 permanent authority. Do not create another world, persistence format, input runtime, rendering core, audio core, or engineering command system.

UI emits explicit application/game commands and consumes projections; it does not mutate simulation stores directly.

Only graphical adapters/composition may reference raylib-cs/native raylib. UI/settings/save catalog/action registry/structural projections remain headless-testable.

Autosave wall time is application behavior, never simulation authority.

User settings are mutable local config; world configuration is immutable authored content selected at world creation and retained in saves.

Use M036 native launcher model: Linux/Bash, Windows/PowerShell 7, shared semantics in `Agentic2D.Engineering`. Do not create a new forest of mirrored `.sh`/`.ps1` scripts; prefer the generic `suite.sh` / `suite.ps1` surface plus focused test filters.

Repeated world/menu/load transitions must detach callbacks, dispose native resources, and transfer world ownership deterministically.

Update current invariant/regression tests when authority changes. Do not recreate completed milestone reviews or long historical campaigns solely for M037.

## 8. Required authority documents

Read only:

1. `AGENTS.md`;
2. `README.md`;
3. `docs/ENGINEERING.md`;
4. `docs/engineering/command-contract.md`;
5. `docs/engineering/validation-tiers.md`;
6. `docs/engineering/human-review-workflow.md`;
7. `docs/engineering/cross-platform-development-and-launcher-policy.md`;
8. `docs/TERMINOLOGY.md`;
9. `docs/SPECS.md`;
10. `docs/specs/runtime-principles.md`;
11. `docs/specs/simulation-world-and-semantic-foundation-contract.md`;
12. current M032–M034 permanent gameplay/logistics/infrastructure specs resolved through `docs/SPECS.md`;
13. `docs/specs/save-compatibility-and-recovery-contract.md`;
14. `docs/specs/reproduction-and-internal-testing-contract.md`;
15. `docs/specs/render-projection-contract.md`;
16. `docs/specs/raylib-debug-client-contract.md`;
17. `docs/specs/sound-definition-and-command-contract.md`;
18. `docs/specs/input-action-map-contract.md`;
19. `docs/specs/tick-bound-input-frame-contract.md`;
20. `docs/specs/semantic-input-recording-and-replay-contract.md`;
21. `docs/specs/ui-toolkit-contract.md`;
22. `docs/specs/application-shell-and-client-contract.md`;
23. `docs/specs/save-catalog-and-autosave-contract.md`;
24. `docs/specs/user-settings-and-display-safety-contract.md`;
25. `docs/specs/software-defined-input-binding-contract.md`;
26. `docs/specs/world-configuration-and-new-game-contract.md`;
27. `docs/architecture/player-and-diagnostics-application-architecture.md`;
28. `docs/decisions/ADR-0048-player-and-diagnostics-clients-share-an-engine-owned-ui-and-application-foundation.md`;
29. `docs/artifacts/product-shell-and-ui-artifact-contract.md`;
30. this milestone document.

Implementation may inspect current source/tests/solution/projects/current engineering launchers needed to implement/prove these contracts. Do not read the external guide repository, `.guide-profile.json`, `.guide-sync/`, copied guides, prompt templates, or `docs/research/`. Read `.review/` only for M037 review execution.

## 9. Files or areas likely affected

```text
AGENTS.md
README.md
docs/ENGINEERING.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ARTIFACTS.md
docs/engineering/
docs/decisions/
docs/artifacts/

src/Agentic2D.Contracts/
src/Agentic2D.Engine/
src/Agentic2D.Rendering/
src/Agentic2D.Input/
src/Agentic2D.Simulation/
src/Agentic2D.Tools/
src/Agentic2D.DebugClient.Raylib/
src/Agentic2D.Engineering/
new application/UI/player-client projects if justified

tests/unit/Agentic2D.Tests.Unit/
game resource area for world configurations
.review/
artifacts/application/M037/
artifacts/validation/m037-smoke/
```

## 10. Validation tiers and concrete commands

### Tier 0 — repository/documentation sanity

Linux:

```bash
./eng/format.sh --verify
./eng/docs-check.sh
```

Windows:

```powershell
pwsh ./eng/format.ps1 --verify
pwsh ./eng/docs-check.ps1
```

Use the documented generic-host equivalent if documentation validation is not exposed as a dedicated launcher.

### Tier 1 — focused unit/contract validation

Linux:

```bash
./eng/test-filter.sh UiToolkit
./eng/test-filter.sh ApplicationShell
./eng/test-filter.sh PlayerDiagnosticsIsolation
./eng/test-filter.sh SaveCatalog
./eng/test-filter.sh Autosave
./eng/test-filter.sh UserSettings
./eng/test-filter.sh DisplaySafety
./eng/test-filter.sh SoftwareDefinedInput
./eng/test-filter.sh WorldConfiguration
./eng/test-filter.sh ProductShellLifecycle
```

Windows: the same filters through `pwsh ./eng/test-filter.ps1 <filter>`.

### Tier 2 — resumable M037 suite

Linux:

```bash
./eng/suite.sh m037-smoke --plan-json
./eng/suite.sh m037-smoke --shard <id>
./eng/suite.sh m037-smoke --verify
```

Windows:

```powershell
pwsh ./eng/suite.ps1 m037-smoke --plan-json
pwsh ./eng/suite.ps1 m037-smoke --shard <id>
pwsh ./eng/suite.ps1 m037-smoke --verify
```

Required logical shards:

```text
authority-normalization
ui-tree-layout
ui-focus-modal-text
application-foundation
player-diagnostics-isolation
main-pause-navigation
new-game-tutorial-entry
save-catalog-naming
manual-save-lifecycle
autosave-scheduling-retention
settings-validation-recovery
display-preview-rollback
input-registry-defaults
input-rebinding-conflicts
input-context-isolation
world-load-unload-resource-lifecycle
headless-structural-proof
linux-player-shell-graphics
windows-player-shell-graphics
affected-current-regression
review-pack
human-review
integrated
completion-audit
```

### Tier 3 — affected current regressions

At minimum preserve the maintained equivalents of input content/mapping/runtime/replay, render projection, Raylib diagnostics client, persistence diagnostics, save compatibility/recovery, operations surface, M034 settlement behavior, and M036 engineering-host/launcher behavior touched by M037 suite additions.

Use current suite/command IDs after implementation. Historical receipt fingerprints need not remain current.

### Tier 5 — human review

Linux:

```bash
./eng/review-list.sh --milestone M037
./eng/review-show.sh review.m037.product-shell-ui-saves-settings-and-input
./eng/review-check.sh --milestone M037
```

Windows: same operations through the PowerShell review launchers.

One approved review record gates M037. Record the platform used for human graphical review. Automated graphical proof is required on both supported development platforms.

## 11. Validation execution mode

```text
Tier 0: direct
Tier 1: direct/focused
Tier 2 structural: resumable-sharded
Linux graphics: graphics-capable Linux
Windows graphics: graphics-capable Windows
Tier 3: affected regression
Tier 5: human-review
final: completion-audit
```

Receipt root: `artifacts/validation/m037-smoke/`.

Aggregate verify fails for missing/stale/failed receipts, platform-authority contradiction, missing structural evidence, diagnostics leakage into player composition, failed save/settings/display/input semantics, incomplete lifecycle proof, missing Linux/Windows graphics proof, failed current regressions, unapproved M037 review, incomplete completion audit, or success inferred from partial output.

## 12. Acceptance criteria

M037 completes only when:

1. active docs no longer contradict M036 Linux/Bash + Windows/PowerShell-7 development support;
2. retained UI toolkit implements required controls/layout/focus/modal/scroll/text/theme/scaling and is headless-testable;
3. UI capture prevents gameplay actions firing through UI/text fields;
4. player and diagnostics compositions are distinct;
5. player startup does not require diagnostics-only services/artifacts;
6. diagnostics capability remains available;
7. main menu contains Continue/New Game/Load Game/Tutorial/Options/Credits/Quit and no Statistics/scenario selector;
8. pause menu contains Resume/Save/Load/Options/Return to Main Menu/Quit;
9. load/return/quit always ask save/continue-without-saving/cancel while a world is active;
10. New Game validates JSON world config and random/entered seed;
11. bundled relaxed/standard/demanding/stress-test configs validate;
12. effective world config is retained immutably in the world/save;
13. Tutorial uses fixed validated seed/config and remains endless;
14. Continue chooses newest valid save deterministically and skips invalid candidates with notice;
15. manual save naming pre-fills `<World Title> — Day <Simulation Day>` and is editable;
16. rename never changes world provenance;
17. save browser supports required operations/status without world hierarchy/delete-world;
18. autosave uses injected monotonic wall time and waits for valid save boundary;
19. autosave retention is configurable, defaults to five, rotates by WorldId, never deletes manual saves;
20. user settings are versioned/validated/atomic/migratable/recoverable;
21. declared audio/display/autosave/focus/alert/UI-scale settings function;
22. display changes use preview/countdown/rollback/next-start recovery;
23. safe mode/reset can recover startup without modifying saves;
24. bindable actions are explicitly registered and overrides use known IDs;
25. input contexts isolate menu/gameplay/tool/text/diagnostics behavior;
26. binding editor supports capture/conflicts/remove/reset-one/reset-all;
27. corrupt binding overrides fall back per action;
28. semantic input replay remains stable;
29. primary shell operations are pointer-operable;
30. game-wide workbench RDP compatibility is not required;
31. repeated world/menu/load cycles do not leak callbacks/stale ownership/native resources within bounded evidence;
32. structural validation passes on both supported development platforms;
33. player-shell graphics proof passes on Linux and Windows;
34. affected M031–M036 regressions pass;
35. M037 artifacts satisfy the artifact contract;
36. blocking review `review.m037.product-shell-ui-saves-settings-and-input` is approved;
37. aggregate M037 verifier passes;
38. `m037-completion-audit.json` shows all applicable obligations satisfied;
39. executor terminal outcome is `COMPLETE`;
40. direct docs reflect current truth;
41. no deferred quick-save/statistics/world-rule/scenario/procedural/audiovisual/population/distribution scope is introduced;
42. ordinary implementation agents require no external guide access.

## 13. Direct documentation impact

Update current project truth where behavior changes: correct stale M036 platform statements in `AGENTS.md` and `docs/ENGINEERING.md`; update README after acceptance; index M037 specs in `docs/SPECS.md`; update terminology, engineering/command docs only for actual current behavior, `docs/ARTIFACTS.md`, decision/artifact indexes, current input/save/render/sound docs where behavior changed, and solution-shape docs if new projects are created.

Do not add Statistics, scenario, procedural-map, asset-integration, or distribution docs.

## 14. Deferred documentation synchronization hints

Created by this package:

```text
.guide-sync/pending/2026-08-20-m037-product-shell-ui-and-settings-sync.md
```

Ordinary implementation agents must not read or resolve it.

## 15. Human-review requirements

```text
applicability: required
completion effect: blocking
review classes: UX, visual, semantic, accessibility-baseline, artifact-quality
review ID: review.m037.product-shell-ui-saves-settings-and-input
owning milestone: M037
reviewer role: repository user
acceptable decision: approved
implicit waiver: none
```

Subject: usability, safety, clarity, and architectural separation of the first player-facing shell, including menus, New Game/Tutorial entry, save naming/browser/autosave, options, display rollback, safe mode, and input rebinding.

Human review may run on one supported graphics-capable platform but must record which. Automated graphics proof remains required on both. Completed M037 review becomes historical and is not reopened by later presentation milestones.

Exact checks:

```text
./eng/review-show.sh review.m037.product-shell-ui-saves-settings-and-input
./eng/review-check.sh --milestone M037
```

or PowerShell equivalents.

## 16. Constrained-runtime handling

Use the generic resumable suite. Generate plan, run bounded shards, run Linux/Windows graphical shards on matching hosts, generate review pack, complete review, refresh human-review receipt if necessary, run aggregate verify, generate completion audit, and continue fixing every agent-resolvable gap.

Partial output, one platform, one graphics proof, passing compilation, or a pending review is not completion.

Terminal outcomes under v0.7.2:

```text
COMPLETE
AWAITING HUMAN REVIEW
BLOCKED
```

Use `AWAITING HUMAN REVIEW` only when all agent-resolvable work is done and M037 review is the sole remaining gate. Use `BLOCKED` only for unavailable external capability or a material planning decision; a genuinely unavailable second platform may qualify after all local agent-resolvable work is complete. Failing tests, incomplete implementation/evidence/docs, or stale receipts are not blockers.

## 17. Out-of-scope guide migration work

M037 does not migrate the guide system. M036 already established v0.7.2. Do not read or modify the external guide repository during ordinary implementation.
