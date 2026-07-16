# Milestone 019 — Sound Feedback, Gameplay State, Lifecycle, Items, and Collection

## Goal

Establish the first broad integrated gameplay-state slice by connecting deterministic sound feedback, health and damage, explicit lifecycle transitions, authored items, inventory ownership, and atomic collection through one replayable consumer-workspace journey.

```text
semantic input
→ movement and animation markers
→ deterministic footstep sound cues
→ damage intent and health transition
→ damaged/defeated domain events
→ animation and sound feedback
→ collection intent
→ atomic inventory/world-item transaction
→ item-collected event and sound feedback
→ unified run evidence
→ semantic replay equivalence
```

Sound is presentation-only. Gameplay state changes remain runtime authority.

## Repository role and maturity assumptions

```text
repository role: capability-provider
maturity: implementation-ready, artifact-first
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation
```

Assume M000–M018 and accepted completion patches are implemented. M018 provides consumer workspaces and unified runs; M017 provides presentation-only animation markers; runtime mutation remains validated and deterministic.

## Execution mode

```text
ai-executed-broad
```

Implement through four mandatory gates:

1. sound definitions, projection, commands, artifacts, optional adapter;
2. health, damage, defeat, lifecycle;
3. items, inventory, atomic collection;
4. integrated workspace journey and replay equivalence.

Do not begin a later gate until focused validation for the prior gate passes.

## Scope

### Gate 1 — Sound foundation

- authored sound definitions under `game/sounds/`;
- stable cue, definition, group, variant, asset, command, and loop-instance IDs;
- deterministic variant selection;
- marker/event/request-to-cue projection;
- backend-neutral `PlayCue`, `StartLoop`, `ReplaceLoop`, `StopLoop`, and `SetGroupVolume` commands;
- immutable sound command frames and loop playback state;
- project-aware content resolution;
- headless inspection and structured artifacts;
- optional raylib playback isolated in `Agentic2D.DebugClient.Raylib`.

### Gate 2 — Health, damage, defeat, lifecycle

- bounded integer `resource.health` component;
- positive-integer damage intents and deterministic resolutions;
- duplicate-correlation rejection;
- capped applied damage;
- post-commit `resource.changed`, `entity.damaged`, and first-transition-only `entity.defeated` events;
- explicit `active`, `defeated`, and optional `inactive` lifecycle state;
- defeated entities remain queryable/renderable but stop normal gameplay behaviors;
- damage and defeat presentation projected from domain events.

### Gate 3 — Items, inventory, collection

- authored item definitions under `game/items/`;
- world-item component with item ID and positive quantity;
- entry-based inventory with distinct-entry capacity and canonical ordering;
- item-defined stack limits;
- all-or-nothing collection;
- atomic inventory update plus world-item entity removal;
- events only after successful commit;
- no duplicate collection after removal;
- collection sound and optional animation feedback projected from `item.collected`.

### Gate 4 — Integrated journey

- focused scenarios for sound markers, loop ownership, damage, defeat, and collection atomicity;
- one integrated `gameplay.sound-damage-collection-lifecycle-smoke` scenario;
- M018 project-aware unified run;
- run-manifest links for sound, resources, damage, lifecycle, items, inventory, collection, animation, render, input, runtime, and review;
- semantic replay equivalence across state, events, cue selections, commands, animation, and final render fingerprint.

## Locked design decisions

### Sound

- sound is presentation, not gameplay authority;
- gameplay emits domain events, never native audio calls;
- animation markers may request sound but cannot mutate gameplay;
- cue variants are selected deterministically from stable context;
- sound groups initially are `sound-group.effects` and `sound-group.ambience`;
- value ranges: volume `[0,1]`, pitch `[0.25,4]`, pan `[-1,1]`;
- loop ownership uses explicit `loopInstanceKey`;
- duplicate `StartLoop` is rejected;
- replacement uses `ReplaceLoop`;
- missing `StopLoop` is an accepted explicit no-op;
- structural evidence is authoritative; audible playback is optional review evidence.

