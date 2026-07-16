# Milestone 021 — Deterministic Presentation Effects, Camera, Declarative UI, Text, and Semantic Interaction Surfaces

## Goal

Add a complete deterministic player-facing presentation layer over existing authoritative runtime state.

Required journey:

```text
persistent-world runtime state and domain events
→ deterministic presentation effects and particles
→ read-only camera projection
→ declarative UI and text projection
→ semantic interaction prompts
→ composed world and screen render evidence
→ save/load reconstruction without replaying transient feedback
```

The final consumer journey must make the M019–M020 gameplay slice understandable and playable through structural evidence and optional graphical review without allowing presentation systems to become gameplay authority.

## Repository role and maturity assumptions

Repository role:

```text
capability-provider
```

The repository implements reusable presentation capabilities and validates them through focused provider tests. It also maintains one bounded consumer-style project journey proving camera, effects, HUD, text, prompts, save/load reconstruction, rendering, and review.

Maturity assumptions:

- implementation-ready and artifact-first;
- headless-first, CLI/API-first, and deterministic;
- Milestones 000 through 020 and accepted completion patches are implemented;
- M019 provides sound, health, lifecycle, inventory, collection, and post-commit gameplay events;
- M020 provides canonical save/load, persistent flags, switches, doors, and resume equivalence;
- animation and sound are already typed presentation projections;
- rendering is read-only and backend-neutral;
- screenshots and physical output are review evidence rather than semantic authority;
- project-aware M018 unified runs and review packs are available;
- authored content uses stable IDs and structured validation.

## Execution mode

```text
ai-executed-broad
```

Implement through six sequential gates:

1. presentation-effect definitions, requests, instances, and deterministic lifetime;
2. bounded deterministic particle projection;
3. read-only camera definitions, follow, bounds, pixel snapping, and shake;
4. declarative UI, finite semantic bindings, text resources, fonts, and deterministic layout;
5. semantic interaction surfaces and structured prompt/rejection projection;
6. integrated saved-and-resumed player-facing coherence journey.

Do not begin a later gate until focused validation for the preceding gate passes.

## Locked design decisions

### Presentation authority

- effects, particles, camera, UI, text, and prompts are presentation only;
- they consume immutable runtime snapshots, post-commit domain events, interaction results, and authored presentation definitions;
- they cannot mutate gameplay state, evaluate gameplay conditions independently, issue damage or collection commands, change flags, open doors, or alter persistence;
- structural projection artifacts are semantic evidence;
- screenshots, visible motion, and physical output are human-review evidence.

### Effects

- effect definitions use stable IDs;
- effect requests retain originating event/request identity;
- effect instances use explicit IDs, start tick, duration ticks, and deterministic seed;
- effect lifetime advances only from runtime ticks;
- effects may produce sprite/render patches, particle emitters, camera requests, UI notifications, and existing sound cue requests;
- effect requests do not duplicate gameplay events;
- pre-save transient effects are not recreated after load;
- effects crossing a save boundary are discarded in M021 rather than persisted;
- no effect or particle state is added to canonical saves.

### Particles

- particles are CPU-side deterministic presentation records;
- particle count is fixed by the authored definition;
- all variation derives from stable seed inputs;
- particles use bounded linear motion and authored scalar curves;
- no particle collision, physics integration, GPU simulation, force fields, or arbitrary scripts;
- particle instances are projected to ordinary backend-neutral render items.

### Camera

- camera is read-only presentation state;
- it never changes entity transforms, spatial queries, collision, targeting, or interaction;
- camera uses runtime ticks, not variable frame time;
- initial policies are immediate follow and fixed-tick bounded interpolation;
- camera supports a dead zone, world bounds, logical viewport, pixel snapping, deterministic temporary offset, and bounded shake;
- shake derives from presentation requests and never affects gameplay coordinates;
- camera state is reconstructed after load from current authoritative state and authored camera definition;
- active pre-save shake is not resumed.

### UI and text

