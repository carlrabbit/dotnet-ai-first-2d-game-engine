# Milestone 013 — Entity Component Runtime and Continuous Kinematic Spatial Module

## Goal

Introduce a minimal deterministic entity/component runtime foundation and use it to implement a second pluggable spatial reference module for continuous kinematic 2D movement.

The milestone must prove this mixed-world model:

```text
grid-authored map
+ map-derived static terrain and obstacle geometry
+ runtime-owned entities and typed components
+ continuously positioned player and NPC
+ large static tree obstacle
→ deterministic kinematic movement and collision resolution
→ structured entity/component/spatial evidence
```

Architectural thesis:

> Maps provide authored static spatial data. Entities provide stable runtime identity and mutable component state. Spatial modules interpret selected entity components and map-derived static world data without owning the entity model.

Required journey:

```text
scenario loads mixed authored world
→ runtime creates entities and typed components
→ immutable entity/component snapshot is created
→ behavior emits continuous movement intent
→ continuous spatial module queries map-derived static geometry
→ displacement is accepted, clipped, slid, or blocked
→ runtime applies component mutations through commands
→ entity/component/spatial events and assertions are produced
→ runtime inspection and review pack expose the complete chain
```

## Repository role and maturity assumptions

Repository role:

```text
capability-provider
```

The repository implements reusable engine/runtime/tooling capability. The milestone uses one bounded repository-owned smoke world to validate the capability; it does not create a full consumer game.

Maturity assumptions:

- implementation-ready;
- artifact-first;
- headless-first and CLI/API-first;
- Milestones 001 through 012 and the Milestone 012 completion patch are implemented;
- stable entity IDs, deterministic ticks, commands, events, queries, behaviors, maps, runtime inspection, and review packs already exist;
- the current entity state is intentionally specialized and is not yet a general entity/component runtime;
- the grid spatial module is a reference module and must remain supported;
- existing diagnostic, status, exit-code, artifact, stable-ID, behavior-phase, and deterministic-ordering rules remain authoritative.

## Execution mode

```text
ai-executed-broad
```

Implementation must proceed in the focus-area order below. The continuous module must not create a second entity universe or bypass the entity/component foundation.

## Scope

1. Stable runtime entity registry and lifecycle.
2. Typed component registration and storage.
3. Deterministic component queries and immutable snapshots.
4. Command-buffered entity/component mutation.
5. Migration of the grid spatial module to runtime-owned entity/component state.
6. Authored scenario component declarations for smoke fixtures.
7. Mixed authored world projection from map data.
8. Map-derived static axis-aligned rectangle geometry.
9. `spatial.continuous-kinematic-2d` reference module.
10. Module-owned continuous transform, kinematic motion, and collision-shape components.
11. World units where one tile is `1.0 × 1.0`.
12. Deterministic axis-separated continuous movement with clipping/sliding.
13. Continuously positioned player and NPC plus one large static tree obstacle.
14. Runtime inspection, scenario assertions, review-pack evidence, tests, and engineering wrappers.
15. One end-to-end entity/component/continuous-motion smoke journey.

## Non-goals

Do not implement:

- a final high-performance ECS;
- archetype storage;
- sparse-set optimization;
- parallel system scheduling;
- automatic component dependency injection;
- runtime component type discovery through reflection;
- source-generated component registries;
- entity prefab/archetype authoring;
- general scene graph;
- parent/child transform hierarchy;
- entity occupancy collision;
- dynamic body/body collision response;
- rigid-body physics;
- mass, force, impulse, restitution, torque, angular velocity, or joints;
- circles, polygons, capsules, slopes, or rotated collision shapes;
- continuous collision detection for high-speed tunneling;
- navigation or pathfinding;
- platformer motion;
- gravity or jumping;
- renderer or animation integration;
- save/load;
- packaged runtime;
- broad unrelated documentation cleanup;
- workflows, TBPs, issue templates, public docs, release docs, or guide migration.

## Focus areas

### 1. Runtime entity registry and lifecycle

