# Milestone 017 — Deterministic Keyframe Animation, Base/Overlay Composition, Markers, and Animated Render Projection

## Goal

Introduce an agent-first animation system that compiles authored animation definitions into deterministic tick-based presentation patches, composes one base clip with one optional overlay clip, emits presentation-only markers, and feeds animated values into the existing read-only render projection.

Required journey:

```text
authored visual definition
+ authored animation definition
+ immutable runtime snapshot
+ semantic input/replay evidence
→ explicit animation selection
→ deterministic tick sampling
→ base presentation patch
→ optional overlay presentation patch
→ animated visual instance
→ existing render projection
→ structural animation/render evidence
```

Animation is presentation authority only. It must not mutate gameplay state, runtime components, collision, interactions, or entity lifecycle.

## Repository role and maturity assumptions

Repository role: `capability-provider`.

The repository implements reusable runtime, input, rendering, tooling, and authored-content capability. Player/NPC animation fixtures are bounded dogfood, not a consumer game.

Assumptions:

- implementation-ready and artifact-first;
- headless-first and CLI/API-first;
- Milestones 000 through 016 and accepted completion patches are implemented;
- visual definitions, backend-neutral render projection, raylib presentation, semantic input frames, recording, and replay exist;
- existing stable-ID, deterministic-ordering, structured diagnostics, artifact, review, and content-validation rules remain authoritative.

## Execution mode

`ai-executed-broad`

Implement authored contracts, validation, compilation, sampling, selection, composition, markers, headless evidence, and replay equivalence before optional graphical review.

## Locked design decisions

- animation is a typed presentation patch over visual definitions;
- animation does not mutate arbitrary object paths or runtime properties;
- authored targets use stable visual part IDs and a finite property registry;
- one base clip is active for an animated visual instance;
- zero or one overlay clip may also be active;
- composition order is visual-definition defaults, then base replacement, then overlay replacement;
- duplicate target properties inside one clip are invalid;
- base and overlay may target the same property; overlay wins;
- one-shot clips do not automatically return, clear, or select another clip;
- completed one-shot clips hold their final sampled values;
- clearing, replacing, or restarting base/overlay selection is explicit;
- selection uses explicit stable selection keys;
- unchanged selection key preserves playback start;
- changed selection key restarts at the current runtime tick;
- the same clip can be restarted only through a different explicit selection key;
- directional animation uses explicit clip IDs, not variants;
- canonical authored time is integer runtime ticks;
- clip duration is a positive integer number of ticks;
- valid keyframe and marker ticks are `0 .. durationTicks - 1`;
- repeat clips wrap by modulo duration;
- once clips complete when elapsed ticks reach duration and sample the final tick;
- initial track value types are `scalar` and `asset-region`;
- vector properties use separate scalar X/Y tracks;
- color animation uses separate scalar red, green, blue, and opacity tracks;
- initial interpolation modes are `step` and `linear`;
- asset-region tracks support `step` only;
- scalar tracks support `step` and `linear`;
- sprite-sequence shorthand is allowed but compiles to a canonical asset-region step track;
- presentation markers are included;
- markers never directly issue gameplay commands;
- marker crossing is previous-local-time exclusive and current-local-time inclusive;
- multi-tick steps emit every crossed marker in deterministic order;
- repeat-loop marker order is loop iteration, marker tick, marker ID;
- no automatic completion marker is generated;
- selection and sampling are separate responsibilities;
- replay equivalence includes animation selection, sampling, markers, and final render fingerprint;
- raylib remains a draw adapter only.

## Scope

1. Authored animation-definition domain.
2. Stable clip, track, keyframe, and marker IDs.
3. Tick-based clip timelines.
4. Finite presentation property registry.
5. Scalar tracks.
6. Asset-region tracks.
7. Step interpolation.
8. Linear scalar interpolation.
9. Once and repeat loop modes.
10. Sprite-sequence shorthand.
11. Canonical animation compilation.
12. Explicit base selection.
13. Optional overlay selection.
14. Selection keys and restart/preserve semantics.
15. One-shot completion with hold-final.
16. Explicit clear/replace/restart operations.
17. Base/overlay property composition.
18. Presentation-only markers.
19. Deterministic marker crossing across multi-tick steps and loops.
20. Animation selection from immutable runtime state.
21. Player idle/walk directional clips.
22. Bounded interaction overlay.
23. Animated render projection.
24. Headless animation inspection.
25. Animation artifacts and diagnostics.
26. Input-replay-driven animation equivalence.
27. Optional graphical review through the existing raylib client.