- UI is an authored declarative tree;
- UI supports a bounded element vocabulary;
- UI bindings use a finite registered semantic vocabulary;
- arbitrary property paths, reflection, scripts, callbacks, and expressions are forbidden;
- UI reads prepared projection values rather than gameplay stores directly;
- text uses stable text resource IDs;
- font resources are authored metadata referencing raw font assets;
- text layout is deterministic in logical pixels;
- runtime gameplay definitions do not embed player-facing display strings where text resource IDs are appropriate;
- M021 does not implement localization, but text-resource identity must permit it later.

### Interaction surfaces

- prompts consume the authoritative interaction candidate and structured resolution/condition evidence;
- UI does not reevaluate door conditions, inventory requirements, flags, or lifecycle;
- prompt state includes action ID, target ID, text resource ID, enabled state, structured reason, priority, and runtime tick;
- disabled prompts explain authoritative rejection reasons;
- input display uses semantic action identity, not hard-coded device keys;
- UI actions remain read-only in M021; no menus or clickable gameplay controls.

### Composition

- world render items, animated render items, and world-space effects are transformed through the camera;
- screen-space UI and text are not transformed by the world camera;
- composition uses explicit layers and stable ordering;
- final render fingerprints include world, effects, camera, UI, and text projections;
- adapter-specific glyph or texture handles do not affect semantic fingerprints.

## Scope

### Gate 1 — Presentation effects

1. Authored effect definitions.
2. Stable effect IDs.
3. Effect requests.
4. Effect instances.
5. Runtime-tick lifetime.
6. Deterministic seeds.
7. World-space and screen-space effect domains.
8. Sprite/render patches.
9. Camera-effect requests.
10. UI-notification requests.
11. Existing sound-cue linkage.
12. Event-to-effect mappings.
13. Effect validation.
14. Effect projection artifacts and diagnostics.

### Gate 2 — Particles

15. Bounded particle-emitter definitions.
16. Fixed particle counts.
17. Deterministic spawn records.
18. Initial position ranges.
19. Velocity ranges.
20. Scale, rotation, tint, and opacity ranges.
21. Scalar lifetime curves.
22. Linear per-tick motion.
23. Particle render-item projection.
24. Particle fingerprints.
25. Replay equivalence.

### Gate 3 — Camera

26. Authored camera definitions.
27. Camera target selectors.
28. Logical viewport.
29. Immediate follow.
30. Fixed-tick bounded interpolation.
31. Dead zone.
32. World bounds.
33. Pixel snapping.
34. Temporary offset.
35. Bounded deterministic shake.
36. Camera state projection.
37. Camera-transformed world projection.
38. Camera artifacts and diagnostics.
39. Post-load reconstruction.

### Gate 4 — Declarative UI and text

40. Authored UI documents.
41. Screen-space root.
42. Panel.
43. Text.
44. Image.
45. Horizontal stack.
46. Vertical stack.
47. Progress bar.
48. Visibility.
49. Fixed padding and spacing.
50. Anchors.
51. Explicit z/layer ordering.
52. Finite semantic binding registry.
53. Projection-value preparation.
54. Authored text resources.
55. Authored font resources.
56. Deterministic glyph/layout projection.
57. UI render items and text commands.
58. UI/text artifacts and diagnostics.

### Gate 5 — Semantic interaction surfaces

59. Prompt projection model.
60. Current interaction candidate consumption.
61. Enabled interaction prompts.
62. Disabled prompts with structured reasons.
63. Semantic input action display.
64. Stable prompt priority.
65. Collection prompt.
66. Switch prompt.
67. Locked-door condition feedback.
68. Open-door prompt.
69. Post-action notification.
70. Prompt artifacts and diagnostics.

### Gate 6 — Integrated player-facing journey

71. Focused effect scenario.
72. Focused particle scenario.
73. Focused camera scenario.
74. Focused UI/text scenario.
75. Focused interaction-prompt scenario.
76. Integrated persistent-world presentation scenario.
77. Save/load reconstruction scenario.
78. Semantic replay equivalence.
79. Final composed render fingerprint.
80. Unified run and review integration.
81. Generated consumer-workspace dogfood.

## Non-goals

Do not implement:

### Effects and particles

