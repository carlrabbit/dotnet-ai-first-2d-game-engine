# Milestone 014 — Authored Entity Definitions, Runtime Instantiation, Spatial Queries, Triggers, and Interactions

## Goal

Introduce reusable authored entity definitions and a deterministic instantiation pipeline, then use instantiated runtime entities to prove spatial queries, entity-based trigger transitions, and explicit interactions.

Required end-to-end journey:

```text
authored entity definitions
+ map entity spawns
+ bounded scenario overrides
→ transactional definition resolution and validation
→ runtime entity/component creation commands
→ continuously positioned player and NPC
→ player moves toward NPC
→ deterministic spatial query finds eligible entities
→ player enters entity-owned trigger volume
→ trigger.entered is emitted
→ player emits InteractIntent
→ interaction resolver selects the NPC
→ BeginInteractionCommand is applied
→ interaction.started is emitted
→ provenance, queries, transitions, resolutions, commands, events, and final state are inspectable
```

The milestone stops at `interaction.started`. It does not implement dialogue, inventory transfer, quests, harvesting, or other downstream interaction domains.

## Repository role and maturity assumptions

Repository role:

```text
capability-provider
```

The repository builds reusable engine/runtime/tooling capability. The authored definitions, map spawns, player, NPC, trigger, and interaction scenario are bounded dogfood fixtures used to validate capability implementation. They do not constitute a consumer game.

Maturity assumptions:

- implementation-ready;
- artifact-first;
- headless-first and CLI/API-first;
- Milestones 001 through 013 and all accepted Milestone 013 completion patches are implemented;
- runtime-owned entity identity, typed component state, immutable snapshots, command-buffered mutation, grid movement, continuous kinematic movement, map static geometry, runtime inspection, and review packs exist;
- static map content remains distinct from runtime entities;
- existing stable-ID, deterministic-ordering, diagnostic, status, exit-code, command/event/query, artifact, and review conventions remain authoritative.

## Execution mode

```text
ai-executed-broad
```

Implementation must proceed in the focus-area order below. Do not begin interaction resolution before entity definitions, instantiation, provenance, and spatial query semantics are stable and tested.

## Locked design decisions

- no entity-definition inheritance;
- definitions contain complete explicit component bundles;
- definitions may declare semantic tags, zero or one default active behavior, interaction capability, and an optional opaque visual asset reference;
- definition ID, spawn ID, and runtime entity ID are separate stable identities;
- maps and scenarios use one shared spawn contract;
- definition defaults are applied before map-spawn overrides, then scenario overrides;
- overrides replace complete component values;
- no partial JSON patch semantics;
- resulting bundles are validated after overrides;
- static objects and entity spawns are explicit separate authored forms;
- interactive world objects must be runtime entities;
- runtime instantiation is transactional;
- one focused dynamic spawn/despawn test is required, but dynamic spawning is not the main scenario;
- spatial queries are AABB overlap, radius/proximity, and entity spatial lookup only;
- deterministic query ordering is mandatory;
- entities do not physically block one another;
- trigger volumes are runtime entity components only;
- trigger transitions are `entered` and `exited`; `stayed` is deferred;
- trigger filters support explicit entity ID, required tags, and required component type IDs;
- interactions require explicit `InteractIntent`;
- interaction eligibility is radius-based;
- an explicit target ID wins when valid; otherwise choose nearest eligible target, then entity ID ordinal;
- `Interactable` declares interaction capability;
- interactions end at `interaction.started`;
- one active behavior per entity per phase remains;
- review-pack summary counts are optional; complete evidence and manifest discoverability are required.

## Scope

1. Authored entity-definition content domain.
2. Stable definition, spawn, and runtime-entity identity.
3. Explicit complete component bundles and behavior declarations.
4. Controlled semantic tags and optional opaque visual asset reference.
5. Shared map/scenario entity-spawn contract.
6. Whole-component replacement overrides.
7. Component-bundle compatibility validation.
8. Transactional runtime entity instantiation.
9. Runtime definition/spawn provenance.
10. One focused dynamic spawn/despawn test.
11. Read-only entity spatial lookup.
12. Deterministic AABB overlap queries.
13. Deterministic radius/proximity queries.
14. Entity-owned trigger-volume component.
15. Trigger filter contract.
16. `entered` and `exited` trigger transitions.
17. Trigger overlap-state ownership and evidence.
18. Explicit interaction behavior and `InteractIntent`.
19. Radius-based interaction eligibility.
20. Deterministic interaction target selection.
21. `Interactable` component.
22. `BeginInteractionCommand` and `interaction.started`.
23. Runtime inspection, artifacts, review-pack integration, tests, scenarios, and engineering wrappers.
24. One bounded player-to-NPC interaction smoke journey.

