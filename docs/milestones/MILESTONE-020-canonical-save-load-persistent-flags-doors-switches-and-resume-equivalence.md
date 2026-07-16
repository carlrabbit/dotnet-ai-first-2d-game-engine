# Milestone 020 — Canonical Save/Load, Persistent Flags, Doors, Switches, and Resume Equivalence

## Goal

Implement deterministic persistence for authoritative runtime state and prove it through one persistent-world journey:

```text
collect item → inventory changes → activate switch → persistent flag changes
→ unlock/open door → collision/rendering change → save → destroy runtime
→ load fresh runtime → continue through door → compare with uninterrupted execution
```

A save is resumable authoritative state, not a diagnostic dump or serialized implementation graph.

## Repository role and maturity assumptions

- Role: `capability-provider` with bounded consumer dogfood.
- Maturity: implementation-ready, artifact-first, headless-first, CLI/API-first.
- M000–M019 and accepted completion patches are available.
- M019 provides inventory, collection, lifecycle, entity removal, sound, animation, rendering, semantic replay, and unified-run evidence.
- Authored maps remain structurally static; mutable world objects are runtime entities.

## Execution mode

```text
ai-executed-broad
```

Implement sequentially:

1. canonical persistence and contributors;
2. save/load/resume and equivalence;
3. persistent flags and bounded conditions;
4. switches and atomic flag transitions;
5. doors and mutable collision/presentation projection;
6. integrated persistent-world coherence journey.

Do not begin a later gate until focused validation for the prior gate passes.

## Scope

### Gate 1 — Canonical persistence

- versioned save schema and manifest;
- canonical save snapshot;
- stable persistence contributor IDs and schema versions;
- required/optional contributor policy;
- immutable snapshot capture at an explicit tick;
- deterministic ordering and fingerprinting;
- contributors for runtime, entities, components, resources, lifecycle, inventory, removed entities, interaction/trigger state, minimal animation continuity, flags, switches, and doors;
- explicit removed-entity persistence preventing collected entities from respawning;
- excluded-state validation.

### Gate 2 — Save/load/resume

- `save create`, `save inspect`, `save validate`, and `project resume`;
- strict project/scenario/content/contributor compatibility;
- complete load planning before mutation;
- transactional load into a fresh runtime only;
- save-load-save canonical equivalence;
- uninterrupted-versus-resumed execution equivalence;
- semantic input continuation;
- structured incompatibility diagnostics;
- unified-run linkage.

### Gate 3 — Flags and conditions

- authored flag definitions under `game/flags/`;
- boolean and closed enum flags;
- explicit flag transitions, revisions, events, persistence;
- condition atoms: `flag-equals`, `inventory-contains`, `entity-lifecycle-equals`;
- composition: `all`, `any`, `not`;
- deterministic, side-effect-free condition inspection.

### Gate 4 — Switches

- switch runtime component/state: `inactive`, `activated`;
- one-shot policy;
- interaction intent/resolution;
- atomic switch-state and declared flag transition;
- repeated activation rejection;
- post-commit `switch.activated` event;
- animation/sound projection;
- persistence.

### Gate 5 — Doors

- door runtime component/state: `locked`, `closed`, `open`;
- structured condition reference;
- interaction intent/resolution;
- atomic door-state and collision transition;
- `door.unlocked` and `door.opened` events;
- collision/spatial participation and interaction availability changes;
- deterministic spatial/render invalidation;
- animation/sound/render projection;
- persistence and post-load restoration before resumed movement.

### Gate 6 — Integrated journey

- collect `item.collectible-crystal` once;
- inventory retains it;
- collected entity remains absent;
- activate one-shot switch;
- set persistent flag;
- open a condition-gated door without consuming the item;
- disable door collision without mutating map tiles;
- save after opening and before traversal;
- destroy and recreate runtime;
- load and verify state;
- continue through doorway;
- compare state, events, sound, animation, rendering, and assertions with uninterrupted execution.

## Locked design decisions

### Save authority

Initial compatibility policy:

```text
supported save schema/version
+ exact project ID
+ exact scenario/world ID
+ compatible content fingerprint
+ complete required contributor set
```

No automatic migrations.