- GPU particle systems;
- particle collision;
- physics particles;
- arbitrary emitters;
- force fields;
- fluid/smoke simulation;
- trails or ribbons;
- decals;
- dynamic lights;
- shaders;
- post-processing;
- bloom;
- screen distortion;
- procedural materials;
- effect scripting;
- effect graphs;
- effect persistence across save/load.

### Camera

- cinematic timelines;
- multiple simultaneous cameras;
- split-screen;
- perspective or 3D cameras;
- zoom animation beyond an authored fixed zoom if needed;
- camera collision;
- look-ahead AI;
- cutscene systems;
- frame-time interpolation;
- gameplay-authoritative camera state.

### UI

- CSS;
- flexbox/grid clone;
- arbitrary constraints;
- responsive web layout;
- rich text;
- markdown rendering;
- text editing;
- scrolling;
- virtualization;
- focus navigation;
- controller menu navigation;
- mouse/touch buttons;
- inventory management UI;
- equipment UI;
- save-slot UI;
- menus;
- settings;
- modal dialogs;
- drag-and-drop;
- UI scripting;
- arbitrary callback binding;
- arbitrary property-path binding;
- UI animation graphs.

### Text and localization

- localization runtime;
- language packs;
- pluralization;
- bidirectional text;
- advanced shaping;
- fallback font stacks;
- runtime font downloads;
- font editor;
- arbitrary system-font discovery.

### General

- gameplay mutation from presentation;
- new combat, inventory, persistence, door, switch, or condition semantics;
- package publishing;
- portable SDK;
- standalone game packaging;
- editor UI;
- networking;
- broad unrelated documentation cleanup;
- workflows, TBPs, issue templates, public docs, release docs, or guide migration.

## Focus areas

## Gate 1 — Presentation effects

### 1. Content root and required definitions

Add:

```text
game/effects/
```

Required definitions:

```text
effect.damage-feedback
effect.collection-burst
effect.switch-activation
effect.door-open
effect.save-confirmation
```

An effect definition may declare:

- schema;
- stable effect definition ID;
- duration ticks;
- domain: world or screen;
- visual/render patches;
- optional particle emitter references;
- optional camera request;
- optional UI notification request;
- optional existing sound cue ID;
- tags;
- provenance.

### 2. Effect requests and instances

An effect request records:

- request ID;
- definition ID;
- source event ID;
- source entity ID if applicable;
- target entity ID if applicable;
- runtime tick;
- world or screen anchor;
- occurrence ordinal;
- deterministic seed context;
- provenance.

An effect instance records:

- stable instance ID;
- definition ID;
- source request/event;
- started-at tick;
- duration ticks;
- seed;
- current age;
- active/completed state;
- emitted child requests;
- fingerprint.

Instance ID derives from stable request identity rather than process sequence.

### 3. Required event mappings

```text
entity.damaged   → effect.damage-feedback
item.collected   → effect.collection-burst
switch.activated → effect.switch-activation
door.opened      → effect.door-open
save.created     → effect.save-confirmation
```

`save.created` is presentation-facing evidence derived from successful save completion. It does not become gameplay state.

### 4. Save/load policy

Transient effects are excluded from canonical persistence.

After loading:

- authoritative world visuals reconstruct;
- health/inventory/door/switch UI reconstructs;
- pre-save effects do not restart;
- pre-save notifications do not reappear;
- pre-save camera shake does not resume;
- new post-load events may create new effects normally.

## Gate 2 — Deterministic particles

### 5. Initial particle definition

Fields:

- stable emitter ID;
- visual definition/part ID;
- fixed particle count;
- emitter duration ticks;
- particle lifetime ticks;
- spawn offset bounds;
- initial velocity bounds;
- initial scale bounds;
- initial rotation bounds;
- angular velocity bounds;
- tint/opacity bounds;
- scale curve;
- opacity curve;
- layer;
- deterministic ordering.

Initial supported curves:

```text
constant
linear
linear-inverse
```

No arbitrary curve scripting.

### 6. Deterministic seed

Seed derives from:

```text
project/scenario seed
effect definition ID
effect instance ID
source event ID
runtime tick
particle ordinal
```

Equivalent replay must produce identical particle records and render fingerprints.

### 7. Motion

Initial motion:

```text
position(tick) = initial position + velocity * age ticks
rotation(tick) = initial rotation + angular velocity * age ticks
```

Use existing deterministic numeric policy.

Particles do not query collision or runtime entities after creation except for an optional fixed spawn anchor resolved at creation.

## Gate 3 — Camera

### 8. Camera content root

Add:

```text
game/cameras/
```

Required definition:

```text
camera.player-follow
```

Fields:

- schema;
- stable camera ID;
- target selector;
- logical viewport width/height;
- follow policy;
- interpolation-per-tick where applicable;
- dead-zone rectangle;
- world bounds or map bounds reference;
- pixel-snap policy;
- zoom, initially fixed;
- layer/projection policy;
- provenance.

### 9. Follow semantics

Initial policies:

```text
immediate
bounded-linear-per-tick
```

No variable-frame-time smoothing.

Target resolution must be stable. Missing or ambiguous targets produce diagnostics.

Dead-zone behavior is deterministic and tested at boundaries.

### 10. Camera shake

A shake request contains:

- request ID;
- source effect/event;
- start tick;
- duration ticks;
- maximum x/y offset;
- frequency policy;
- deterministic seed.

Shake offset is bounded and deterministic per tick.

It affects only rendered world projection.

### 11. Camera composition

Order:

```text
authoritative world positions
→ animation/effect world patches
→ camera transformation and clipping
→ world render layers
→ screen-space UI/text layers
```

UI never receives world-camera transforms.

## Gate 4 — Declarative UI and text

### 12. Content roots

Add:

```text
game/ui/
game/text/
game/fonts/
```

Required UI document:

```text
ui.player-hud
```

Required text resources include:

```text
text.prompt.collect-crystal
text.prompt.activate-switch
text.prompt.open-door
text.prompt.locked-door
text.notification.item-collected
text.notification.switch-activated
text.notification.door-opened
text.notification.save-created
```

Required font metadata:

```text
font.ui.default
```

### 13. UI element vocabulary

Required elements:

```text
root
panel
text
image
horizontal-stack
vertical-stack
progress-bar
```

Common properties:

- stable element ID;
- visibility binding;
- anchor;
- offset;
- width/height where applicable;
- padding;
- spacing;
- layer;
- child order;
- style references or bounded inline values.

Do not introduce unrestricted style cascading.

### 14. Finite semantic binding vocabulary

Initial bindings:

```text
player.health.current
player.health.maximum
player.health.normalized
player.inventory.distinct-count
player.inventory.item-count:<item-id>
interaction.current.present
interaction.current.enabled
interaction.current.text-id
interaction.current.reason-id
notification.current.present
notification.current.text-id
save.last-status
door:<entity-id>.state
switch:<entity-id>.state
```

Parameterized IDs are validated structured bindings, not arbitrary paths.

A binding provider projects immutable values from runtime and presentation state.

Bindings cannot mutate gameplay.

### 15. Text and font projection

Text resource:

- stable text ID;
- default string value;
- tags;
- provenance.

Font resource:

- stable font ID;
- raw asset reference;
- size/metrics policy;
- glyph set policy;
- provenance.

Headless projection produces deterministic text runs/glyph placement or an equivalent adapter-neutral text-command form.

Native glyph atlas handles are adapter-specific and excluded from semantic fingerprints.

### 16. Layout

Layout uses logical pixels.

Required deterministic rules:

- fixed child order;
- explicit stack orientation;
- fixed padding/spacing;
- anchors against logical viewport;
- explicit measured text policy;
- stable rounding;
- stable z/layer order.

No content-dependent platform font measurement may alter headless fingerprints. Use checked-in font metrics, deterministic adapter-neutral measurement, or another explicit repository-local policy.

## Gate 5 — Semantic interaction surfaces

### 17. Prompt model

Required fields:

- prompt ID;
- action ID;
- target entity ID;
- text resource ID;
- enabled;
- structured reason ID;
- semantic input action ID;
- priority;
- runtime tick;
- source interaction candidate/result;
- fingerprint.

Required examples:

```text
Collect crystal
Activate switch
Locked — requires crystal and vault power
Open door
```

### 18. Authority

Prompt projection consumes:

- current authoritative interaction candidate;
- interaction target selection evidence;
- condition evaluation evidence;
- structured rejection reason;
- semantic input mapping.

It must not recompute door conditions or inventory state independently.

### 19. Notifications

Initial notification model:

- stable request/instance ID;
- text resource ID;
- start tick;
- duration ticks;
- priority;
- source event;
- completed state.

Only one current notification is required. Resolve concurrent requests deterministically by priority, tick, and ID.

Notifications are transient and not persisted.

## Gate 6 — Integrated journey

### 20. Required focused scenarios

```text
presentation.effect-damage-smoke
presentation.particle-collection-smoke
presentation.camera-follow-shake-smoke
presentation.ui-hud-smoke
presentation.interaction-prompt-smoke
presentation.persistent-world-player-facing-smoke
presentation.post-load-reconstruction-smoke
```

### 21. Integrated scenario

Use the M020 persistent-world journey:

Initial state:

- active player;
- health component;
- empty inventory;
- collectible crystal;
- inactive switch;
- locked door;
- active door collision;
- player-follow camera;
- player HUD;
- deterministic input and seed.

Journey:

```text
move player
→ camera follows
→ interaction prompt shows collection

collect crystal
→ inventory changes
→ item removed
→ collection effect/particles
→ notification
→ HUD updates

receive damage
→ health changes
→ damage animation/sound
→ damage effect
→ bounded camera shake
→ health bar updates

approach locked door
→ authoritative condition rejection
→ disabled prompt explains requirement

activate switch
→ flag changes
→ switch effect/notification
→ prompt state changes

open door
→ collision changes
→ animation/sound/effect
→ prompt updates

save
→ save confirmation notification
→ destroy runtime
→ load fresh runtime
→ authoritative state restores
→ camera/HUD/prompts/world visuals reconstruct
→ old particles, shake, and notifications do not replay
→ continue through door
```

### 22. Replay and resume equivalence

Equivalent replay must produce identical:

- effect requests;
- effect instances;
- particle spawn records;
- camera states per tick;
- UI binding values;
- UI layout records;
- text commands;
- prompt states;
- notification arbitration;
- composed render fingerprints.

Save/load comparison distinguishes:

```text
authoritative state equivalence
presentation reconstruction equivalence
transient presentation intentionally absent
```

## Implementation constraints