Required mappings:

```text
presentation.footstep → cue.player.footstep
entity.damaged        → cue.entity.damage
entity.defeated       → cue.entity.defeat
item.collected        → cue.item.collection
```

### Damage and lifecycle

- M019 implements only `resource.health`;
- damage must be a positive integer; healing is not negative damage;
- flow is intent → resolution → command/transition → post-commit events;
- applied damage is capped at remaining health;
- defeat occurs once when health first reaches minimum;
- defeat is not entity removal;
- primary slice leaves defeated entities present with lifecycle `defeated`;
- normal behaviors do not run for defeated entities;
- gameplay correctness cannot depend on animation or audio timing.

### Items and collection

- initial item is `item.collectible-crystal`;
- inventory is entry-based, not grid-based;
- capacity counts distinct item entries;
- entries sort by item-definition ID ordinal;
- collection is full-stack or rejected;
- prevalidate the complete transaction;
- inventory mutation and world-item removal commit atomically;
- failure leaves both inventory and world unchanged;
- event emission follows commit.

## Non-goals

Do not implement:

- mandatory physical audio, streaming music, playlists, DSP, filters, reverb, spatial acoustics, microphone input, or dynamic audio graphs;
- healing, armor, resistances, critical hits, status effects, regeneration, factions, weapons, projectiles, enemy AI, respawning, or combat formulas;
- automatic destruction on defeat, corpses, pooling, resurrection, or generic lifecycle state machines;
- equipment, grid inventory, nested containers, durability, crafting, currency, shops, loot tables, dropping, weight, quest items, keys/doors, or partial collection;
- save/load, quests, dialogue, networking, editor UI, scripting, SDK packaging, workspace migration/update, package publishing, broad cleanup, workflows, TBPs, issue templates, public/release docs, or guide migration.

## Focus areas

### 1. Sound content and commands

Required definitions:

```text
sound-definition.player-footstep
sound-definition.entity-damage
sound-definition.entity-defeat
sound-definition.item-collection
sound-definition.ambient-loop-smoke
```

Definitions contain schema, stable IDs, group, variants, asset references, bounded defaults, tags, and provenance. Use deterministic small WAV fixtures where raw audio is needed.

A cue request records cue ID, source kind/ID, runtime tick, occurrence ordinal, selection context, seed, and optional explicit variant. Do not use ambient process randomness.

One immutable command frame per applicable tick records ordered selections, commands, resulting loop state, diagnostics, and fingerprint. Define and test stable source priority.

### 2. Resource, damage, and lifecycle

Resource fields:

```text
resourceTypeId
current
minimum
maximum
revision
```

Validation:

```text
minimum <= current <= maximum
maximum > minimum
```

Damage intent includes intent ID, source or environmental source ID, target entity ID, damage-kind ID, positive amount, runtime tick, correlation ID, and provenance.

Required damage kinds:

```text
damage.generic
damage.environment
```

Resolution rejects missing target, missing health, invalid lifecycle, non-positive amount, duplicate correlation, and already-defeated target. It records requested/applied amount, previous/resulting health, lifecycle before/after, rejection reason, and generated records.

### 3. Items, inventory, collection

Item definition contains stable ID, stackability, maximum stack, tags, visual reference, collection cue, and provenance.

World-item component contains item-definition ID, positive quantity, and revision.

Inventory contains inventory ID, maximum distinct entries, ordered `(itemDefinitionId, quantity)` entries, and revision.

Collection intent includes intent ID, collector entity ID, world-item entity ID, optional expected item ID, runtime tick, correlation ID, and provenance.

Collection validates collector/activity/inventory, world entity/item, capacity, stack limit, correlation, and references before committing.

### 4. Integrated scenario

Initial state:

- active player with health and empty inventory;
- active target with health;
- collectible world item;
- simple authored map, visual, animation, sound, input, and scenario content;
- deterministic seed.

Journey:

```text
player moves
→ footstep markers and cues

environment damages player
→ health transition, damage event, overlay, cue

player collects item through explicit interaction
→ inventory update + entity removal atomically
→ item.collected and cue

player damages target
→ target reaches minimum health
→ damaged + defeated events
→ lifecycle defeated
→ normal target behavior suppressed
→ defeat presentation and cue
```

No enemy AI is required.

## Implementation constraints

- runtime owns authoritative components, lifecycle, commands, transactions, and domain events;
- behaviors read immutable snapshots and emit intents;
- sound projection reads immutable events/markers and emits presentation commands;
- markers and sound cannot cause damage or collection;
- raylib types remain isolated;
- physical playback is optional;
- event emission occurs after commit;
- collection is atomic and all-or-nothing;
- defeat and removal remain distinct;
- all acceptance tests are headless and network-independent;
- ordinary implementation agents must not read `.guide-profile.json`, `.guide-sync/`, external guides, copied guides, or prompt templates.

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
docs/specs/asset-metadata-contract.md
docs/specs/behavior-modules.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/entity-definition-and-instantiation-contract.md
docs/specs/spatial-query-and-trigger-contract.md
docs/specs/interaction-runtime-contract.md
docs/specs/visual-definition-contract.md
docs/specs/render-projection-contract.md
docs/specs/input-action-map-contract.md
docs/specs/tick-bound-input-frame-contract.md
docs/specs/semantic-input-recording-and-replay-contract.md
docs/specs/animation-definition-contract.md
docs/specs/animation-selection-and-sampling-contract.md
docs/specs/animation-marker-contract.md
docs/specs/animated-render-projection-contract.md
docs/specs/game-project-manifest-contract.md
docs/specs/game-workspace-manifest-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/specs/sound-definition-and-command-contract.md
docs/specs/sound-cue-projection-contract.md
docs/specs/resource-damage-and-lifecycle-contract.md
docs/specs/item-inventory-and-collection-contract.md
docs/specs/gameplay-presentation-event-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/artifacts/render-projection-artifact-contract.md
docs/artifacts/animation-execution-artifact-contract.md
docs/artifacts/sound-execution-artifact-contract.md
docs/artifacts/gameplay-state-artifact-contract.md
docs/decisions/ADR-0018-rendering-is-read-only-and-raylib-is-an-isolated-adapter.md
docs/decisions/ADR-0020-animation-produces-typed-presentation-patches.md
docs/decisions/ADR-0021-workspaces-separate-game-truth-from-engine-acquisition.md
docs/decisions/ADR-0022-sound-is-deterministic-presentation-projection.md
docs/decisions/ADR-0023-gameplay-state-changes-use-explicit-atomic-runtime-transactions.md
docs/milestones/MILESTONE-017-deterministic-keyframe-animation-base-overlay-markers-and-animated-render-projection.md
docs/milestones/MILESTONE-018-game-workspace-manifest-deterministic-scaffolding-and-unified-agent-execution-workflow.md
docs/milestones/MILESTONE-019-sound-feedback-gameplay-state-lifecycle-items-and-collection.md
```

Do not read external guide documents for implementation.

## Files or areas likely affected

Recommended new projects:

```text
src/Agentic2D.Sound
src/Agentic2D.Gameplay
```

Likely affected:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Entities
src/Agentic2D.Behaviors
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Animation
src/Agentic2D.Rendering
src/Agentic2D.Workspaces
src/Agentic2D.Tools
src/Agentic2D.DebugClient.Raylib
tests/unit/Agentic2D.Tests.Unit
game/sounds
game/items
game/entities
game/visuals
game/animations
game/input
game/scenarios/smoke
game/assets/raw/samples
game/assets/metadata
```

