# Feature Roadmap Reconstruction

## Status

Historical roadmap reconstructed for future planning; non-authoritative.

## Question

What product-feature roadmap existed before the corrective train, what has already been implemented, and which remaining directions should be reconsidered rather than mechanically resumed?

## Roadmap snapshot before cleanup

The planning sequence had converged on:

```text
product shell
→ terrain / map authoring / procedural world generation / camera
→ entity presentation / growth / animation / readability
→ audiovisual asset integration / packaging / visual direction
→ tutorial / story / statistics / export
→ population departure / settlement abandonment
→ distribution / configuration / compatibility / capture / final polish
```

This sequence was a planning roadmap, not project authority.

## What happened to the first stage

The "product shell" stage was substantially implemented by M037:

- main/pause navigation;
- world creation/continue/load;
- save catalog/manual save/autosave;
- user settings and safe display handling;
- remappable semantic input;
- player versus diagnostics composition;
- tutorial entry point;
- basic credits/accessibility shell.

Therefore future planning should not reintroduce "product shell" as a new roadmap milestone.

The retained UI toolkit implementation from M037 is now itself subject to the separate M052–M054 agent-oriented UI track, but the application/workflow capabilities still exist and should be reused.

## Direction 1 — terrain, map presentation and world generation

This was the intended next product-facing direction after M037.

Historical motivation:

- the settlement simulation had meaningful regions, infrastructure and logistics but presentation remained placeholder/procedural;
- M037 explicitly described the product as ready for terrain/map presentation work;
- procedural generation, terrain transitions and map-presentation overhaul were explicitly deferred rather than rejected.

Questions that remain genuinely open:

- authored maps versus procedural generation versus a hybrid;
- what "region" means visually and spatially to a player;
- deterministic world-generation contracts and seeds;
- camera/navigation relationship to larger worlds;
- terrain transition semantics;
- whether generated world structure belongs to reusable engine capability or game-specific dogfood;
- what headless evidence is necessary so agents can inspect generated worlds without screenshots.

This direction still looks structurally valid, but it should be re-planned from current runtime/UI/asset authority.

## Direction 2 — entity presentation, growth, animation and readability

Historical goal:

Make autonomous settlement behavior legible and alive rather than represented mainly by debug/procedural markers.

Potential scope included:

- stronger entity visual definitions;
- growth/condition representation;
- walking/work/activity animation;
- infrastructure condition presentation;
- visual differentiation of resources/workers/structures;
- clearer state transitions and overlays.

Parts of this capability already exist in engine form:

- deterministic animation projection;
- rendering projections;
- semantic effects/sound cues;
- M034 growth/condition domain state;
- asset preview/promotion infrastructure.

The remaining question is integration and visual language, not inventing animation infrastructure from zero.

The active M052–M054 UI track may affect how HUD/operations/readability surfaces are authored but should not own world/entity presentation semantics.

## Direction 3 — audiovisual asset integration, packaging and visual direction

Historical goal:

Replace placeholders/procedural presentation with selected, reviewed, legally usable audiovisual content and make the product look/sound coherent.

Since then, M047–M050 created a much stronger curation/promotion chain than this roadmap originally assumed.

Remaining planning should therefore distinguish:

```text
asset curation/promotion machinery
from
runtime consumption/integration
from
art direction/content selection
from
distribution/legal closure
```

M049 is already reserved for the first missing technical closure: promoted-content consumption and real affected rebuild.

Broader art/audio/music selection, visual direction and licensing remain product/content work after that technical boundary is trustworthy.

## Direction 4 — tutorial, story, statistics and export

M037 implemented tutorial entry only, not tutorial content.

Historical future areas included:

- guided onboarding/tutorial;
- player-facing statistics/lifetime summaries;
- telemetry-like internal summaries/export where appropriate;
- story/narrative framing;
- additional export/distribution polish.

These were deliberately deferred because they are downstream of having a coherent game/presentation.

They should not be bundled automatically. Tutorial, narrative, statistics and distribution have different value/risk profiles and may become separate planning tracks.

## Direction 5 — population departure and settlement abandonment

This was a planned new simulation/gameplay consequence after the core settlement loop was stable.

The rough product idea was that persistent shortages, poor conditions or systemic failure should eventually have consequences beyond temporary alerts—people may leave and a settlement may be abandoned.

This direction remains conceptually compatible with indirect control and autonomous simulation, but its semantics were never made current authority.

Future planning would need to resolve at least:

- who/what decides departure;
- whether workers have individualized tolerance/state;
- deterministic versus probabilistic behavior;
- warning/explanation requirements;
- relation to food/water/comfort/infrastructure failure;
- persistence and multi-fidelity equivalence;
- recovery/repopulation;
- what "abandoned" means for a region/world;
- whether this begins the larger personality/social-system track.

Do not infer these rules from the historical roadmap.

## Direction 6 — distribution and final product polish

Historical closing work included:

- configuration/compatibility cleanup;
- platform validation/catch-up;
- capture/evidence polish;
- distribution packaging;
- final audiovisual/legal closure;
- performance and release readiness.

Some foundations already exist:

- Linux standalone export work;
- platform epoch/catch-up model;
- product shell/settings;
- reproducible validation;
- current release/consumer validation methodology.

This should be revisited only after the intended product slice is clearer.

## New track inserted after the historical roadmap — agent-oriented UI

The M052–M054 track is not part of the old feature roadmap.

It emerged from difficulty building/reviewing workbench GUIs and is now an architectural/research track:

```text
M052  coherent typed UI capability
M053  real workbench migration
M054  comparative agent-effectiveness evaluation
```

Its result may change how later roadmap features are authored and validated, so it is reasonable to keep the broader feature roadmap paused while this track establishes evidence.

## Rebase guidance

Do not restore the historical roadmap as numbered milestones.

Instead, after the current planning cleanup and active UI work reach useful boundaries:

1. inspect current repository capability;
2. reassess M049;
3. choose the next product objective;
4. select only the minimum roadmap directions needed to support it;
5. plan them under the current 0.9 obligation/evidence model;
6. retain other directions as candidate research/planning context.

## Current candidate ordering

If no new product evidence changes priorities, the historically strongest continuation is still roughly:

```text
M049 asset consumption/rebuild closure
→ terrain/map/world presentation
→ entity/world visual readability
→ integrated audiovisual direction
→ onboarding/statistics/narrative as justified
→ richer population consequences
→ distribution/release polish
```

The active UI track proceeds independently and may alter authoring/operations UX along the way.

## Freshness

Reconstructed: 2026-10-12.

Revalidate after M049, M052–M054, or any explicit choice of the next concrete product objective.