- Presentation code never mutates runtime state.
- Effects derive from post-commit events.
- UI never reevaluates gameplay conditions.
- Binding vocabulary is finite and registered.
- Runtime ticks are the only authoritative presentation time.
- Randomness is deterministic and seed-derived.
- World and screen coordinate spaces are explicit.
- Camera affects rendering only.
- UI uses logical viewport coordinates.
- Layout measurement is deterministic.
- Text uses stable resource IDs.
- Raw display strings are not scattered through gameplay contracts.
- Transient effects, particles, shake, and notifications are excluded from saves.
- Required validation is headless and network-independent.
- Raylib remains an optional adapter.
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
docs/specs/entity-component-runtime-contract.md
docs/specs/entity-definition-and-instantiation-contract.md
docs/specs/deterministic-behavior-runtime-contract.md
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
docs/specs/sound-definition-and-command-contract.md
docs/specs/sound-cue-projection-contract.md
docs/specs/resource-damage-and-lifecycle-contract.md
docs/specs/item-inventory-and-collection-contract.md
docs/specs/gameplay-presentation-event-contract.md
docs/specs/canonical-save-snapshot-contract.md
docs/specs/persistence-contributor-contract.md
docs/specs/save-load-and-resume-contract.md
docs/specs/persistent-flag-and-condition-contract.md
docs/specs/stateful-world-entity-contract.md
docs/specs/mutable-world-projection-contract.md
docs/specs/game-project-manifest-contract.md
docs/specs/game-workspace-manifest-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/specs/presentation-effect-definition-and-projection-contract.md
docs/specs/deterministic-particle-projection-contract.md
docs/specs/camera-definition-and-projection-contract.md
docs/specs/declarative-ui-and-binding-contract.md
docs/specs/text-and-font-resource-contract.md
docs/specs/semantic-interaction-surface-contract.md
docs/specs/player-facing-presentation-composition-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/artifacts/render-projection-artifact-contract.md
docs/artifacts/animation-execution-artifact-contract.md
docs/artifacts/sound-execution-artifact-contract.md
docs/artifacts/gameplay-state-artifact-contract.md
docs/artifacts/save-execution-artifact-contract.md
docs/artifacts/persistent-world-state-artifact-contract.md
docs/artifacts/player-facing-presentation-artifact-contract.md
docs/decisions/ADR-0018-rendering-is-read-only-and-raylib-is-an-isolated-adapter.md
docs/decisions/ADR-0020-animation-produces-typed-presentation-patches.md
docs/decisions/ADR-0022-sound-is-deterministic-presentation-projection.md
docs/decisions/ADR-0024-saves-contain-canonical-authoritative-state.md
docs/decisions/ADR-0025-stateful-world-objects-are-runtime-entities.md
docs/decisions/ADR-0026-player-facing-presentation-is-derived-and-transient.md
docs/decisions/ADR-0027-ui-bindings-use-a-finite-semantic-vocabulary.md
docs/milestones/MILESTONE-019-sound-feedback-gameplay-state-lifecycle-items-and-collection.md
docs/milestones/MILESTONE-020-canonical-save-load-persistent-flags-doors-switches-and-resume-equivalence.md
docs/milestones/MILESTONE-021-deterministic-presentation-effects-camera-declarative-ui-text-and-semantic-interaction-surfaces.md
```

Do not read the external guide repository for implementation.

## Files or areas likely affected

Recommended projects:

```text
src/Agentic2D.Presentation
src/Agentic2D.UI
```

Likely affected:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Entities
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Animation
src/Agentic2D.Sound
src/Agentic2D.Rendering
src/Agentic2D.Persistence
src/Agentic2D.Workspaces
src/Agentic2D.Tools
src/Agentic2D.DebugClient.Raylib
tests/unit/Agentic2D.Tests.Unit
```

Likely content:

```text
game/effects/
game/cameras/
game/ui/
game/text/
game/fonts/
game/assets/raw/samples/
game/assets/metadata/
game/scenarios/smoke/
```

Generated workspace:

```text
game-content/effects/
game-content/cameras/
game-content/ui/
game-content/text/
game-content/fonts/
```

## Required product commands

Add command shapes consistent with current CLI:

```bash
agentic2d presentation inspect   --project <project-or-workspace>   --scenario <scenario-id>   --output <directory>

agentic2d effect inspect <effect-id-or-path> --output <directory>

agentic2d camera inspect   --project <project-or-workspace>   --scenario <scenario-id>   --output <directory>

agentic2d ui inspect <ui-id-or-path>   --project <project-or-workspace>   [--scenario <scenario-id>]   --output <directory>
```

Update `content validate`, `project validate`, `project run`, `project resume`, `run inspect`, `run review`, and `render project` to include the new domains and artifact families automatically.

## Required artifacts

```text
player-facing-presentation-result.json
effect-definitions.json
effect-requests.jsonl
effect-instances.jsonl
particle-emitters.json
particle-instances.jsonl
particle-samples.jsonl
camera-definitions.json
camera-states.jsonl
camera-requests.jsonl
ui-definitions.json
ui-binding-values.jsonl
ui-layout.jsonl
text-resources.json
font-resources.json
text-commands.jsonl
interaction-prompts.jsonl
notifications.jsonl
presentation-composition.jsonl
player-facing-presentation-diagnostics.json
```

Do not emit empty placeholder records for unexercised capabilities.

## Required engineering wrappers