Introduce a runtime-owned entity registry.

Required capabilities:

- create entity by stable `EntityId`;
- reject duplicate entity creation;
- query entity existence;
- destroy entity;
- remove all components when an entity is destroyed;
- deterministically enumerate current entities;
- reject component mutation for unknown entities;
- emit entity lifecycle events;
- expose lifecycle evidence through runtime inspection.

Required event families:

```text
entity.created
entity.destroyed
entity.component-added
entity.component-updated
entity.component-removed
```

Exact stable IDs may differ only when documented consistently.

Entity identity is not an object graph and must not be represented by a universal mutable `GameObject` base class.

### 2. Typed component runtime

Introduce a simple typed component-store abstraction.

Required semantic operations:

```text
register component type
add or set component
try-get component
remove component
query entities with component T
query entities with components T1 + T2
deterministically enumerate component-bearing entities
```

Required characteristics:

- component types are explicitly registered;
- component type IDs are stable;
- component state is owned by the runtime;
- spatial modules own component types and semantics, not entity existence;
- storage implementation may use dictionaries or arrays;
- storage implementation must be replaceable without changing semantic contracts;
- no reflection-based hot-path lookup;
- no commitment to archetype or sparse-set ECS yet.

Required initial component types:

```text
component.grid-position
component.continuous-transform-2d
component.kinematic-motion-2d
component.collision-aabb-2d
component.spatial-membership
```

`GridPosition` remains grid-module-owned. Continuous component types remain continuous-module-owned.

### 3. Immutable snapshots and queries

Extend the behavior/runtime snapshot so it can expose deterministic, read-only entity/component state.

Required capabilities:

- entity existence query;
- typed component lookup;
- deterministic component-bearing entity query;
- snapshot fingerprint derived from semantic state;
- fresh snapshot per tick;
- same snapshot for all behaviors within one behavior phase;
- no mutable component-store access from behavior code.

Snapshot fingerprints must include stable component type IDs and deterministically ordered entity/component values relevant to the snapshot.

### 4. Command-buffered mutation

All runtime entity/component mutation must occur through runtime commands or an equivalent command buffer applied after intent/domain resolution.

Required command semantics include:

```text
CreateEntity
DestroyEntity
SetGridPosition
SetContinuousTransform2
SetKinematicMotion2
AddCollisionAabb2
RemoveComponent
```

A generic internal component mutation primitive is acceptable, but externally inspected commands and diagnostics must remain stable and typed enough to understand.

Required safety:

- behavior modules cannot mutate stores directly;
- spatial modules return accepted mutation commands or rejected domain resolutions;
- commands validate entity existence and component compatibility;
- failed command application leaves prior state intact;
- command order is deterministic;
- lifecycle and component events record factual outcomes.

### 5. Grid-module migration

Migrate the existing grid spatial implementation so it uses runtime-owned entity/component state rather than a resolver-private entity-position dictionary.

Required preservation:

- existing `spatial.grid` behavior;
- accepted and rejected grid movement;
- conservative unresolved semantics;
- Milestone 012 scenarios, artifacts, and wrappers;
- module ownership of `GridPosition`;
- no grid APIs in universal engine contracts.

The grid resolver may maintain derived per-execution caches, but runtime-owned `GridPosition` is the source of mutable entity state.

### 6. Authored scenario entity/component declarations

Extend scenario initial state to declare entity components explicitly for new scenarios.

Recommended shape:

```json
{
  "initialState": {
    "entities": [
      {
        "id": "entity.player",
        "components": [
          {
            "type": "component.continuous-transform-2d",
            "value": { "x": 0.5, "y": 0.5 }
          },
          {
            "type": "component.kinematic-motion-2d",
            "value": { "velocityX": 0.0, "velocityY": 0.0, "maxSpeed": 1.0 }
          },
          {
            "type": "component.collision-aabb-2d",
            "value": { "halfWidth": 0.3, "halfHeight": 0.3 }
          },
          {
            "type": "component.spatial-membership",
            "value": {
              "worldId": "map.continuous-smoke",
              "spatialModuleId": "spatial.continuous-kinematic-2d"
            }
          }
        ]
      }
    ]
  }
}
```