Update the generated minimal-game template with equivalent `game-content/sounds/`, `game-content/items/`, and integrated fixture content where bounded dogfood is appropriate.

## Required commands

Add product commands consistent with current CLI conventions:

```bash
agentic2d sound inspect <sound-id-or-path> --output <directory>
agentic2d sound project --project <project-or-workspace> --scenario <scenario-id> --output <directory>
agentic2d gameplay inspect --project <project-or-workspace> --scenario <scenario-id> --output <directory>
```

`project validate`, `project run`, `run inspect`, and `run review` must include the new domains automatically.

## Required artifacts

Sound:

```text
sound-result.json
sound-definitions.json
sound-cue-selections.jsonl
sound-commands.jsonl
sound-command-frames.jsonl
sound-playback-state.jsonl
sound-diagnostics.json
```

Gameplay:

```text
gameplay-result.json
resource-transitions.jsonl
damage-intents.jsonl
damage-resolutions.jsonl
lifecycle-transitions.jsonl
collection-intents.jsonl
collection-resolutions.jsonl
inventory-transitions.jsonl
world-item-transitions.jsonl
gameplay-diagnostics.json
```

Do not create empty placeholder files for unexercised families.

## Required engineering wrappers

```bash
./eng/sound-content-smoke.sh
./eng/sound-marker-cue-smoke.sh
./eng/sound-loop-ownership-smoke.sh
./eng/gameplay-damage-resource-smoke.sh
./eng/gameplay-defeat-lifecycle-smoke.sh
./eng/gameplay-collection-atomicity-smoke.sh
./eng/gameplay-integrated-smoke.sh
./eng/gameplay-replay-smoke.sh
./eng/m019-smoke.sh
```

All required wrappers run headlessly. Optional physical playback smoke is separate and skips cleanly when unavailable.

## Validation tiers and concrete commands

### Tier 1 — Unit and contract tests

Cover content validation, cue determinism, command ordering, loop ownership, event mappings, health bounds, damage rejection/acceptance, duplicate correlation, defeat-once semantics, behavior suppression, stack/capacity validation, atomic rollback, entity removal, and replay equivalence.

### Tier 2 — Focused smokes

```bash
./eng/sound-content-smoke.sh
./eng/sound-marker-cue-smoke.sh
./eng/sound-loop-ownership-smoke.sh
./eng/gameplay-damage-resource-smoke.sh
./eng/gameplay-defeat-lifecycle-smoke.sh
./eng/gameplay-collection-atomicity-smoke.sh
```

### Tier 3 — Integrated capability

```bash
./eng/gameplay-integrated-smoke.sh
./eng/gameplay-replay-smoke.sh
```

### Tier 4 — Full gate

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/content-validate.sh entities
./eng/content-validate.sh visuals
./eng/content-validate.sh animations
./eng/content-validate.sh sounds
./eng/content-validate.sh items
./eng/m015-smoke.sh
./eng/m016-smoke.sh
./eng/m017-smoke.sh
./eng/m018-smoke.sh
./eng/m019-smoke.sh
```

Expected direct command shapes:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate sounds --output artifacts/content/sounds
dotnet run --project src/Agentic2D.Tools -- content validate items --output artifacts/content/items
dotnet run --project src/Agentic2D.Tools -- sound inspect sound-definition.player-footstep --output artifacts/sound/player-footstep
dotnet run --project src/Agentic2D.Tools -- sound project --project . --scenario sound.marker-cue-smoke --output artifacts/sound/marker-cue
dotnet run --project src/Agentic2D.Tools -- gameplay inspect --project . --scenario gameplay.sound-damage-collection-lifecycle-smoke --output artifacts/gameplay/integrated
dotnet run --project src/Agentic2D.Tools -- project run . --scenario gameplay.sound-damage-collection-lifecycle-smoke --output artifacts/runs/m019-integrated
dotnet run --project src/Agentic2D.Tools -- run inspect artifacts/runs/m019-integrated --output artifacts/run-inspect/m019-integrated
dotnet run --project src/Agentic2D.Tools -- run review artifacts/runs/m019-integrated --output artifacts/review/m019-integrated
```