Persist authoritative state including runtime tick/continuation, entity IDs, persistent components, health, lifecycle, inventory, removed-entity state, flags, switches, doors, and interaction/trigger state required for continuation.

Do not persist native handles, textures, sounds, raylib state, sound commands, render commands/items, sampled animation frames, marker artifacts, review packs, diagnostics, artifact paths, wall-clock values, caches, or transient command/event buffers.

Animation persistence is limited to explicit selection continuity only when reconstruction cannot provide exact continuation. Physical sound playback state is never persisted; pre-save one-shots must not replay after load.

### Removed entities

Use explicit tombstones or equivalent authoritative spawn-state records. Merely omitting an entity from the save is insufficient when scenario initialization would respawn it.

### Load

```text
parse → schema/compatibility validation → contributor/reference validation
→ complete load plan → construct fresh runtime → one transaction
→ reconstructed-state validation → optional resume
```

Failed load leaves no partially reconstructed runtime.

### Flags and conditions

Setting a flag to its existing value is an accepted, evidenced no-op. Conditions are bounded structured data, not scripts.

### Stateful world objects

Switches and doors are runtime entities. Authored map tiles and static map collision are not rewritten. Door state projects into collision, spatial indexing, interaction, animation, sound, and rendering.

## Non-goals

Do not implement:

- save-slot UI, autosave, quick-save, cloud saves, profiles;
- encryption, compression, delta/incremental saves;
- background saving, partial/chunk loading, streaming worlds;
- arbitrary historical migrations or best-effort incompatible load;
- counters, strings, timers, arbitrary expressions, scripting, rule engines;
- pressure plates, timed switches, wiring graphs, multi-stage puzzles;
- item consumption, key semantics, chests, destructible terrain, tile replacement;
- pathfinding/navmesh updates, physics doors, automatic closing;
- particles, camera, UI, editor, networking;
- portable SDK, packaging, deployment;
- broad documentation cleanup, guide migration, TBPs, issue templates, workflows, or public/release docs.

## Focus areas

### Canonical snapshot

Recommended files:

```text
save-manifest.json
save-snapshot.json
save-diagnostics.json
```

Canonical ordering:

```text
contributors by ID
entities by entity ID
components by entity ID then component type ID
resources by entity ID then resource ID
inventory entries by item definition ID
removed entities by entity ID
flags by flag ID
switches/doors by entity ID
```

### Contributor boundary

Introduce an internal boundary such as `IPersistenceContributor` with:

- stable ID and schema version;
- required/optional status;
- capture from immutable state;
- canonicalization;
- reference and compatibility validation;
- load-plan creation;
- transactional application;
- diagnostics/fingerprint.

Unknown required contributors reject load. Unknown optional contributors may be ignored only when explicitly declared optional and safe.

### Commands

```bash
agentic2d save create --project <project-or-workspace> --run <run-dir> --tick <tick-or-final> --save-id <id> --output <dir>
agentic2d save inspect <save-path> --output <dir>
agentic2d save validate <save-path> --project <project-or-workspace> --output <dir>
agentic2d project resume <project-or-workspace> --save <save-path> [--recording <recording>] --output <run-dir>
agentic2d state inspect --project <project-or-workspace> --scenario <scenario-id> --output <dir>
```

Update `content validate`, `project validate`, `project run`, `run inspect`, and `run review` to include flags, saves, resume, switches, doors, and persistent-world evidence.

### Required scenarios

```text
save.canonical-roundtrip-smoke
save.incompatible-content-smoke
state.flag-condition-smoke
state.switch-activation-smoke
state.door-collision-smoke
gameplay.persistent-world-resume-smoke
```

### Required artifacts

Save:

```text
save-result.json
save-manifest.json
save-snapshot.json
save-contributors.json
save-validation.json
save-load-plan.json
save-equivalence.json
save-diagnostics.json
```

Persistent world:

```text
persistent-world-result.json
flag-transitions.jsonl
condition-evaluations.jsonl
switch-intents.jsonl
switch-resolutions.jsonl
switch-transitions.jsonl
door-intents.jsonl
door-resolutions.jsonl
door-transitions.jsonl
projection-invalidations.jsonl
persistent-world-diagnostics.json
```

### Required wrappers