Existing scenario entity shapes must remain supported for existing fixtures. Do not force a broad migration of all old scenario files when an adapter can preserve compatibility.

Validation must reject:

- duplicate entity IDs;
- duplicate component type IDs on one entity;
- unknown component type IDs;
- invalid component values;
- components incompatible with the selected spatial module;
- missing required components for the continuous motion target.

### 7. Mixed authored world projection

Treat the map as authored static spatial content, not as the runtime entity store.

Required mixed-world model:

```text
MapContent
├─ tile/terrain layers
├─ semantic cells
├─ static object declarations
├─ markers/spawns
└─ source references

RuntimeEntityWorld
├─ player
├─ NPC
└─ other mutable/inspectable entities

StaticSpatialWorld
├─ map bounds
├─ tile-derived blocked geometry
└─ object-derived obstacle geometry
```

Not every map decoration becomes an entity.

The smoke world must contain:

```text
entity.player
entity.npc.smoke
object.tree.large.smoke
```

The player and NPC are runtime entities. The tree is initially a static authored map object unless implementation requires mutable runtime behavior. It must have stable authored identity and inspectable static-geometry evidence.

### 8. Map static-object extension

Extend the map contract with optional static object declarations while keeping existing maps valid.

Recommended shape:

```json
{
  "objects": [
    {
      "id": "object.tree.large.smoke",
      "kind": "static-obstacle",
      "assetId": "asset.tile-atlas-smoke",
      "position": { "x": 1.5, "y": 1.5 },
      "bounds": {
        "kind": "aabb",
        "halfWidth": 0.45,
        "halfHeight": 0.45
      }
    }
  ]
}
```

Initial supported object geometry:

```text
axis-aligned rectangle only
```

Validation must cover:

- stable unique object IDs;
- finite position and bounds values;
- positive half-extents;
- object bounds within or intentionally intersecting map bounds according to documented policy;
- supported kind and geometry;
- referenced asset existence when an asset is declared;
- deterministic ordering.

### 9. Continuous kinematic spatial module

Introduce module ID:

```text
spatial.continuous-kinematic-2d
```

The module owns:

```text
ContinuousTransform2 { X, Y }
KinematicMotion2 { VelocityX, VelocityY, MaxSpeed }
CollisionAabb2 { HalfWidth, HalfHeight }
```

World units:

```text
one authored map tile = 1.0 × 1.0 world units
```

Rendering pixels are outside this milestone.

Numeric policy:

- use finite `double` values;
- reject NaN and infinity;
- normalize `-0.0` where serialized;
- define deterministic rounding/serialization policy for evidence;
- compare semantic values using an explicitly documented tolerance only where exact arithmetic is not guaranteed;
- repeated runs on the same platform and runtime must produce equivalent semantic evidence.

### 10. Continuous movement intent and integration

Required behavior:

```text
behavior.player-move-east-continuous
```

Required intent:

```text
ContinuousMoveIntent
```

The intent expresses desired direction or displacement; it does not mutate transform state.

Required fixed-tick flow:

```text
read transform + motion from immutable snapshot
→ emit continuous movement intent
→ clamp/normalize desired motion
→ calculate requested displacement for one fixed tick
→ resolve against map bounds and static AABBs
→ return accepted/clipped/slid/blocked resolution
→ apply resulting transform/motion command
```

The initial smoke path must use a documented fixed timestep or one-tick normalized displacement. Do not derive simulation behavior from wall-clock elapsed time.

### 11. Collision and response policy

Initial collision sources:

- map bounds;
- map-derived blocked tile rectangles where available;
- map static-object AABBs.

Initial collision shape:

```text
entity AABB
```

Required response:

- deterministic axis-separated resolution;
- movement along X and Y evaluated in a documented stable order;
- clipping to the maximum valid displacement;
- sliding along the unblocked axis;
- blocked when neither axis can move;
- no penetration in final state.