```bash
./eng/presentation-effect-smoke.sh
./eng/presentation-particle-smoke.sh
./eng/presentation-camera-smoke.sh
./eng/presentation-ui-text-smoke.sh
./eng/presentation-interaction-surface-smoke.sh
./eng/presentation-integrated-smoke.sh
./eng/presentation-replay-smoke.sh
./eng/presentation-post-load-smoke.sh
./eng/presentation-review-smoke.sh
./eng/m021-smoke.sh
```

All required wrappers are headless. Optional graphical review remains separate.

## Validation tiers and concrete repository commands

### Tier 1 — Unit and contract tests

Cover effect validation/identity/lifetime, particle determinism, camera target/dead-zone/bounds/snap/shake, UI tree and finite bindings, deterministic layout/text, prompt authority, notification arbitration, composition, and post-load non-replay of transients.

### Tier 2 — Focused headless smokes

```bash
./eng/presentation-effect-smoke.sh
./eng/presentation-particle-smoke.sh
./eng/presentation-camera-smoke.sh
./eng/presentation-ui-text-smoke.sh
./eng/presentation-interaction-surface-smoke.sh
```

### Tier 3 — Integrated and replay

```bash
./eng/presentation-integrated-smoke.sh
./eng/presentation-replay-smoke.sh
./eng/presentation-post-load-smoke.sh
./eng/presentation-review-smoke.sh
```

### Tier 4 — Full milestone and regressions

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
./eng/content-validate.sh effects
./eng/content-validate.sh cameras
./eng/content-validate.sh ui
./eng/content-validate.sh text
./eng/content-validate.sh fonts
./eng/m015-smoke.sh
./eng/m016-smoke.sh
./eng/m017-smoke.sh
./eng/m018-smoke.sh
./eng/m019-smoke.sh
./eng/m020-smoke.sh
./eng/m021-smoke.sh
```

### Expected direct checks

```bash
dotnet run --project src/Agentic2D.Tools -- content validate effects --output artifacts/content/effects
dotnet run --project src/Agentic2D.Tools -- content validate cameras --output artifacts/content/cameras
dotnet run --project src/Agentic2D.Tools -- content validate ui --output artifacts/content/ui

dotnet run --project src/Agentic2D.Tools --   presentation inspect   --project .   --scenario presentation.persistent-world-player-facing-smoke   --output artifacts/presentation/integrated

dotnet run --project src/Agentic2D.Tools --   project run .   --scenario presentation.persistent-world-player-facing-smoke   --output artifacts/runs/m021-integrated

dotnet run --project src/Agentic2D.Tools --   project resume .   --save artifacts/saves/persistent-world/save-snapshot.json   --recording game/input/recordings/presentation-after-load.json   --output artifacts/runs/m021-resumed

dotnet run --project src/Agentic2D.Tools --   run inspect artifacts/runs/m021-resumed   --output artifacts/run-inspect/m021-resumed