## Non-goals

Do not implement arbitrary property paths, runtime component animation, transform/root-motion mutation, method tracks, gameplay commands from markers, automatic clip fallback, animation graphs, blend trees, weighted blending, more than one base or overlay, additive/multiplicative composition, masks, cross-fades, cubic or Bézier interpolation, ping-pong, skeletal animation, bones, IK, particles, audio playback, shader animation, camera animation, UI animation, runtime authoring, physical input integration, save/load, network synchronization, packaged-runtime optimization, broad cleanup, workflows, TBPs, issue templates, public docs, release docs, or guide migration.

## Focus areas

### 1. Animation-definition content

Canonical root:

```text
game/animations/
```

Required IDs and paths:

```text
animation-definition.player.basic
game/animations/animation-definition.player.basic.json

animation-definition.npc.talkable-smoke
game/animations/animation-definition.npc.talkable-smoke.json
```

Definitions contain schema, stable ID, visual-definition reference, clips, tracks/shorthand, markers, and provenance. They must not contain runtime entity IDs, current ticks, selection state, gameplay commands, or native renderer values.

### 2. Validation

Validate schema, stable IDs, visual/part/region references, clip duration, loop mode, track target/type/interpolation compatibility, tick bounds, unique IDs, tick-zero keyframes, scalar ranges, finite values, duplicate targets, marker shape, and shorthand consistency. Invalid content produces stable diagnostics.

### 3. Property registry

Required properties:

```text
visual.region
visual.offset.x
visual.offset.y
visual.scale.x
visual.scale.y
visual.rotation-degrees
visual.tint.red
visual.tint.green
visual.tint.blue
visual.opacity
```

`visual.region` is `asset-region` and step-only. Other properties are scalar. Scale is positive finite; tint and opacity are `[0,1]`; other scalar values are finite.

### 4. Clip and keyframe model

Canonical clip fields:

```text
id
durationTicks
loop
tracks
markers
```

Each track has stable ID, visual part/property target, value type, interpolation, and keyframes. Property order is not semantic. Clips, tracks, keyframes, and markers canonicalize deterministically.

### 5. Sprite-sequence shorthand

Support `kind: sprite-sequence` with `partId`, ordered regions, `ticksPerFrame`, and loop. Compile it to a canonical `visual.region` step track. Canonical compiled data is runtime authority.

### 6. Compilation

Pipeline:

```text
authored JSON
→ validation
→ reference resolution
→ shorthand expansion
→ canonical clips/tracks/keyframes/markers
→ compiled fingerprint
```

Compiled output contains no unresolved IDs or shorthand. Runtime sampling must not repeatedly parse authored JSON.

### 7. Time and playback

Use integer runtime ticks.

```text
elapsedTicks = runtimeTick - startedAtRuntimeTick
```

Repeat:

```text
loopIteration = elapsedTicks / durationTicks
localTick = elapsedTicks % durationTicks
status = playing
```

Once:

```text
elapsed < duration → localTick = elapsed, playing
elapsed >= duration → localTick = duration - 1, completed
```

Reject samples before the start tick. Do not use render-frame or wall-clock time.

### 8. Track sampling

Step selects the greatest keyframe tick not exceeding local tick. Linear scalar tracks interpolate between surrounding keyframes. After the final keyframe, hold the final value. Every track requires a keyframe at tick zero. Asset-region tracks reject linear interpolation.

### 9. Selection frame

Selection result includes source, animation-definition ID, base clip/key/reason, and optional overlay clip/key/reason. Selection reads immutable runtime state and does not mutate runtime.

Smoke selection:

```text
velocity zero + facing east → clip.idle.east
velocity non-zero + facing east → clip.walk.east
```

Use explicit clip IDs. Do not build a general expression language.

### 10. Continuity and restart