## Non-goals

Do not implement definition inheritance, definition fragments, traits, mixins, nested definitions, child entities, scene graphs, arbitrary JSON Patch, runtime hot reload, source-generated registries, multiple active behaviors per entity/phase, behavior priorities, entity/entity physical collision, spatial partitioning, raycasts, line of sight, navigation, pathfinding, static map triggers, `trigger.stayed`, arbitrary filter expressions, interaction callbacks, dialogue, inventory, quests, harvesting, combat, rendering, animation, audio, save/load, networking, packaged runtime, broad cleanup, workflows, TBPs, issue templates, public docs, release docs, or guide migration.

## Focus areas

### 1. Authored entity-definition domain

Add schema-validatable authored entity definitions.

Required IDs:

```text
entity-definition.player.basic
entity-definition.npc.talkable-smoke
```

Definitions may contain:

- stable definition ID;
- controlled semantic tags;
- component defaults;
- zero or one behavior assignment;
- interaction capability components;
- optional opaque `visualAssetId`.

Definitions must not contain runtime entity ID, world/map position, current mutable transform, scenario-specific state, or inheritance.

### 2. Definition validation

Validate:

- schema version;
- stable unique definition ID;
- tag syntax and deterministic canonicalization;
- known component type IDs;
- no duplicate component types;
- valid component values;
- zero or one behavior;
- known behavior/lifecycle when present;
- optional asset reference;
- absence of instance-only fields.

### 3. Component-bundle compatibility

Use explicit hand-written validation.

Initial rules:

```text
KinematicMotion2 requires ContinuousTransform2 after overrides.
CollisionAabb2 requires ContinuousTransform2 for continuous membership.
TriggerVolume2 requires ContinuousTransform2.
Interactable requires ContinuousTransform2.
Continuous movement behavior requires transform, motion, collision shape, and continuous membership.
GridPosition requires spatial.grid membership.
Continuous movement requires spatial.continuous-kinematic-2d membership.
GridPosition + ContinuousTransform2 is rejected as ambiguous spatial authority by default.
```

Do not build a generic dependency engine.

### 4. Shared entity-spawn contract

Maps and scenarios use one semantic spawn contract.

Required identities:

```text
definitionId
spawnId
entityId
```

Recommended shape:

```json
{
  "id": "spawn.npc.talkable-smoke",
  "entityId": "entity.npc.talkable-smoke",
  "definitionId": "entity-definition.npc.talkable-smoke",
  "overrides": [
    {
      "componentType": "component.continuous-transform-2d",
      "value": { "x": 3.5, "y": 1.5 }
    },
    {
      "componentType": "component.spatial-membership",
      "value": {
        "worldId": "map.interaction-smoke",
        "spatialModuleId": "spatial.continuous-kinematic-2d"
      }
    }
  ]
}
```

Rules:

- identities are explicit;
- overrides replace whole components;
- no partial patches;
- final bundle validation occurs after all layers;
- spawn-time instance components such as transform and membership may be added when explicitly supported.

Precedence:

```text
definition defaults
→ map spawn overrides
→ scenario overrides
```

### 5. Transactional runtime instantiation

Required flow:

```text
resolve definition
→ merge full-component overrides
→ validate resulting bundle
→ validate behavior
→ generate ordered runtime commands
→ apply only when validation succeeds
→ record provenance and evidence
```

Command order:

```text
CreateEntity
→ provenance
→ components ordered by component type ID
→ behavior assignment
```

Loaders must not mutate stores directly.

At minimum, each spawn is atomic. Prefer validating all required initial spawns before any commit.

### 6. Runtime provenance

Every instantiated entity retains immutable provenance:

```text
definitionId
spawnId
sourceKind
sourceId/sourcePath
mapId
scenarioId
override summary
behavior source
```

Provenance is inspectable and not gameplay-mutable.

### 7. Focused dynamic spawn/despawn path

Use the same instantiation service in one focused test:

```text
instantiate entity-definition.npc.talkable-smoke dynamically
→ verify components and provenance
→ destroy entity
→ verify component removal and lifecycle evidence
```

No pooling, respawn, or scheduling.

### 8. Spatial query boundary

Required read-only operations:

```text
lookup entity spatial state
query entities overlapping an AABB
query entities within radius
```

The first implementation may scan entities deterministically.

### 9. Spatial query semantics

AABB overlap results:

```text
entity ID ordinal
```

Radius results:

```text
distance ascending
→ entity ID ordinal
```

Radius uses transform-position distance, must be finite and non-negative, and supports an excluded entity ID.

### 10. Query filters

Supported predicates:

```text
explicit entity ID
required semantic tags
required component type IDs
```

Predicates combine with logical AND.

No arbitrary expressions, regex, scripts, or callbacks.

### 11. Trigger-volume component

Add:

```text
component.trigger-volume-2d
```

It is an entity-owned AABB centered on continuous transform.

Required fields:

- finite positive half-width;
- finite positive half-height;
- filter;
- optional trigger ID.

It is non-solid. Static map triggers are excluded.

### 12. Trigger-state ownership and transitions

Runtime owns prior overlap state:

```text
(triggerId, entityId)
```

Per tick:

```text
entered = current - previous
exited = previous - current
```

Required events:

```text
trigger.entered
trigger.exited
```

No `trigger.stayed`.

Trigger evaluation occurs after accepted movement/component mutations. First qualifying evaluation emits `trigger.entered`.

Ordering:

```text
trigger ID ordinal
→ entity ID ordinal
→ entered before exited
```

### 13. Interaction capability

Add:

```text
component.interactable
```

Fields:

```text
interactionKind
range
allowedInteractorTags
optional requiredInteractorComponents
```

Initial kind:

```text
interaction.talk
```

No callbacks or script references.

### 14. Interaction initiation

Add:

```text
behavior.player-interact
InteractIntent
```

Intent fields:

```text
intent ID
interactor entity ID
optional explicit target entity ID
optional requested interaction kind
behavior assignment ID
ordering key
```

Behavior does not mutate interaction state.

### 15. Interaction resolver

Flow:

```text
read interactor snapshot
→ validate spatial state
→ collect radius candidates
→ apply Interactable filters
→ honor valid explicit target
→ otherwise nearest candidate
→ entity ID tie-break
→ accepted/rejected InteractionResolution
```

Accepted resolution returns:

```text
BeginInteractionCommand
```

Accepted command emits:

```text
interaction.started
```

Payload includes kind, interactor, target, intent, and tick.

The milestone stops there.

### 16. Interaction rejection

Required reasons:

```text
interactor-not-found
interactor-spatial-state-missing
explicit-target-not-found
explicit-target-ineligible
no-eligible-target-in-range
interaction-kind-mismatch
```

Rejected interaction emits no `interaction.started`.

### 17. Primary smoke world

Required map:

```text
game/maps/smoke/map-interaction-smoke.map.json
map.interaction-smoke
```

Required definitions:

```text
game/entities/entity-definition.player.basic.json
game/entities/entity-definition.npc.talkable-smoke.json
```

Required runtime entities:

```text
entity.player
entity.npc.talkable-smoke
```

The NPC owns the trigger volume and `Interactable`. Retain one static obstacle to preserve the mixed-world boundary.

### 18. Required scenarios

```text
game/scenarios/smoke/entity-definition-instantiation-smoke.json
entity.definition-instantiation-smoke

game/scenarios/smoke/trigger-enter-exit-smoke.json
trigger.enter-exit-smoke

game/scenarios/smoke/npc-interaction-smoke.json
interaction.npc-smoke
```

The instantiation scenario proves definition resolution, override precedence, bundle validation, transactional creation, provenance, and command order.

The trigger scenario proves outside → inside → `trigger.entered`, then inside → outside → `trigger.exited`, with no `trigger.stayed`.

The interaction scenario proves player/NPC instantiation, movement into range, `InteractIntent`, deterministic candidate selection, accepted command, and `interaction.started`.

Focused tests must cover explicit target, nearest target, equal-distance tie-break, no target, and ineligible explicit target.

### 19. Runtime inspection and artifacts

Inspection must expose:

- resolved definitions;
- definition/spawn/entity IDs;
- override layers and merged bundle;
- generated commands;
- instantiation status;
- provenance;
- query inputs, filters, candidates, ordering, and results;
- trigger previous/current overlaps;
- entered/exited transitions;
- interaction intent;
- candidates and distances;
- target selection and reason;
- command/rejection;
- events, assertions, diagnostics;
- final entity/component state.

Review packs use the existing runtime-inspection family unless a dedicated group is necessary.

### 20. Engineering wrappers

Add:

```bash
./eng/entity-definition-smoke.sh
./eng/spatial-query-trigger-smoke.sh
./eng/interaction-smoke.sh
./eng/m014-smoke.sh
```

Wrappers must validate semantic fields, not only file existence or exit codes.

## Implementation constraints

- Runtime owns entity identity and component instances.
- Definitions are authored defaults, not runtime objects.
- Definition, spawn, and entity IDs stay distinct.
- No inheritance or implicit composition.
- Override precedence is explicit and deterministic.
- Whole-component replacement only.
- Instantiation generates normal validated commands.
- No direct store mutation by loaders, behaviors, query services, trigger systems, or resolvers.
- Static objects remain distinct from entity spawns.
- Interactive world objects must be entities.
- Existing grid and continuous semantics remain unchanged.
- Spatial queries are read-only.
- Entity overlap does not imply blocking.
- Trigger volumes are non-solid.
- Trigger state is runtime-owned and inspectable.
- Interaction requires explicit intent.
- Target selection is deterministic.
- One behavior per entity per phase remains.
- `interaction.started` is the terminal gameplay event.
- No third-party ECS, spatial index, physics, scripting, or interaction framework.
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
docs/specs/behavior-modules.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/pluggable-spatial-runtime-contract.md
docs/specs/map-content-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/review-pack-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/mixed-world-projection-contract.md
docs/specs/continuous-kinematic-spatial-module-contract.md
docs/specs/entity-definition-and-instantiation-contract.md
docs/specs/spatial-query-and-trigger-contract.md
docs/specs/interaction-runtime-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/entity-component-continuous-spatial-artifact-contract.md
docs/artifacts/entity-instantiation-query-trigger-interaction-artifact-contract.md
docs/decisions/ADR-0016-runtime-owns-entities-components-and-spatial-modules-own-spatial-semantics.md
docs/decisions/ADR-0017-authored-definitions-instantiate-runtime-entities-and-interactions-use-explicit-intents.md
docs/milestones/MILESTONE-013-entity-component-runtime-and-continuous-kinematic-spatial-module.md
docs/milestones/MILESTONE-014-authored-entity-definitions-runtime-instantiation-spatial-queries-triggers-and-interactions.md
```

Do not read external guide documents.

## Files or areas likely affected

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Entities
src/Agentic2D.Behaviors
src/Agentic2D.Spatial.Continuous
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Tools
tests/unit/Agentic2D.Tests.Unit
```

A new `src/Agentic2D.Interactions` project is justified only if it creates a clear reusable boundary. Do not create one project per query, trigger, interaction, scenario, or artifact.

Likely authored content:

```text
game/entities/entity-definition.player.basic.json
game/entities/entity-definition.npc.talkable-smoke.json
game/maps/smoke/map-interaction-smoke.map.json
game/scenarios/smoke/entity-definition-instantiation-smoke.json
game/scenarios/smoke/trigger-enter-exit-smoke.json
game/scenarios/smoke/npc-interaction-smoke.json
```

Required scripts:

```text
eng/entity-definition-smoke.sh
eng/spatial-query-trigger-smoke.sh
eng/interaction-smoke.sh
eng/m014-smoke.sh
```

## Validation tiers and concrete repository commands

Use focused tests during implementation.

Required final validation:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/content-validate.sh maps
./eng/content-validate.sh entities
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
./eng/entity-definition-smoke.sh
./eng/spatial-query-trigger-smoke.sh
./eng/interaction-smoke.sh
./eng/m014-smoke.sh
```

Required direct checks:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate entities --output artifacts/content/entities
dotnet run --project src/Agentic2D.Tools -- content validate game/maps/smoke/map-interaction-smoke.map.json --output artifacts/content/map-interaction-smoke
dotnet run --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/entity-definition-instantiation-smoke.json --output artifacts/content/entity-definition-instantiation-smoke
dotnet run --project src/Agentic2D.Tools -- scenario run entity.definition-instantiation-smoke --output artifacts/scenarios/entity-definition-instantiation-smoke
dotnet run --project src/Agentic2D.Tools -- scenario run trigger.enter-exit-smoke --output artifacts/scenarios/trigger-enter-exit-smoke
dotnet run --project src/Agentic2D.Tools -- scenario run interaction.npc-smoke --output artifacts/scenarios/interaction-npc-smoke
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario interaction.npc-smoke --map map.interaction-smoke --output artifacts/runtime/interaction-npc-smoke
dotnet run --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/m014
```