Required resolution statuses:

```text
accepted
clipped
slid
blocked
```

A clipped or slid movement is a successful domain resolution when a non-zero valid displacement is applied.

No physics terminology such as force, impulse, mass, or rigid body may enter the core contracts.

### 12. Smoke scenarios

Required authored map:

```text
game/maps/smoke/map-continuous-smoke.map.json
map.continuous-smoke
```

Required scenarios:

```text
game/scenarios/smoke/entity-component-runtime-smoke.json
entity.component-runtime-smoke

game/scenarios/smoke/continuous-kinematic-movement-smoke.json
continuous.kinematic-movement-smoke

game/scenarios/smoke/continuous-kinematic-tree-collision-smoke.json
continuous.kinematic-tree-collision-smoke
```

Required behavior:

```text
behavior.player-move-east-continuous
```

The entity/component scenario must prove creation, component assignment, query, update, removal, and destruction through a bounded execution path.

The accepted movement scenario must prove continuous movement through unobstructed space.

The tree-collision scenario must prove clipping, sliding, or blocking against `object.tree.large.smoke`, unchanged or correctly resolved final transform, and useful evidence.

The NPC must remain independently queryable and unchanged unless the scenario explicitly moves it.

### 13. Runtime inspection and artifacts

Runtime inspection must expose:

- entity registry state;
- entity lifecycle events;
- registered component type IDs;
- component state by entity;
- snapshot fingerprints;
- component mutation commands and results;
- selected spatial module;
- requested displacement;
- collision candidates;
- map/static object source references;
- per-axis resolution;
- final applied displacement;
- final transforms and motion;
- assertions and diagnostics.

Review packs must include the new entity/component and continuous-spatial evidence, either within runtime inspection or through a dedicated group when existing grouping is insufficient.

### 14. Engineering wrappers

Add:

```bash
./eng/entity-runtime-smoke.sh
./eng/continuous-spatial-smoke.sh
./eng/m013-smoke.sh
```

`entity-runtime-smoke.sh` validates entity lifecycle, typed component storage, deterministic queries, snapshot behavior, command-buffered mutation, and grid-module migration.

`continuous-spatial-smoke.sh` validates unobstructed movement, map-bound collision, tree AABB collision, clipping/sliding/blocking, deterministic evidence, and unchanged NPC state.

`m013-smoke.sh` executes the complete mixed-world journey and final review-pack generation.

Wrappers must inspect meaningful semantic fields, not only process exit codes or file existence.

## Implementation constraints

- Entity existence and mutable component state are runtime-owned.
- Spatial modules own spatial component types and interpretation.
- Spatial modules must not own a separate private entity registry.
- Maps remain authored static content and are not converted wholesale into entities.
- Static map geometry and dynamic entity state must remain distinguishable in evidence.
- Behavior modules retain read-only snapshot/query access and intent emission only.
- Mutation occurs through validated commands after domain resolution.
- Existing behavior-phase ordering remains unchanged.
- Existing grid scenarios and wrappers must continue to pass.
- World units are independent of pixel dimensions.
- Use finite `double` values with explicit serialization normalization.
- Use AABBs only.
- Axis evaluation order must be documented and stable.
- Do not add a third-party physics package.
- Do not introduce a general ECS framework dependency.
- Do not optimize storage prematurely.
- `System.Text.Json` and current repository dependencies should be sufficient.
- Ordinary implementation agents must not read `.guide-profile.json`, `.guide-sync/`, copied research guides, prompt templates, or the external guide repository.

## Required authority documents