Per layer:

```text
same selectionKey → preserve startedAtRuntimeTick
different selectionKey → restart at current runtime tick
```

Restarting the same clip requires a new key.

Required operations:

```text
SelectBaseClip
RestartBaseClip
SelectOverlayClip
RestartOverlayClip
ClearOverlayClip
```

The sampler never changes selection.

### 11. Derived playback state

Track per layer:

```text
clipId
selectionKey
startedAtRuntimeTick
elapsedTicks
localTick
loopIteration
status: playing|completed
```

This is derived presentation state, not gameplay-authoritative component state.

### 12. Composition

```text
visual defaults
→ base sampled replacements
→ overlay sampled replacements
```

Overlay wins conflicts. Untargeted properties retain earlier/default values. No additive, multiplicative, weighted, masked, or cross-fade modes. Evidence identifies the winning layer and track.

### 13. One-shot completion

Once clips complete, sample the final tick, and hold final values. They do not return, clear, select, or restart automatically. The smoke must show completed overlay hold followed by an explicit clear returning to base-only composition.

### 14. Markers

Markers contain ID, tick, registered presentation kind, and structured payload. Initial kinds may include:

```text
presentation.footstep
presentation.effect
presentation.debug
presentation.animation-complete
```

No marker is generated automatically. Markers cannot mutate runtime, deal damage, change collision, spawn gameplay entities, or progress interactions/quests.

### 15. Marker crossing

Emit all markers in `(previous exclusive, current inclusive)`. Tick-zero markers emit once on first sample. Multi-tick advances emit every crossing. Repeat-loop ordering is loop iteration, marker tick, marker ID. Do not silently drop markers across multiple loops.

Occurrence evidence includes source, animation definition, layer, clip, selection key, marker, runtime observation tick, local tick, loop iteration, kind, and payload.

### 16. Animated render projection

Pipeline:

```text
visual definition
+ base patch
+ optional overlay patch
→ animated visual instance
→ existing semantic render projection
```

Runtime transforms remain authoritative; visual offsets do not mutate them. Raylib remains unchanged except that existing commands carry sampled regions/transforms/tints.

Animated items preserve source, visual/animation definition, selections, playback, property provenance, values, tick, and fingerprints.

### 17. Directional fixture

Required player clips:

```text
clip.idle.east
clip.idle.west
clip.walk.east
clip.walk.west
```

North/south are optional. No variants or inheritance. Extend the checked-in smoke atlas or add a bounded simple-color PNG fixture with matching metadata.

### 18. Overlay fixture

Required once clip:

```text
clip.interaction-pulse
```

It proves scalar animation, hold-final, unique selection keys, completion evidence, and explicit clear. It may animate opacity, scale channels, or tint channels.

### 19. Input/replay-driven journey

Use M016 semantic frames:

```text
idle → move east → stop → interact
```

Expected selection:

```text
clip.idle.east
→ clip.walk.east
→ clip.idle.east
→ overlay clip.interaction-pulse
→ overlay completed/held
→ explicit overlay clear
```

Replay must produce equivalent selections, playback, samples, markers, animated items, and final render fingerprint.

### 20. Headless commands

Add product CLI capability such as:

```text
agentic2d animation inspect <animation-id-or-path> --output <directory>
agentic2d animation project --scenario <scenario-id> --output <directory>
```

Use current CLI conventions. Commands must not initialize raylib.

### 21. Artifacts

Required:

```text
animation-result.json
compiled-animation.json
animation-selections.jsonl
animation-playback.jsonl
animation-samples.jsonl
animation-markers.jsonl
animated-render-items.jsonl
animation-diagnostics.json
```

Existing input, runtime, interaction, render, and replay artifacts remain part of end-to-end evidence.

### 22. Engineering wrappers

Add:

```text
./eng/animation-content-smoke.sh
./eng/animation-sampling-smoke.sh
./eng/animation-marker-smoke.sh
./eng/animated-render-smoke.sh
./eng/animation-replay-smoke.sh
./eng/m017-smoke.sh
```

All required wrappers run headlessly. Graphical review through the existing raylib environment is optional.

## Implementation constraints