Use current CLI syntax if exact parsing differs and update direct documentation consistently.

## Acceptance criteria

### Gate 1

1. Required sound definitions validate.
2. IDs and asset references are stable.
3. Values are finite and in range.
4. Variant selection is deterministic and evidenced.
5. Marker/event mappings are explicit.
6. Command ordering is stable.
7. One-shots do not create persistent loop state.
8. Loop start/replace/stop rules are explicit and tested.
9. Missing stop yields the documented no-op result.
10. Projection is headless and raylib-free.
11. Physical playback is optional.
12. Replay produces equivalent cue selections and commands.

### Gate 2

13. Health bounds validate.
14. Non-positive damage is rejected.
15. Missing/invalid/already-defeated targets are rejected.
16. Duplicate correlation cannot reapply damage.
17. Applied damage is capped.
18. Events follow successful mutation.
19. Defeat emits once.
20. Defeat and removal remain distinct.
21. Defeated entities remain inspectable/renderable.
22. Normal behavior is suppressed for defeated entities.
23. Presentation feedback does not control gameplay.
24. Replay preserves health, lifecycle, and event state.

### Gate 3

25. Item/world-item/inventory content validates.
26. Entries have canonical order.
27. Capacity and stack limits are enforced.
28. Collection is all-or-nothing.
29. Rejection leaves both world and inventory unchanged.
30. Success atomically updates inventory and removes the world entity.
31. Events follow commit.
32. Removed item cannot be collected again.
33. Collection cue derives from `item.collected`.
34. Replay preserves inventory, removal, and event state.

### Gate 4

35. Integrated scenario uses semantic input.
36. Footstep markers project deterministic cues.
37. Damage changes health and produces feedback.
38. Collection changes inventory and removes item.
39. Target defeat changes lifecycle and suppresses behavior.
40. Unified run links all exercised families.
41. Run inspect validates sound/gameplay evidence.
42. Run review presents structural evidence and optional playback review separately.
43. Replay produces equivalent intents, resolutions, events, transitions, sound frames, final runtime state, final playback state, animation evidence, and final render fingerprint.
44. Provider and consumer validation remain distinct.
45. All required validation is headless and network-independent.
46. No excluded sound/combat/inventory/save/editor/packaging/guide work is introduced.
47. M015–M018 regressions pass.
48. ADR-0022, ADR-0023, M019, specs, artifacts, commands, wrappers, and content domains are indexed after acceptance.

## Direct documentation impact

Update only where required:

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
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/engineering/future-dotnet-solution.md
docs/specs/content-validation-contract.md
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/interaction-runtime-contract.md
docs/specs/animation-marker-contract.md
docs/specs/animated-render-projection-contract.md
docs/specs/game-project-manifest-contract.md
docs/specs/workspace-scaffolding-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/artifacts/review-pack-artifact-contract.md
```

Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-16-m019-index-and-crosslink-sync.md
.guide-sync/pending/2026-07-16-m019-human-review-gameplay-sound-boundary-followup.md
```

Ordinary implementation agents must not read these files.

## Human review requirements

Review sound authority, marker/event provenance, deterministic variant usefulness, loop ownership, damage mutation boundaries, defeat/removal distinction, defeated behavior suppression, inventory simplicity, atomic collection and rollback, event-to-feedback traceability, unified evidence quality, replay equivalence, and whether future save/load can persist authoritative health/lifecycle/inventory/removal state without persisting sound commands.

## Out-of-scope guide migration work

No guide migration is included. Do not modify `.guide-profile.json`, copy guide documents/templates, reference external guides as operational authority, or require ordinary implementation agents to read `.guide-sync/`.