After unpacking the package, the implementation agent must read only:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/SCENARIOS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/engineering/future-dotnet-solution.md
docs/specs/runtime-principles.md
docs/specs/minimal-deterministic-runtime.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/behavior-modules.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/pluggable-spatial-runtime-contract.md
docs/specs/grid-spatial-module-contract.md
docs/specs/map-content-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/review-pack-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/mixed-world-projection-contract.md
docs/specs/continuous-kinematic-spatial-module-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/behavior-spatial-execution-artifact-contract.md
docs/artifacts/entity-component-continuous-spatial-artifact-contract.md
docs/decisions/ADR-0015-behaviors-emit-intents-and-spatial-modules-resolve-them.md
docs/decisions/ADR-0016-runtime-owns-entities-components-and-spatial-modules-own-spatial-semantics.md
docs/milestones/MILESTONE-013-entity-component-runtime-and-continuous-kinematic-spatial-module.md
```

Do not read external guide-system documents for implementation.

## Files or areas likely affected

Likely source areas:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Behaviors
src/Agentic2D.Spatial.Grid
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Tools
tests/unit/Agentic2D.Tests.Unit
```

A new project is justified if it creates a clear dependency boundary:

```text
src/Agentic2D.Entities
src/Agentic2D.Spatial.Continuous
```

Preferred dependency direction:

```text
Contracts
  ↑
Entities / Engine
  ↑
Spatial.Grid
Spatial.Continuous
  ↑
ScenarioRunner / Tools
```

Do not create one project per component, command, scenario, or artifact.

Likely authored content:

```text
game/maps/smoke/map-continuous-smoke.map.json
game/scenarios/smoke/entity-component-runtime-smoke.json
game/scenarios/smoke/continuous-kinematic-movement-smoke.json
game/scenarios/smoke/continuous-kinematic-tree-collision-smoke.json
```

Required engineering scripts:

```text
eng/entity-runtime-smoke.sh
eng/continuous-spatial-smoke.sh
eng/m013-smoke.sh
```

## Validation tiers and concrete repository commands

Use focused unit tests during implementation.

Required final validation:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/content-validate.sh maps
./eng/asset-inspect-smoke.sh
./eng/review-pack-smoke.sh
./eng/asset-curation-smoke.sh
./eng/asset-review-smoke.sh
./eng/asset-perception-smoke.sh
./eng/map-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/m011-smoke.sh
./eng/behavior-smoke.sh
./eng/grid-spatial-smoke.sh
./eng/m012-smoke.sh
./eng/entity-runtime-smoke.sh
./eng/continuous-spatial-smoke.sh
./eng/m013-smoke.sh
```

Required direct checks:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate game/maps/smoke/map-continuous-smoke.map.json --output artifacts/content/map-continuous-smoke

dotnet run --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/entity-component-runtime-smoke.json --output artifacts/content/entity-component-runtime-smoke

dotnet run --project src/Agentic2D.Tools -- scenario run entity.component-runtime-smoke --output artifacts/scenarios/entity-component-runtime-smoke

dotnet run --project src/Agentic2D.Tools -- scenario run continuous.kinematic-movement-smoke --output artifacts/scenarios/continuous-kinematic-movement-smoke

dotnet run --project src/Agentic2D.Tools -- scenario run continuous.kinematic-tree-collision-smoke --output artifacts/scenarios/continuous-kinematic-tree-collision-smoke

dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario continuous.kinematic-tree-collision-smoke --map map.continuous-smoke --output artifacts/runtime/continuous-kinematic-tree-collision-smoke

dotnet run --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/m013
```

If current CLI syntax differs, use the current authoritative syntax and update direct command documentation in the same change.

## Acceptance criteria

### Entity registry and lifecycle

1. Runtime owns one entity registry used by both grid and continuous scenarios.
2. Stable entity creation, existence query, deterministic enumeration, and destruction work.
3. Duplicate creation and unknown-entity mutation produce stable diagnostics.
4. Destroying an entity removes all owned components.
5. Entity lifecycle events and inspection evidence are complete.

### Typed component runtime

6. Explicitly registered typed component stores support set/get/remove and deterministic queries.
7. Component type IDs are stable and present in evidence.
8. Querying entities with one and two component types is deterministic.
9. Storage implementation does not expose mutable internals to behavior code.
10. No archetype/sparse-set/general ECS dependency is introduced.
11. Grid and continuous spatial components remain module-owned.
12. Runtime owns component instances and mutation.

