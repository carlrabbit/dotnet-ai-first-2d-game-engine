# Milestone 015 — Read-Only Rendering Projection and raylib-cs Debug Client

## Goal

Introduce a backend-neutral, read-only rendering projection and use it through an isolated raylib-cs debug client that can render live scenario snapshots, pause, step, reset, load recorded snapshot artifacts, display deterministic debug overlays, and capture screenshots only on explicit request.

Required journey:

```text
authored map + assets + visual definitions
+ immutable runtime snapshot
→ deterministic world render projection
→ deterministic render command list
→ isolated raylib-cs adapter
→ interactive debug window
→ pause / step / reset through runtime APIs
→ optional explicit screenshot
→ structured render artifacts and diagnostics
```

Rendering is presentation only and must not become gameplay authority.

## Repository role and maturity assumptions

Repository role: `capability-provider`.

The repository implements reusable engine/runtime/tooling capability. Rendering fixtures and the graphical client are bounded dogfood, not a consumer game.

Assumptions:

- implementation-ready, artifact-first, headless-first, CLI/API-first;
- Milestones 001–014 and accepted completion patches are implemented;
- maps, raw PNG metadata, runtime entities/components, entity definitions, continuous transforms, static map objects, interactions, runtime snapshots, inspection artifacts, and review packs exist;
- M014 entity definitions may contain an opaque visual reference that M015 may migrate to `visualDefinitionId`;
- existing stable-ID, deterministic-ordering, diagnostics, command/event/query, artifact, and review rules remain authoritative.

## Execution mode

`ai-executed-broad`

Complete backend-neutral projection and headless validation before raylib-cs integration.

## Locked design decisions

- rendering consumes immutable snapshots and authored content only;
- renderer and raylib adapter never mutate runtime state;
- only the graphical debug-client project references raylib-cs;
- backend-neutral contracts contain no raylib-cs types;
- visual definitions are a separate authored domain;
- entity definitions and static map objects may reference `visualDefinitionId`;
- visual references never determine entity/static ownership;
- visual definitions may contain multiple parts;
- one real checked-in PNG atlas uses simple single-color regions;
- world projection remains in world units;
- origin is top-left; X positive right; Y positive down;
- one tile remains `1.0 × 1.0` world units;
- logical viewport is `320 × 180`;
- presentation uses integer-scaled render texture with letterboxing;
- point filtering is the default;
- default visual anchor is bottom-center;
- ordering is layer, explicit order, optional Y sort, stable item ID;
- static map projection is on-demand and cached;
- dynamic projection is rebuilt per immutable runtime snapshot;
- full static rebuild occurs on map/visual/asset structural revision change;
- no interpolation;
- live scenario and recorded-snapshot modes share one projection path;
- live controls include run, pause, step one tick, step ten, reset, overlays, keyboard entity cycling, camera pan/zoom, and close;
- no mouse selection;
- screenshots occur only through explicit CLI request or key press;
- screenshots are review evidence, not cross-platform semantic truth;
- structural render artifacts are machine-stable authority;
- graphics smoke is a separate environment-dependent tier;
- no NativeAOT, package publishing, or packaged runtime.

## Scope

1. Authored visual-definition domain.
2. Visual references from entity definitions and static map objects.
3. Real single-color PNG atlas smoke fixture.
4. Backend-neutral render primitives.
5. Semantic world render projection.
6. Backend-neutral render command compilation.
7. Deterministic render layers and ordering.
8. Static map projection and cache.
9. Dynamic entity projection.
10. Multi-part static-object projection.
11. Camera suggestion and viewport projection.
12. Debug overlays.
13. Structural render artifacts.
14. Headless render-projection CLI command.
15. Renderable snapshot artifact.
16. Separate raylib-cs executable.
17. Exact pinned raylib-cs dependency.
18. Texture/resource cache and asset binding.
19. Live scenario mode.
20. Pause, step, step-ten, reset.
21. Snapshot-artifact mode.
22. Keyboard-only selection and controls.
23. Explicit screenshot capture.
24. Environment-dependent raylib smoke.
25. Human visual review.

## Non-goals