- Core animation contracts contain no raylib-cs types.
- Animation targets registered presentation properties only.
- Animation never mutates runtime/entity/spatial/interaction state.
- Selection and sampling remain separate.
- Playback state is derived presentation state.
- Runtime ticks are canonical time.
- Base and overlay are the only layers.
- Replacement is the only composition mode.
- Completed once clips hold final values.
- No automatic selection transition occurs.
- Markers are presentation-only.
- Compilation and evidence are deterministic.
- Headless tests are primary.
- M016 semantic replay drives equivalence.
- M015 rendering/raylib boundaries remain unchanged.
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
docs/specs/content-validation-contract.md
docs/specs/asset-metadata-contract.md
docs/specs/visual-definition-contract.md
docs/specs/render-projection-contract.md
docs/specs/raylib-debug-client-contract.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/interaction-runtime-contract.md
docs/specs/input-action-map-contract.md
docs/specs/tick-bound-input-frame-contract.md
docs/specs/semantic-input-recording-and-replay-contract.md
docs/specs/animation-definition-contract.md
docs/specs/animation-selection-and-sampling-contract.md
docs/specs/animation-marker-contract.md
docs/specs/animated-render-projection-contract.md
docs/artifacts/input-execution-and-replay-artifact-contract.md
docs/artifacts/render-projection-artifact-contract.md
docs/artifacts/animation-execution-artifact-contract.md
docs/decisions/ADR-0018-rendering-is-read-only-and-raylib-is-an-isolated-adapter.md
docs/decisions/ADR-0019-input-is-tick-bound-semantic-data-and-replay-uses-resolved-frames.md
docs/decisions/ADR-0020-animation-produces-typed-presentation-patches.md
docs/milestones/MILESTONE-015-read-only-rendering-projection-and-raylib-cs-debug-client.md
docs/milestones/MILESTONE-016-deterministic-multi-device-input-frames-action-mapping-and-semantic-replay.md
docs/milestones/MILESTONE-017-deterministic-keyframe-animation-base-overlay-markers-and-animated-render-projection.md
```

Do not read external guide documents for implementation.

## Files or areas likely affected

Recommended project:

```text
src/Agentic2D.Animation
```

Likely affected:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Validation
src/Agentic2D.Rendering
src/Agentic2D.ScenarioRunner
src/Agentic2D.Tools
src/Agentic2D.DebugClient.Raylib
tests/unit/Agentic2D.Tests.Unit
```

Likely content:

```text
game/animations/animation-definition.player.basic.json
game/animations/animation-definition.npc.talkable-smoke.json
game/scenarios/smoke/animation-player-locomotion-smoke.json
game/scenarios/smoke/animation-overlay-marker-smoke.json
game/scenarios/smoke/animation-semantic-replay-smoke.json
```

Likely asset/visual updates:

```text
game/assets/raw/samples/
game/assets/metadata/
game/visuals/visual-definition.player.basic.json
game/visuals/visual-definition.npc.talkable-smoke.json
```

Required scripts are the six wrappers listed above.

## Validation tiers and concrete commands

### Tier 1 — Focused tests

Cover validation/canonicalization, shorthand expansion, step/linear sampling, once/repeat boundaries, selection-key preserve/restart, base/overlay composition, completion/hold-final, explicit clear, marker crossing at tick zero/multi-tick/loops, replay equivalence, and invalid gameplay-target/duplicate-track rejection.

### Tier 2 — Headless smokes

```bash
./eng/animation-content-smoke.sh
./eng/animation-sampling-smoke.sh
./eng/animation-marker-smoke.sh
./eng/animated-render-smoke.sh
./eng/animation-replay-smoke.sh
```

### Tier 3 — Regression and complete gate

```bash
./eng/check.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/content-validate.sh visuals
./eng/content-validate.sh animations
./eng/m015-smoke.sh
./eng/m016-smoke.sh
./eng/m017-smoke.sh
```

M017 does not require graphics for acceptance. Existing M015 graphics validation must remain valid.

### Direct checks

Expected shapes:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate animations --output artifacts/content/animations

dotnet run --project src/Agentic2D.Tools -- animation inspect animation-definition.player.basic --output artifacts/animation/player-basic