dotnet run --project src/Agentic2D.Tools --   run review artifacts/runs/m021-resumed   --output artifacts/review/m021-resumed
```

Use exact current CLI syntax where it differs and update active documentation consistently.

## Acceptance criteria

### Gate 1 — Effects

1. Effect definitions validate.
2. Effect IDs and mappings are stable.
3. Effect requests retain originating event identity.
4. Instance IDs are deterministic.
5. Lifetime advances only by runtime ticks.
6. Damage, collection, switch, door, and save-success mappings work.
7. Effects cannot mutate gameplay.
8. Effect artifacts contain complete provenance.
9. Replay produces equivalent effect instances.
10. Pre-save effects do not restart after load.

### Gate 2 — Particles

11. Particle count is fixed and bounded.
12. Particle seeds are deterministic.
13. Spawn ranges validate.
14. Curves use the bounded supported vocabulary.
15. Motion is deterministic per tick.
16. Particles do not query collision.
17. Particle render-item ordering is stable.
18. Replay produces equivalent particle records and fingerprints.
19. Particles are excluded from canonical saves.
20. Pre-save particles do not reappear after load.

### Gate 3 — Camera

21. Camera definitions validate.
22. Target selection is deterministic.
23. Missing/ambiguous targets diagnose clearly.
24. Immediate follow works.
25. Bounded fixed-tick interpolation works.
26. Dead-zone boundary behavior is stable.
27. World bounds clamp correctly.
28. Pixel snapping is deterministic.
29. Shake is bounded and seed-deterministic.
30. Camera never changes runtime coordinates.
31. Camera never changes collision or targeting.
32. UI is not camera transformed.
33. Camera reconstructs after load.
34. Pre-save shake does not resume.
35. Replay produces equivalent camera-state fingerprints.

### Gate 4 — UI and text

36. UI documents validate.
37. Required element vocabulary works.
38. Unknown elements reject.
39. Bindings use the finite registry.
40. Unknown/arbitrary bindings reject.
41. Binding providers are read-only.
42. Health HUD reflects authoritative health.
43. Inventory HUD reflects authoritative inventory.
44. Visibility bindings work.
45. Layout ordering and rounding are deterministic.
46. Text resources use stable IDs.
47. Font resources use stable metadata and assets.
48. Headless text measurement is deterministic.
49. Native handles do not affect fingerprints.
50. UI/text reconstruct from loaded state.
51. Temporary notifications do not persist.

### Gate 5 — Interaction surfaces

52. Prompt candidate comes from authoritative interaction state.
53. Prompt does not independently evaluate gameplay conditions.
54. Collection prompt appears for collection candidate.
55. Switch prompt appears for inactive switch.
56. Locked-door prompt uses structured rejection evidence.
57. Open-door prompt appears after requirements pass.
58. Semantic action ID is displayed without hard-coded device dependence.
59. Prompt priority/order is deterministic.
60. Notifications arbitrate deterministically.
61. Prompt/notification evidence preserves source linkage.

### Gate 6 — Integrated journey

62. Camera follows the player.
63. HUD displays health and inventory.
64. Collection updates UI and emits effect/particles/notification.
65. Damage updates health UI and emits effect/shake.
66. Locked door displays authoritative requirement feedback.
67. Switch activation changes prompt and emits feedback.
68. Door opening emits animation, sound, effect, and render changes after commit.
69. Save success emits a transient notification.
70. Fresh load restores authoritative state.
71. Camera, HUD, prompts, and persistent world visuals reconstruct.
72. Old effect instances, particles, shake, and notifications do not replay.
73. Player continues through the open door.
74. Unified run links all exercised presentation families.
75. `run inspect` validates presentation evidence.
76. `run review` distinguishes structural evidence from screenshots.
77. Replay produces equivalent structural presentation and final render fingerprints.
78. Provider and bounded consumer validation remain distinct.
79. Required tests remain headless and network-independent.

### Scope protection

80. No gameplay mutation, UI scripting, localization framework, menus, save UI, editor, SDK, packaging, guide migration, TBPs, or issue templates are introduced.
81. Existing M015–M020 gates remain passing.
82. New specs, artifact contract, ADRs, commands, content roots, scenarios, and M021 are indexed after acceptance.

## Direct documentation impact

Update only as required:

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
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/render-projection-contract.md
docs/specs/animated-render-projection-contract.md
docs/specs/sound-cue-projection-contract.md
docs/specs/gameplay-presentation-event-contract.md
docs/specs/save-load-and-resume-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/artifacts/review-pack-artifact-contract.md
```

Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

The package adds:

```text
.guide-sync/pending/2026-07-16-m021-index-roadmap-and-crosslink-sync.md
.guide-sync/pending/2026-07-16-m021-human-review-player-facing-presentation-followup.md
```

Ordinary implementation agents must not read them.

## Human review requirements

Review presentation authority, deterministic visual usefulness, camera comfort, shake bounds, UI readability, semantic binding sufficiency, deterministic text layout, prompt clarity, post-load reconstruction, absence of transient replay, structural-versus-screenshot agreement, and readiness for later consumer extension APIs.

## Out-of-scope guide migration work

No guide migration is included.

Do not modify `.guide-profile.json`, copy guide documents or prompt templates, reference external guides as operational authority, or require ordinary implementation agents to read `.guide-sync/`.