Do not implement runtime mutation from rendering, raylib types in core contracts, gameplay input mapping, direct key polling in behaviors, interpolation, animation, particles, lighting, shader framework, audio, UI toolkit, editor, mouse selection, scene graph, transform hierarchy, text layout system, font asset pipeline, atlas generation/repacking, batching optimization, GPU profiling, dirty rectangles, destructible terrain, runtime map-edit commands solely for rendering, pixel-perfect cross-platform screenshot baselines, guaranteed fully headless raylib operation, web/mobile deployment, NativeAOT, packaged releases, broad cleanup, workflows, TBPs, issue templates, public docs, release docs, or guide migration.

## Focus areas

### 1. Visual-definition content

Canonical root:

```text
game/visuals/
```

Required IDs:

```text
visual-definition.ground.smoke
visual-definition.player.basic
visual-definition.npc.talkable-smoke
visual-definition.tree.large
```

Each definition contains schema, stable ID, and one or more parts. Each part contains stable part ID, asset/region IDs, anchor, finite offset, finite positive world size, layer, integer order, sort mode, and tint.

Supported anchors:

```text
top-left
center
bottom-center
```

Supported sort modes:

```text
fixed
y
```

Initial layers, in order:

```text
background
ground
entities
foreground
debug
ui
```

Visual definitions contain presentation only, not entity identity, collision, behavior, interaction, or runtime state.

### 2. Visual references

Entity definitions and static map objects may reference `visualDefinitionId`.

Rules:

- references are optional but must resolve when present;
- invalid references produce stable diagnostics;
- a static object stays static;
- a runtime entity stays an entity;
- projection records `sourceKind`, `sourceId`, `visualDefinitionId`, part, asset, and region;
- no mutable runtime `Renderable` component is added.

### 3. Real PNG atlas fixture

Add:

```text
game/assets/raw/samples/render-atlas-smoke.png
game/assets/metadata/render-atlas-smoke.asset.json
```

Stable asset ID:

```text
asset.render-atlas-smoke
```

Required regions:

```text
region.ground
region.player
region.npc
region.blocked
region.tree-base
region.tree-canopy
```

Use actual PNG decoding in graphical smoke. Regions may be simple single colors. The tree must use separate base and canopy regions. Check the PNG in; do not generate it at runtime.

### 4. Backend-neutral rendering types

Introduce engine-owned types for points, sizes, rectangles, colors, camera, viewport, asset-region references, semantic items, commands, and frames. All numeric values must be finite and serializable. No raylib-cs types outside the adapter.

### 5. Semantic render projection

Project authored/static and runtime/dynamic state into semantic render items.

Required source categories:

```text
map-tile
static-map-object
runtime-entity
debug-overlay
ui-overlay
```

Each item includes stable item ID, source linkage, visual/part/asset/region linkage, world destination, anchor, layer, order, sort mode, Y-sort coordinate, tint, and map/snapshot provenance.

Projection reads immutable runtime snapshots, map content, entity provenance, visual definitions, and validated asset metadata. It does not load textures.

### 6. Deterministic ordering

Sort by:

```text
layer index ascending
→ explicit order ascending
→ Y coordinate ascending when sortMode = y
→ stable item ID ordinal
```

The tree proves multi-part ordering:

```text
base: entities / y
canopy: foreground / fixed
```

### 7. Backend-neutral command compilation

Required command kinds:

```text
clear
begin-world-camera
draw-texture-region
draw-solid-rectangle
draw-line
draw-text
end-world-camera
begin-screen-space
end-screen-space
```

Commands preserve semantic item/source linkage and contain no native handles.

### 8. Static map projection cache

Build static projection on demand.

Cache key includes:

```text
map ID
map structural version/fingerprint
visual-definition revision/fingerprint
asset metadata revision/fingerprint
render projection settings
```

Equivalent inputs yield equivalent cached projection. Structural changes rebuild the complete static projection. Cache contains no native resources. Do not implement chunk-level invalidation.

### 9. Dynamic projection and frame composition

Rebuild dynamic projection for each immutable snapshot. Include runtime visuals plus optional overlays for selected entity, collision AABBs, triggers, interaction radius, IDs, tick, scenario, and last trigger/interaction event.

Combine:

```text
cached static projection
+ dynamic projection
+ client-local overlays
→ deterministic RenderFrame
```

Paused redraw of the same snapshot must be semantically equivalent. No interpolation.

### 10. Camera and viewport

Logical viewport: `320 × 180`.

Render to a logical render texture, scale by the largest fitting integer factor, and letterbox.

Required camera behavior:

- follow `entity.player` by default in live mode;
- keyboard pan and zoom;
- reset camera;
- camera state remains client-local.

### 11. Keyboard selection and overlays

Required selection:

```text
Tab: next entity by entity ID ordinal
Shift+Tab: previous entity
```

No mouse selection.

Required overlays include entity IDs, collision AABBs, trigger AABBs, interaction radius, static obstacle bounds, selected marker, tick, scenario ID, and recent trigger/interaction summary.

### 12. Headless projection CLI

Add a product CLI command such as:

```bash
agentic2d render project --scenario interaction.npc-smoke --tick final --output artifacts/render/interaction-npc-smoke
```

It must produce projection artifacts without initializing raylib and must use the same projector/compiler as the graphical client.

### 13. Renderable snapshot artifact

Add a stable artifact sufficient to reconstruct projection without rerunning simulation. Include scenario, map/version, tick, render-relevant entity/component state, entity-definition/provenance references, static object references, visual-definition references, asset metadata references, and diagnostics. Exclude client-local camera, selection, and overlay toggles.

### 14. raylib-cs graphical project

Add:

```text
src/Agentic2D.Rendering
src/Agentic2D.DebugClient.Raylib
```

Only the second references raylib-cs. `Agentic2D.Tools` must not reference raylib-cs.

Pin the exact verified package version. Planning baseline:

```text
Raylib-cs 8.0.0
```

Record binding and native raylib versions in dependency/project evidence.

### 15. Resource ownership

The client owns window/context, textures, render texture, and any other native resources. Use a texture cache keyed by stable asset ID plus source fingerprint. Load each atlas once, apply point filtering, reuse regions, provide deterministic missing-texture fallback/diagnostics, unload all resources, keep raylib calls on the client thread, and clean up through `try/finally` or equivalent.

### 16. Live scenario mode

Launch form, adjusted to current conventions:

```bash
dotnet run --project src/Agentic2D.DebugClient.Raylib -- scenario --scenario interaction.npc-smoke --map map.interaction-smoke
```

Controls:

```text
Space: pause/resume
Period: step one tick while paused
Shift+Period: step ten ticks while paused
R: reset scenario
F1: toggle overlays
Tab / Shift+Tab: cycle selected entity
Arrow keys: pan camera
Mouse wheel or +/-: zoom camera
F12: explicit screenshot
Escape: close
```

Mouse wheel is permitted for zoom; mouse selection is not.

Runtime APIs own `RunOneTick`, `RunTicks(N)`, `ResetScenario`, and `GetLatestSnapshot`. Scenario completion pauses automatically; step is a no-op until reset; no implicit reset.

### 17. Snapshot-artifact mode

Launch form:

```bash
dotnet run --project src/Agentic2D.DebugClient.Raylib -- snapshot --input artifacts/render/interaction-npc-smoke/render-snapshot.json
```

Load and validate the artifact, resolve visuals/assets, use the same projector/compiler, render without simulation, allow camera/overlays/selection/explicit screenshot, and expose run/step/reset as unavailable.

### 18. Explicit screenshot capture

Capture only through `F12` or explicit `--capture <path>`.

Command capture flow:

```text
initialize
→ load source
→ project stable frame
→ draw
→ capture PNG
→ emit metadata
→ close in capture-only mode
```

Metadata includes capture sequence, source mode, scenario/snapshot ID, tick, map/version, projection fingerprint, viewport, output path, and diagnostics. Do not use wall-clock timestamps as semantic identity. Do not capture automatically during normal launch or ordinary smoke.

### 19. Structural artifacts

Required files:

```text
render-result.json
render-snapshot.json
render-frame.json
render-items.jsonl
render-commands.jsonl
asset-bindings.json
render-diagnostics.json
```

Explicit capture additionally produces:

```text
frame.png
frame-metadata.json
```

Semantic validation uses JSON artifacts, not PNG equality.

### 20. Engineering wrappers

Add:

```bash
./eng/visual-content-smoke.sh
./eng/render-projection-smoke.sh
./eng/raylib-debug-client-smoke.sh
./eng/m015-smoke.sh
```

`visual-content-smoke.sh` validates visual definitions, asset/region references, tree parts, and entity/static references.

`render-projection-smoke.sh` validates static caching, dynamic projection, ordering, pause/reset/step projection semantics, snapshot round trip, and structural artifacts without raylib.

