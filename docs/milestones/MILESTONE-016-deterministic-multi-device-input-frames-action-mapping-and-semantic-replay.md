# Milestone 016 — Deterministic Multi-Device Input Frames, Action Mapping, and Semantic Replay Foundation

## Goal

Introduce a backend-neutral input model that converts keyboard, mouse, and controller samples into deterministic semantic actions and pointer state bound to simulation ticks, then prove recording and replay through synthetic headless sources.

Required journey:

```text
authored input map
+ synthetic keyboard/mouse/controller samples
→ backend-neutral raw input snapshot
→ deterministic binding evaluation
→ tick-bound semantic InputFrame
→ behavior reads immutable input query
→ normal runtime intent/resolution
→ semantic frames recorded
→ recorded frames replayed without physical devices
→ equivalent intents, events, final state, and render projection
```

M016 implements the engine-level keyboard, mouse, and controller design. It deliberately does not require new physical raylib mouse/controller setup.

## Repository role and maturity assumptions

Repository role: `capability-provider`.

The repository supplies reusable runtime, content, tooling, rendering, and validation capability. Input maps, scripted samples, movement, interaction, and replay scenarios are bounded dogfood, not a consumer game.

Assumptions:

- implementation-ready, artifact-first, headless-first, CLI/API-first;
- M000–M015 and accepted completion patches are implemented;
- fixed runtime ticks, immutable snapshots, behavior intents, continuous movement, interactions, render projection, and the isolated raylib client exist;
- raylib debug controls are client-local and are not gameplay input authority;
- stable-ID, ordering, diagnostics, content-validation, scenario, artifact, and review conventions remain authoritative.

## Execution mode

`ai-executed-broad`

Implement contracts, authored content, synthetic sources, tick semantics, runtime integration, evidence, and replay before any optional live adapter bridge.

## Locked design decisions

- Gameplay input uses semantic actions, never platform enums.
- Keyboard, mouse, and controller are supported by the backend-neutral design.
- New physical raylib mouse/controller integration is excluded from acceptance.
- Action value types are `digital`, `scalar`, and `vector2`.
- Pointer state is a separate channel.
- Physical device ID, logical input-source ID, and future player ID are distinct.
- M016 supports one logical gameplay source: `input-source.player-1`.
- Input maps and scripted sequences are authored structured content.
- Digital bindings combine by OR.
- Scalar bindings select greatest absolute magnitude, preserving sign; exact ties use binding ID ordinal.
- Vector bindings sum and clamp magnitude to `1.0`.
- Controller sticks use radial dead zones; scalar axes use axial dead zones.
- Values above dead zones are rescaled across the remaining range.
- Raw samples are provenance/diagnostic data.
- Consumed semantic `InputFrame` records are replay authority.
- Exactly one semantic frame is consumed for each applicable simulation tick.
- Press/release edges accumulate until consumed; held state persists.
- While paused, gameplay edges and impulses remain queued for the next step/resume.
- Reset clears all previous, held, queued, analog, pointer, and replay state.
- Debug-client controls remain client-local and separate from gameplay actions.
- Behaviors read immutable input frames and emit normal intents.
- Providers and mappings never mutate runtime stores.
- Pointer spaces are `window`, `logical-viewport`, and `world`.
- Outside-viewport pointer positions are flagged, not clamped.
- Mouse buttons may bind to digital actions; pointer position remains separate.
- Controller hot-plug is representable but physical hot-plug support is deferred.
- Semantic recording and deterministic playback are included.
- Multiplayer, remapping UI, network input, rollback, text input, and feedback are excluded.

## Scope