dotnet run --project src/Agentic2D.Tools -- animation project --scenario animation-player-locomotion-smoke --output artifacts/animation/player-locomotion

dotnet run --project src/Agentic2D.Tools -- scenario run animation-overlay-marker-smoke --output artifacts/scenarios/animation-overlay-marker

dotnet run --project src/Agentic2D.Tools -- scenario run animation-semantic-replay-smoke --output artifacts/scenarios/animation-replay

dotnet run --project src/Agentic2D.Tools -- render project --scenario animation-semantic-replay-smoke --tick final --output artifacts/render/animation-replay
```

Use current CLI syntax if it differs and update direct docs consistently.

## Acceptance criteria

1. Required animation definitions validate.
2. Clip, track, keyframe, and marker IDs are stable and unique.
3. Visual/part/region references resolve.
4. Invalid properties, types, interpolation, ranges, and duplicate targets produce stable diagnostics.
5. Sprite shorthand compiles to canonical region tracks.
6. Equivalent authored content produces equivalent compiled output/fingerprint.
7. Sampling uses compiled definitions.
8. Tick-zero/final-tick conventions are tested.
9. Repeat wrapping is deterministic.
10. Once completion holds final tick.
11. Step and linear sampling are correct and finite.
12. Every track has a tick-zero keyframe.
13. Asset-region tracks reject linear interpolation.
14. One base and zero/one overlay are supported.
15. Same selection key preserves playback; changed key restarts.
16. Same clip restarts through a new key.
17. Explicit directional IDs are used.
18. Playback remains presentation-derived.
19. No automatic fallback/return/clear/restart exists.
20. Visual defaults, base, then overlay composition is correct.
21. Overlay wins conflicts and evidence identifies the winner.
22. No additive, multiplicative, weighted, masked, or cross-fade composition exists.
23. Markers validate and tick-zero emits once.
24. Previous-exclusive/current-inclusive crossing works.
25. Multi-tick and repeat-loop marker occurrences are complete and ordered.
26. No automatic completion marker is generated.
27. Markers cannot mutate gameplay/runtime.
28. Animation applies before final render-item generation.
29. Raylib types do not enter animation contracts.
30. Runtime transforms remain authoritative.
31. Animated item provenance is complete.
32. Idle/walk/idle selection follows semantic input.
33. Direction change restarts explicitly.
34. Interaction overlay starts with unique key.
35. Completed overlay holds final values.
36. Explicit clear returns base-only composition.
37. Marker evidence is observable.
38. Semantic replay produces equivalent selections, samples, markers, animated items, and final render fingerprint.
39. No physical input or graphics environment is required.
40. M015/M016 boundaries and regressions pass.
41. All required wrappers run headlessly.
42. ADR-0020 and M017 are indexed after acceptance.
43. No excluded runtime property animation, gameplay marker command, automatic fallback, blend tree, skeletal animation, audio, particles, UI/editor, save/load, packaging, guide, workflow, TBP, or issue-template work is introduced.

## Direct documentation impact

Update only where needed:

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
docs/specs/visual-definition-contract.md
docs/specs/render-projection-contract.md
docs/specs/raylib-debug-client-contract.md
docs/specs/semantic-input-recording-and-replay-contract.md
docs/artifacts/render-projection-artifact-contract.md
docs/artifacts/input-execution-and-replay-artifact-contract.md
```

Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

The package adds:

```text
.guide-sync/pending/2026-07-14-m017-index-and-crosslink-sync.md
.guide-sync/pending/2026-07-14-m017-human-review-and-animation-boundary-followup.md
```

Ordinary implementation agents must not read these files.

## Human review requirements

Verify typed targets, runtime immutability, base/overlay precedence, explicit selection/restart/clear, hold-final completion, directional clip readability, shorthand/canonical equivalence, marker crossing and presentation-only authority, scalar-channel overhead, property provenance, replay equivalence, and future extensibility without weakening gameplay authority.

## Out-of-scope guide migration work

No guide migration is included. Do not modify `.guide-profile.json`, copy guide documents/templates, reference external guides as operational authority, or require ordinary agents to read `.guide-sync/`.