`raylib-debug-client-smoke.sh` is environment-dependent and validates native initialization, real PNG loading, one frame draw, optional explicit capture when requested, and clean shutdown.

`m015-smoke.sh` always runs headless gates and invokes graphics smoke only when documented preconditions are available, reporting explicit pass/fail/skipped status.

## Implementation constraints

- Headless workflows remain usable without raylib native dependencies.
- Only the graphical project references raylib-cs.
- Core/runtime/entity/map/asset contracts contain no raylib types.
- Projection is read-only and never advances simulation.
- Simulation tick rate and presentation frame rate are separate.
- Paused redraw does not advance runtime.
- No interpolation.
- Static and dynamic render state remain distinguishable.
- Entity/static provenance remains visible.
- Visual definitions do not determine entity/static ownership.
- Real PNG loading is required for graphical smoke.
- Projection tests run without window/GPU.
- Screenshot PNG is review evidence only.
- Structural artifacts are deterministic.
- Graphics infrastructure is not required for every unit-test environment.
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
docs/specs/asset-metadata-contract.md
docs/specs/map-content-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/review-pack-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/mixed-world-projection-contract.md
docs/specs/continuous-kinematic-spatial-module-contract.md
docs/specs/entity-definition-and-instantiation-contract.md
docs/specs/spatial-query-and-trigger-contract.md
docs/specs/interaction-runtime-contract.md
docs/specs/visual-definition-contract.md
docs/specs/render-projection-contract.md
docs/specs/raylib-debug-client-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/entity-instantiation-query-trigger-interaction-artifact-contract.md
docs/artifacts/render-projection-artifact-contract.md
docs/decisions/ADR-0016-runtime-owns-entities-components-and-spatial-modules-own-spatial-semantics.md
docs/decisions/ADR-0017-authored-definitions-instantiate-runtime-entities-and-interactions-use-explicit-intents.md
docs/decisions/ADR-0018-rendering-is-read-only-and-raylib-is-an-isolated-adapter.md
docs/milestones/MILESTONE-014-authored-entity-definitions-runtime-instantiation-spatial-queries-triggers-and-interactions.md
docs/milestones/MILESTONE-015-read-only-rendering-projection-and-raylib-cs-debug-client.md
```

Do not read external guide documents.

## Files or areas likely affected

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Entities
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Tools
src/Agentic2D.Rendering
src/Agentic2D.DebugClient.Raylib
tests/unit/Agentic2D.Tests.Unit
```

Likely authored content:

```text
game/assets/raw/samples/render-atlas-smoke.png
game/assets/metadata/render-atlas-smoke.asset.json
game/visuals/visual-definition.ground.smoke.json
game/visuals/visual-definition.player.basic.json
game/visuals/visual-definition.npc.talkable-smoke.json
game/visuals/visual-definition.tree.large.json
```

Likely updates:

```text
game/entities/entity-definition.player.basic.json
game/entities/entity-definition.npc.talkable-smoke.json
game/maps/smoke/map-interaction-smoke.map.json
```

Required scripts:

```text
eng/visual-content-smoke.sh
eng/render-projection-smoke.sh
eng/raylib-debug-client-smoke.sh
eng/m015-smoke.sh
```

## Validation tiers and concrete repository commands

### Tier 1 — Headless structural validation

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
./eng/asset-inspect-smoke.sh
./eng/review-pack-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/m013-smoke.sh
./eng/m014-smoke.sh
./eng/visual-content-smoke.sh
./eng/render-projection-smoke.sh
```

### Tier 2 — Graphics-capable raylib smoke

Required before acceptance on at least one supported graphics-capable environment:

```bash
./eng/raylib-debug-client-smoke.sh
```

The wrapper documents platform/display/native-library prerequisites and emits explicit pass/fail/skipped status.

### Tier 3 — Complete M015 gate

```bash
./eng/m015-smoke.sh
```

It always executes headless gates. Graphics smoke is conditional only with explicit status.

### Required direct checks

```bash
dotnet run --project src/Agentic2D.Tools -- content validate visuals --output artifacts/content/visuals

dotnet run --project src/Agentic2D.Tools -- asset inspect asset.render-atlas-smoke --output artifacts/assets/render-atlas-smoke

dotnet run --project src/Agentic2D.Tools -- render project --scenario interaction.npc-smoke --tick final --output artifacts/render/interaction-npc-smoke