Use current authoritative CLI syntax if it differs.

## Acceptance criteria

1. Both required entity definitions validate.
2. Definitions use complete bundles and no inheritance.
3. Invalid definition/component/behavior data produces stable diagnostics.
4. Definition, spawn, and entity IDs are explicit and distinct.
5. Maps and scenarios share one spawn contract.
6. Override precedence is definition → map spawn → scenario.
7. Overrides replace whole components.
8. Resulting bundles validate after overrides.
9. Invalid instantiation does not partially commit.
10. Instantiation generates deterministic runtime commands.
11. Instantiation does not mutate stores directly.
12. Generated component commands are ordered by component type ID.
13. Entities retain immutable provenance.
14. Focused dynamic spawn/despawn uses the same instantiation service.
15. Entity spatial lookup works.
16. AABB queries order by entity ID.
17. Radius queries order by distance then entity ID.
18. ID/tag/component filters use AND semantics.
19. Queries are read-only.
20. Entities do not physically block each other.
21. `component.trigger-volume-2d` validates and is non-solid.
22. Trigger overlap state is runtime-owned.
23. Outside → inside emits exactly one `trigger.entered`.
24. Inside → outside emits exactly one `trigger.exited`.
25. No `trigger.stayed` is emitted.
26. Trigger filtering and ordering are deterministic.
27. Initial-overlap policy is implemented and documented.
28. `component.interactable` validates.
29. `behavior.player-interact` emits intent without mutation.
30. Explicit valid target wins.
31. Otherwise nearest eligible target wins.
32. Equal distance uses entity ID ordinal.
33. Eligibility is radius-based.
34. Accepted resolution produces `BeginInteractionCommand`.
35. Accepted command emits `interaction.started`.
36. Rejected interaction emits no `interaction.started`.
37. Required rejection reasons have focused tests.
38. No downstream interaction domain is implemented.
39. Runtime inspection exposes definitions, spawns, overrides, provenance, queries, triggers, and interactions.
40. Required evidence files contain real records, not placeholders.
41. Review-pack manifest makes evidence discoverable.
42. New wrappers validate semantic fields.
43. Equivalent runs produce equivalent evidence.
44. All Milestone 013 and earlier gates pass.
45. Direct docs are updated where necessary.
46. ADR-0017 and Milestone 014 are indexed after acceptance.
47. No excluded scope is introduced.

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
docs/engineering/future-dotnet-solution.md
docs/specs/runtime-principles.md
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/behavior-modules.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/map-content-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/review-pack-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/mixed-world-projection-contract.md
docs/specs/continuous-kinematic-spatial-module-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/entity-component-continuous-spatial-artifact-contract.md
```

Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-13-m014-index-and-crosslink-sync.md
.guide-sync/pending/2026-07-13-m014-human-review-and-interaction-boundary-followup.md
```

The implementation agent must not read these files.

## Human review requirements

Reviewers must verify:

1. definitions are authored defaults, not mutable runtime objects;
2. definition, spawn, and runtime entity IDs remain distinct;
3. override precedence is visible;
4. invalid instantiation cannot partially commit;
5. provenance explains every entity;
6. static objects were not silently converted into entities;
7. spatial queries are read-only and deterministic;
8. trigger volumes are non-solid;
9. trigger overlap state and entered/exited evidence are reconstructable;
10. interaction requires explicit intent;
11. target selection is deterministic and visible;
12. `interaction.started` is the terminal boundary;
13. one-behavior-per-entity-per-phase remains;
14. future dialogue/inventory/quest modules can consume the event without changing current contracts.

## Out-of-scope guide migration work

No guide migration is included. Do not change `.guide-profile.json`, copy guides or prompt templates, reference external guides as operational authority, or require ordinary implementation agents to read `.guide-sync/`.