```bash
./eng/save-canonical-roundtrip-smoke.sh
./eng/save-incompatibility-smoke.sh
./eng/save-resume-equivalence-smoke.sh
./eng/state-flag-condition-smoke.sh
./eng/state-switch-activation-smoke.sh
./eng/state-door-collision-smoke.sh
./eng/persistent-world-integrated-smoke.sh
./eng/persistent-world-review-smoke.sh
./eng/m020-smoke.sh
```

All required wrappers are headless and network-independent.

## Implementation constraints

- Do not reflexively serialize runtime object graphs or CLR type names.
- Use explicit schemas, stable IDs, canonical records, and contributor versions.
- Capture one immutable tick snapshot.
- Load only after complete validation and only into a fresh runtime.
- Exclude machine-specific paths from semantic identity.
- Prefer strict incompatibility rejection over unsafe recovery.
- Maps remain static; doors/switches are runtime entities.
- Conditions are side-effect-free structured data.
- Door opening changes runtime collision/projection state, not authored tiles.
- Presentation derives from committed gameplay state/events.
- Ordinary implementation agents must not read `.guide-profile.json`, `.guide-sync/`, external guides, copied guides, or prompt templates.

## Required authority documents

Read only:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/SCENARIOS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/engineering/future-dotnet-solution.md
docs/specs/runtime-principles.md
docs/specs/minimal-deterministic-runtime.md
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/entity-definition-and-instantiation-contract.md
docs/specs/mixed-world-projection-contract.md
docs/specs/pluggable-spatial-runtime-contract.md
docs/specs/grid-spatial-module-contract.md
docs/specs/continuous-kinematic-spatial-module-contract.md
docs/specs/spatial-query-and-trigger-contract.md
docs/specs/interaction-runtime-contract.md
docs/specs/render-projection-contract.md
docs/specs/semantic-input-recording-and-replay-contract.md
docs/specs/animation-selection-and-sampling-contract.md
docs/specs/game-project-manifest-contract.md
docs/specs/game-workspace-manifest-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/specs/resource-damage-and-lifecycle-contract.md
docs/specs/item-inventory-and-collection-contract.md
docs/specs/gameplay-presentation-event-contract.md
docs/specs/canonical-save-snapshot-contract.md
docs/specs/persistence-contributor-contract.md
docs/specs/save-load-and-resume-contract.md
docs/specs/persistent-flag-and-condition-contract.md
docs/specs/stateful-world-entity-contract.md
docs/specs/mutable-world-projection-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/animation-execution-artifact-contract.md
docs/artifacts/sound-execution-artifact-contract.md
docs/artifacts/gameplay-state-artifact-contract.md
docs/artifacts/save-execution-artifact-contract.md
docs/artifacts/persistent-world-state-artifact-contract.md
docs/decisions/ADR-0023-gameplay-state-changes-use-explicit-atomic-runtime-transactions.md
docs/decisions/ADR-0024-saves-contain-canonical-authoritative-state.md
docs/decisions/ADR-0025-stateful-world-objects-are-runtime-entities.md
docs/milestones/MILESTONE-018-game-workspace-manifest-deterministic-scaffolding-and-unified-agent-execution-workflow.md
docs/milestones/MILESTONE-019-sound-feedback-gameplay-state-lifecycle-items-and-collection.md
docs/milestones/MILESTONE-020-canonical-save-load-persistent-flags-doors-switches-and-resume-equivalence.md
```

## Files or areas likely affected

Recommended new project:

```text
src/Agentic2D.Persistence
```

Likely affected:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Entities
src/Agentic2D.Behaviors
src/Agentic2D.Gameplay
src/Agentic2D.Spatial.Grid
src/Agentic2D.Spatial.Continuous
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Animation
src/Agentic2D.Sound
src/Agentic2D.Rendering
src/Agentic2D.Workspaces
src/Agentic2D.Tools
tests/unit/Agentic2D.Tests.Unit
game/flags/
game/entities/
game/visuals/
game/animations/
game/sounds/
game/input/
game/scenarios/smoke/
```

## Validation tiers and concrete commands

Tier 1 covers canonical ordering, contributor compatibility, excluded state, tombstones, transactional load rollback, round-trip equivalence, flags/conditions, atomic switch/flag transitions, door collision/projection invalidation, and resumed equivalence.