1. Backend-neutral raw input sample model.
2. Device and logical source identity.
3. Authored input maps.
4. Keyboard, mouse, and controller bindings.
5. Digital, scalar, and vector2 actions.
6. Composite keyboard vectors.
7. Pointer state and coordinate transforms.
8. Dead-zone/scaling and binding combination.
9. Presentation-frame accumulator.
10. Tick-bound `InputFrame`.
11. Pressed/held/released phase calculation.
12. Running, paused, stepped, completed, and reset semantics.
13. Synthetic keyboard/mouse/controller sources.
14. Scripted raw-sample and semantic-frame sequences.
15. Immutable runtime input query.
16. Movement via `action.move`.
17. Interaction via `action.interact`.
18. Semantic frame recording.
19. Semantic replay and equivalence validation.
20. Input artifacts and runtime-inspection linkage.
21. Optional thin bridge from existing raylib keyboard state.
22. Bounded mixed-device smoke scenarios.

## Non-goals

Do not implement physical raylib mouse/controller polling, controller discovery/mapping databases, live hot-plug, device glyphs, rumble, touch, gestures, pen input, raw mouse, cursor capture/warping, mouse targeting, text input/IME, chords, action contexts, remapping UI, accessibility UI, local multiplayer, ownership transfer, network input, rollback/prediction, latency compensation, full state replay, save/load, animation, audio, packaging, workflows, TBPs, issue templates, public docs, release docs, or guide migration.

## Focus areas

### 1. Identity and raw sample model

Distinct concepts:

```text
physicalDeviceId
inputSourceId
playerId
```

Required source:

```text
input-source.player-1
```

Supported device kinds:

```text
keyboard
mouse
controller
synthetic
replay
```

Raw sample batches include stable sequence, source/device identity, presentation sample index, control values, coordinate space, and diagnostics. Raw sampling may be more frequent than runtime ticks.

### 2. Authored input-map domain

Canonical root:

```text
game/input/maps/
```

Required map:

```text
input-map.player.default
```

Required actions:

```text
action.move       vector2
action.interact   digital
action.zoom       scalar
```

Required binding kinds:

```text
keyboard-key
mouse-button
mouse-wheel-x
mouse-wheel-y
controller-button
controller-axis
controller-stick
composite-vector2
```

Authored controls use semantic names, not raylib enum identifiers.

### 3. Input-map validation

Validate schema, stable IDs, unique action/binding IDs, action/binding type compatibility, semantic controls, finite scales, dead zones in `[0,1)`, composite structure, and deterministic canonical ordering.

Stable diagnostic categories must cover invalid schema, duplicates, unsupported bindings, type mismatch, dead zone, scale, control name, and source identity.

### 4. Semantic controls

Mouse buttons:

```text
primary secondary middle auxiliary-1 auxiliary-2
```

Controller buttons:

```text
south east west north
left-shoulder right-shoulder
left-stick right-stick
start select
dpad-up dpad-down dpad-left dpad-right
```

Controller axes:

```text
left-stick-x left-stick-y
right-stick-x right-stick-y
left-trigger right-trigger
```

Keyboard names use stable conventional names such as `W`, `A`, `S`, `D`, `E`, `Enter`, and arrows.

### 5. Action values and combination

Digital phases:

```text
inactive pressed held released
```

Digital values are `0.0` or `1.0`.

Scalar values are finite in `[-1,1]`. Wheel bindings are one-tick impulses.

Vector components are finite in `[-1,1]`; final magnitude is clamped to `1.0`.

Combination:

```text
digital: OR
scalar: greatest absolute magnitude, binding-ID tie-break
vector2: sum then clamp
```

### 6. Dead zones

Sticks use radial dead zones:

```text
magnitude < deadZone → zero
rescaledMagnitude = (magnitude - deadZone) / (1 - deadZone)
```

Scalar axes use axial dead zones with equivalent rescaling.

Test zero, below boundary, boundary, above boundary, maximum, negative scalar, and non-finite rejection.

### 7. Pointer channel

Required pointer:

```text
pointer.primary
```

Fields include source/device IDs, absolute position, delta, wheel X/Y, space, `insideViewport`, optional world position, and diagnostics.

M015 conversion:

```text
logicalX = (windowX - letterboxOffsetX) / integerScale
logicalY = (windowY - letterboxOffsetY) / integerScale
```

Outside viewport remains represented and is never silently clamped. World conversion uses read-only camera data.

### 8. Input accumulator

The accumulator:

- retains current down/held and latest analog state;
- queues press/release edges;
- accumulates pointer delta and wheel impulses;
- produces one semantic frame when a tick requests it;
- consumes queued one-shot state after frame production;
- preserves held and latest analog state.

Presentation samples never execute gameplay directly.

### 9. Tick-bound `InputFrame`

Required fields:

- schema/version;
- tick and frame sequence;
- source ID;
- input-map ID/revision;
- digital/scalar/vector actions;
- pointer states;
- provenance and diagnostics.

The runtime exposes the frame immutably to behavior code. Missing actions resolve to inactive/zero.

### 10. Runtime-mode semantics

Running consumes one frame per tick.

Paused retains pending gameplay state without consumption.

Step-one consumes one frame.

Step-ten consumes ten frames; queued edges/impulses occur only in the first frame unless new samples are inserted.

Completed scenarios consume no additional frames.

Reset clears previous phases, held state, queued edges, pointer/wheel accumulators, axes, recording buffer, and replay cursor.

### 11. Synthetic sources and authored sequences

Canonical root:

```text
game/input/sequences/
```

Required raw sequence:

```text
input-sequence.mixed-device-approach-and-interact
```

It must prove keyboard movement, pointer movement, mouse interaction, wheel impulse, controller dead zone, controller movement, controller interaction, multiple bindings, and deterministic ordering.

Required semantic sequence:

```text
input-frames.player-approach-and-interact
```

Raw sequences validate mapping; semantic sequences isolate runtime consumption and replay.

### 12. Runtime integration

Add immutable input queries.

`action.move` drives the existing movement behavior/intent path.

`action.interact` drives `behavior.player-interact` and the existing `InteractIntent`.

No input component/provider/mapping may directly mutate transforms, interactions, or stores.

Equivalent semantic actions from different synthetic devices must produce equivalent runtime outcomes where semantics match.

### 13. Semantic recording and replay

Record every consumed semantic frame.

Replay flow:

```text
run from synthetic raw samples
→ record semantic frames
→ reset/recreate normal scenario initial state
→ replay semantic frames
→ compare outcomes
```

Compatibility checks include scenario/content/runtime identity, input-map revision, tick/seed assumptions.

Replay equivalence covers frames, intents, movement/interaction resolutions, commands, events, final component state, assertions, and final render-projection fingerprint.

Replay requires no raylib, window, keyboard, mouse, or controller.

### 14. Optional raylib bridge

An optional bridge may translate the existing live keyboard path into backend-neutral raw keyboard samples.

It must remain in:

```text
src/Agentic2D.DebugClient.Raylib
```

Physical mouse/controller polling and hot-plug remain excluded. Synthetic tests are the primary evidence for all device kinds.

### 15. Required scenarios

```text
input.mapping-mixed-device-smoke
input.runtime-approach-and-interact-smoke
input.semantic-replay-smoke
```

The mapping smoke validates raw-to-semantic resolution.

The runtime smoke moves the player and initiates the existing interaction.

The replay smoke records the synthetic run, replays it, and proves runtime plus render equivalence.

### 16. Artifacts

Required:

```text
input-map.json
raw-input-samples.jsonl
input-action-resolutions.jsonl
input-frames.jsonl
input-recording.json
input-replay-result.json
input-diagnostics.json
```

Inspection links sample → binding → action → frame → behavior → intent → resolution → event/state → render projection.

### 17. Engineering wrappers

Add:

```bash
./eng/input-content-smoke.sh
./eng/input-mapping-smoke.sh
./eng/input-runtime-smoke.sh
./eng/input-replay-smoke.sh
./eng/m016-smoke.sh
```

All required wrappers are headless. New physical mouse/controller setup is not an acceptance gate.