### Snapshot and mutation boundary

13. Snapshots expose immutable entity/component queries.
14. All behaviors in a phase read the same snapshot.
15. A new snapshot reflects prior-tick accepted component mutations.
16. Snapshot fingerprints are deterministic and include component state.
17. Entity/component changes occur through validated commands or command buffer.
18. Failed commands leave previous state intact.

### Grid migration

19. `spatial.grid` uses runtime-owned entity/component state.
20. Existing Milestone 012 accepted/rejected and `each-tick` behavior remains correct.
21. No grid-specific API leaks into universal core/entity contracts.
22. All Milestone 012 wrappers pass unchanged or with direct compatible updates.

### Mixed authored world

23. `map.continuous-smoke` validates.
24. Map source includes stable static tree obstacle `object.tree.large.smoke`.
25. Player and NPC are runtime entities; the tree remains clearly static authored geometry unless explicitly justified otherwise.
26. Static and dynamic spatial data are distinct in artifacts.
27. World units consistently treat one tile as `1.0 × 1.0`.

### Continuous kinematic module

28. `spatial.continuous-kinematic-2d` registers explicitly.
29. `ContinuousTransform2`, `KinematicMotion2`, and `CollisionAabb2` are module-owned.
30. Unobstructed fixed-tick movement reaches the expected transform.
31. Movement cannot leave map bounds.
32. Tree collision produces deterministic clipping, sliding, or blocking.
33. Final state contains no AABB penetration.
34. The NPC remains independently queryable and unchanged in the smoke path.
35. Continuous movement uses no wall-clock time, forces, impulses, or third-party physics.
36. All numeric evidence is finite, normalized, and repeatable.

### Evidence and validation

37. Runtime inspection exposes entity registry, components, snapshot, intent, static geometry, collision candidates, per-axis resolution, command results, final transforms, assertions, and diagnostics.
38. Review pack includes useful entity/component/continuous-spatial evidence.
39. New wrappers validate semantic content, not only artifact existence.
40. Equivalent repeated runs produce semantically equivalent evidence.
41. All prior milestone validation gates continue to pass.

### Documentation and scope

42. Direct project-truth documentation is updated where current statements would otherwise be false.
43. ADR-0016 and Milestone 013 are indexed after acceptance.
44. No excluded final ECS, physics, platformer, rendering, save/load, packaging, guide, workflow, TBP, issue-template, public-doc, or release work is introduced.

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
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/specs/runtime-principles.md
docs/specs/minimal-deterministic-runtime.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/pluggable-spatial-runtime-contract.md
docs/specs/grid-spatial-module-contract.md
docs/specs/map-content-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/review-pack-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/behavior-spatial-execution-artifact-contract.md
```

Update a document during implementation only if the implemented behavior would otherwise contradict or remain undiscoverable in active authority. Do not perform unrelated prose cleanup.

## Deferred documentation synchronization hints

The package adds:

```text
.guide-sync/pending/2026-07-13-m013-index-and-crosslink-sync.md
.guide-sync/pending/2026-07-13-m013-human-review-and-runtime-boundary-followup.md
```

The implementation agent must not read these files.

## Human review requirements

Human review must verify:

1. entity identity is runtime-owned and not hidden inside spatial modules;
2. component stores do not expose mutable world access to behaviors;
3. grid and continuous component types remain module-owned;
4. the grid module migrated without semantic regression;
5. the static tree remains distinguishable from runtime entities;
6. world-unit and coordinate semantics are understandable;
7. axis-separated collision evidence explains requested and applied displacement;
8. no physics vocabulary or unnecessary backend abstraction entered core contracts;
9. the entity/component API is small enough to replace storage later;
10. a future health, inventory, animation, or dialogue component could use the same runtime foundation without spatial coupling.

## Out-of-scope guide migration work

No guide-system migration is part of this milestone.

Do not:

- change `.guide-profile.json`;
- copy guide documents or prompt templates;
- reference external guides as target-repository operational authority;
- require ordinary implementation agents to read `.guide-sync/`.