Tier 2:

```bash
./eng/save-canonical-roundtrip-smoke.sh
./eng/save-incompatibility-smoke.sh
./eng/state-flag-condition-smoke.sh
./eng/state-switch-activation-smoke.sh
./eng/state-door-collision-smoke.sh
```

Tier 3:

```bash
./eng/save-resume-equivalence-smoke.sh
./eng/persistent-world-integrated-smoke.sh
./eng/persistent-world-review-smoke.sh
```

Tier 4:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh entities
./eng/content-validate.sh visuals
./eng/content-validate.sh animations
./eng/content-validate.sh sounds
./eng/content-validate.sh items
./eng/content-validate.sh flags
./eng/m015-smoke.sh
./eng/m016-smoke.sh
./eng/m017-smoke.sh
./eng/m018-smoke.sh
./eng/m019-smoke.sh
./eng/m020-smoke.sh
```

## Acceptance criteria

1. Save schema and contributors are versioned and stable-ID based.
2. Serialization and record ordering are deterministic.
3. Missing/unknown required contributors reject load precisely.
4. Excluded adapter/presentation/transient state is absent.
5. Collected entity absence is explicitly persisted and does not respawn.
6. Save capture uses one immutable tick.
7. Save inspection explains identity, contributors, state counts, fingerprints, and compatibility.
8. Project, scenario/world, content, and contributor incompatibilities are diagnosed.
9. Load targets a fresh runtime, validates fully, and applies transactionally.
10. Failed load leaves no partial runtime.
11. Save-load-save without advancing is canonically equivalent.
12. Runtime tick and deterministic continuation restore.
13. Resumed semantic input produces equivalent authoritative final state and event ordering.
14. Pre-save one-shot sounds do not replay after load.
15. Required animation continuity and final render fingerprint are equivalent.
16. Boolean/enum flags validate, persist, and use revisions.
17. Same-value flag set is an evidenced no-op.
18. All supported condition atoms and composition are deterministic and side-effect-free.
19. Switch is a runtime entity and one-shot activation is deterministic.
20. Switch-state and flag changes commit atomically; repeat activation changes nothing.
21. Door is a runtime entity with explicit states and bounded condition.
22. Failed door condition leaves state/collision unchanged.
23. Successful opening changes state and collision atomically, then emits events.
24. Map tiles remain unchanged.
25. Spatial/render invalidations are explicit and deterministic.
26. Open/non-colliding door state restores before resumed movement.
27. Inventory, removed item, flags, switch, and door persist.
28. Player continues through the door after fresh load.
29. Unified run links save, gameplay, world-state, animation, sound, render, and review evidence.
30. `run inspect` and `run review` explain equivalence or divergence.
31. Provider and bounded consumer validation remain distinct.
32. Required tests are headless and network-independent.
33. No out-of-scope save UI, migrations, map mutation, particles, camera, UI, SDK, packaging, guide, workflow, TBP, or issue-template work is introduced.
34. M015–M019 regressions pass and M020 docs/commands/artifacts are indexed after acceptance.

## Direct documentation impact

Update only direct project truth needed for discoverability and correctness, including README/AGENTS, terminology/spec/scenario/content/artifact/human-review/engineering/milestone/decision indexes, product CLI and validation docs, affected runtime/entity/spatial/interaction/render/workspace/gameplay contracts, unified-run/review contracts, and the roadmap. Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-16-m020-index-roadmap-and-crosslink-sync.md
.guide-sync/pending/2026-07-16-m020-human-review-persistence-world-state-followup.md
```

Ordinary implementation agents must not read these files.

## Human review requirements

Review canonical authority boundaries, contributor extensibility, strict compatibility diagnostics, explicit removed-entity persistence, minimal animation continuity, no persisted sound playback, transactional load, meaningful equivalence, bounded conditions, atomic switch/flag transition, static-map/runtime-door separation, collision restoration timing, event/presentation provenance, and evidence usability.

## Out-of-scope guide migration work

No guide migration. Do not modify `.guide-profile.json`, copy guide documents, reference guides as operational authority, or require ordinary implementation agents to read `.guide-sync/`.