## Implementation constraints

- Core input contracts contain no raylib types.
- Authored mappings contain no backend enum names.
- Raw collection and semantic resolution are separate.
- Mapping/providers never mutate runtime.
- Runtime tick is authoritative; presentation frame is diagnostic.
- One frame is consumed per applicable tick.
- Edge and impulse accumulation is deterministic.
- Debug controls remain separate.
- Synthetic sources are primary proof.
- Semantic frames are replay authority.
- Replay starts from normal authored initial state.
- Existing behavior, spatial, interaction, and rendering boundaries remain unchanged.
- Ordinary implementation agents must not read `.guide-profile.json`, `.guide-sync/`, copied guides, prompt templates, or the external guide repository.

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
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/continuous-kinematic-spatial-module-contract.md
docs/specs/entity-definition-and-instantiation-contract.md
docs/specs/spatial-query-and-trigger-contract.md
docs/specs/interaction-runtime-contract.md
docs/specs/render-projection-contract.md
docs/specs/raylib-debug-client-contract.md
docs/specs/input-action-map-contract.md
docs/specs/tick-bound-input-frame-contract.md
docs/specs/semantic-input-recording-and-replay-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/entity-instantiation-query-trigger-interaction-artifact-contract.md
docs/artifacts/render-projection-artifact-contract.md
docs/artifacts/input-execution-and-replay-artifact-contract.md
docs/decisions/ADR-0015-behaviors-emit-intents-and-spatial-modules-resolve-them.md
docs/decisions/ADR-0017-authored-definitions-instantiate-runtime-entities-and-interactions-use-explicit-intents.md
docs/decisions/ADR-0018-rendering-is-read-only-and-raylib-is-an-isolated-adapter.md
docs/decisions/ADR-0019-input-is-tick-bound-semantic-data-and-replay-uses-resolved-frames.md
docs/milestones/MILESTONE-014-authored-entity-definitions-runtime-instantiation-spatial-queries-triggers-and-interactions.md
docs/milestones/MILESTONE-015-read-only-rendering-projection-and-raylib-cs-debug-client.md
docs/milestones/MILESTONE-016-deterministic-multi-device-input-frames-action-mapping-and-semantic-replay.md
```

Do not read external guide documents.

## Files or areas likely affected

Recommended project:

```text
src/Agentic2D.Input
```

Likely existing areas:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Behaviors
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Rendering
src/Agentic2D.Tools
tests/unit/Agentic2D.Tests.Unit
```

Optional bridge only:

```text
src/Agentic2D.DebugClient.Raylib
```

Authored content:

```text
game/input/maps/input-map.player.default.json
game/input/sequences/input-sequence.mixed-device-approach-and-interact.json
game/input/sequences/input-frames.player-approach-and-interact.json
game/scenarios/smoke/input-mapping-mixed-device-smoke.json
game/scenarios/smoke/input-runtime-approach-and-interact-smoke.json
game/scenarios/smoke/input-semantic-replay-smoke.json
```

## Validation tiers and concrete commands

Focused tests must cover maps, combination, dead zones, pointer transforms, accumulation, pause/step/reset, recording, compatibility, and replay.

Required headless smokes:

```bash
./eng/input-content-smoke.sh
./eng/input-mapping-smoke.sh
./eng/input-runtime-smoke.sh
./eng/input-replay-smoke.sh
```