dotnet run --project src/Agentic2D.DebugClient.Raylib -- scenario --scenario interaction.npc-smoke --map map.interaction-smoke

dotnet run --project src/Agentic2D.DebugClient.Raylib -- snapshot --input artifacts/render/interaction-npc-smoke/render-snapshot.json

dotnet run --project src/Agentic2D.DebugClient.Raylib -- snapshot --input artifacts/render/interaction-npc-smoke/render-snapshot.json --capture artifacts/render/interaction-npc-smoke/frame.png
```

Use current CLI argument conventions if they differ.

## Acceptance criteria

1. The real PNG atlas exists and validates.
2. `asset.render-atlas-smoke` resolves its PNG and required regions.
3. All required visual definitions validate.
4. Visual parts have stable IDs and deterministic ordering.
5. Entity definitions reference player/NPC visual definitions.
6. The static tree references its visual definition without becoming an entity.
7. Tree base/canopy use separate parts/layers.
8. Invalid visual/asset/region references produce stable diagnostics.
9. Backend-neutral contracts contain no raylib types.
10. Projection reads immutable snapshots and authored content only.
11. Projection performs no runtime mutation or tick advancement.
12. Static and dynamic projection are distinct.
13. Static cache keys include structural/revision inputs.
14. Equivalent requests produce equivalent projections.
15. Structural change rebuilds static projection.
16. Dynamic projection is rebuilt per snapshot.
17. Paused redraw is semantically equivalent.
18. Reset restores the initial projection.
19. Step one/ten use runtime APIs and frame ticks match runtime ticks.
20. No interpolation is introduced.
21. Ordering follows layer/order/Y/item ID.
22. Tree canopy appears in foreground independently of ownership.
23. Anchors/offsets are deterministic.
24. Item/source/visual/asset linkage is retained.
25. Headless `render project` works without raylib initialization.
26. Required structural artifacts contain real records.
27. Snapshot artifact reconstructs equivalent projection.
28. Live and artifact modes share projector/compiler code.
29. Structural artifacts exclude volatile machine data.
30. Review-pack manifest makes render artifacts discoverable.
31. Only `Agentic2D.DebugClient.Raylib` references raylib-cs.
32. Package version is pinned exactly and binding/native versions are recorded.
33. Window/context lifecycle is clean.
34. Real PNG texture loads once and is reused.
35. Point filtering is applied.
36. Native resources are unloaded.
37. Missing-resource fallback is deterministic and diagnostic.
38. Integer scaling and letterboxing work.
39. Live mode runs.
40. Pause does not advance runtime.
41. Step one/ten and reset work.
42. Scenario completion pauses without implicit reset.
43. Snapshot mode renders without simulation.
44. Keyboard entity cycling is deterministic.
45. Camera state is client-local.
46. No mouse selection is implemented.
47. `F12` and `--capture` capture only on explicit request.
48. Normal launch/smoke do not capture automatically.
49. Projection tests run headlessly.
50. Graphics smoke passes on at least one documented graphics-capable environment.
51. Screenshot pixels are not cross-platform semantic truth.
52. Human review confirms map, player, NPC, tree occlusion, and overlays are understandable.
53. All M014 and earlier gates pass.
54. ADR-0018 and M015 are indexed after acceptance.
55. No excluded scope is introduced.

## Direct documentation impact

Update only where implementation would otherwise contradict or remain undiscoverable:

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
docs/specs/asset-metadata-contract.md
docs/specs/map-content-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/review-pack-contract.md
docs/specs/entity-definition-and-instantiation-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
```

Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-13-m015-index-and-crosslink-sync.md
.guide-sync/pending/2026-07-13-m015-human-review-and-rendering-boundary-followup.md
```

The implementation agent must not read these files.

## Human review requirements

Reviewers must verify that rendering is read-only; raylib types do not leak; the headless CLI has no native graphics dependency; visual references do not change ownership; real PNG bindings are proven; static/dynamic projections are distinct; cache invalidation and ordering are credible; tree occlusion is understandable; stepping/reset use runtime APIs; live/artifact modes share one path; screenshots are explicit review evidence; resource cleanup is credible; and future animation, input, or alternative renderers can extend the projection without changing runtime authority.

## Out-of-scope guide migration work

No guide migration is included. Do not change `.guide-profile.json`, copy guide documents or prompt templates, reference external guides as operational authority, or require ordinary implementation agents to read `.guide-sync/`.