Required final gate:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/content-validate.sh maps
./eng/content-validate.sh entities
./eng/content-validate.sh visuals
./eng/content-validate.sh input-maps
./eng/content-validate.sh input-sequences
./eng/m014-smoke.sh
./eng/m015-smoke.sh
./eng/m016-smoke.sh
```

Expected direct command shapes:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate input-maps --output artifacts/content/input-maps
dotnet run --project src/Agentic2D.Tools -- content validate input-sequences --output artifacts/content/input-sequences

dotnet run --project src/Agentic2D.Tools -- input inspect \
  input-sequence.mixed-device-approach-and-interact \
  --input-map input-map.player.default \
  --output artifacts/input/mapping-smoke

dotnet run --project src/Agentic2D.Tools -- scenario run \
  input.runtime-approach-and-interact-smoke \
  --output artifacts/scenarios/input-runtime-smoke

dotnet run --project src/Agentic2D.Tools -- input replay \
  --scenario input.runtime-approach-and-interact-smoke \
  --recording artifacts/input/mapping-smoke/input-recording.json \
  --output artifacts/input/replay-smoke

dotnet run --project src/Agentic2D.Tools -- render project \
  --scenario input.runtime-approach-and-interact-smoke \
  --tick final \
  --output artifacts/render/input-runtime-smoke
```

Use current CLI conventions if exact parsing differs.

## Acceptance criteria

### Content and identity

1. Required maps/sequences validate.
2. Device, source, and future player identities are distinct.
3. Only `input-source.player-1` is active.
4. Authored controls contain no raylib enums.
5. Invalid content yields stable diagnostics.

### Mapping

6. Digital phases work.
7. Scalar/vector ranges and clamping work.
8. Keyboard composite, mouse button/wheel, controller button/axis/stick resolve synthetically.
9. Combination policies and tie-breaks are deterministic.
10. Resolution is independent of sample collection order.

### Dead zones and pointer

11. Radial and axial dead zones/rescaling cover boundaries.
12. Non-finite values reject.
13. Letterbox/scale pointer conversion is correct.
14. Outside viewport is flagged, not clamped.
15. World pointer conversion is read-only.
16. Pointer remains separate from actions.

### Tick semantics

17. More than one presentation sample may precede a tick.
18. Exactly one semantic frame is consumed per applicable tick.
19. Edges persist until consumed; held state persists.
20. Pointer/wheel impulses consume once.
21. Pause, step-one, step-ten, completion, and reset match the contract.
22. Debug controls remain separate.

### Runtime

23. Behaviors read immutable semantic frames.
24. Providers/mappers do not mutate stores.
25. `action.move` uses normal movement resolution.
26. `action.interact` uses existing interaction resolution.
27. Equivalent semantic actions from different synthetic devices yield equivalent outcomes.

### Recording/replay

28. Consumed semantic frames are recorded.
29. Raw samples are not replay authority.
30. Replay requires no physical devices or raylib.
31. Compatibility mismatches reject clearly.
32. Frames, intents, resolutions, commands, events, final state, assertions, and final render fingerprint are equivalent.
33. Volatile machine data is excluded.

### Evidence and scope

34. Required artifacts contain real records.
35. Inspection linkage is complete.
36. Review packs expose input evidence.
37. All required wrappers run headlessly.
38. New physical mouse/controller support is not required or implied.
39. M015 and earlier regressions pass.
40. ADR-0019 and M016 are indexed after acceptance.
41. No excluded remapping, multiplayer, network, rollback, text, save/load, animation, audio, packaging, guide, workflow, TBP, issue-template, public-doc, or release work is introduced.

## Direct documentation impact

Update only where current truth would otherwise be false or undiscoverable:

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
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/raylib-debug-client-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/render-projection-artifact-contract.md
```

Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-14-m016-index-and-crosslink-sync.md
.guide-sync/pending/2026-07-14-m016-human-review-and-input-boundary-followup.md
```

The implementation agent must not read these files.

## Human review requirements

Reviewers verify semantic/backend separation, identity boundaries, one-frame-per-tick semantics, edge retention, pause/step/reset, pointer spaces, dead zones, deterministic combination, runtime non-mutation, semantic replay authority, strict equivalence, and that synthetic support does not falsely imply physical mouse/controller integration.

## Out-of-scope guide migration work

No guide migration is included. Do not change `.guide-profile.json`, copy guide material, reference external guides as operational authority, or require implementation agents to read `.guide-sync/`.
