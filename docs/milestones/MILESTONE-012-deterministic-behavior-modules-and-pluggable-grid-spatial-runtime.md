# Milestone 012 — Deterministic Behavior Modules and Pluggable Grid Spatial Runtime

## Goal

Introduce deterministic compiled behavior modules and connect them to a pluggable spatial-resolution boundary through one bounded grid-movement reference slice.

```text
scenario selects behavior + spatial module
→ behavior reads one immutable world snapshot
→ behavior emits movement intent
→ grid resolver evaluates authored map and approved semantics
→ resolver accepts or rejects
→ runtime applies accepted command
→ events, assertions, diagnostics, inspection evidence, and review pack are produced
```

The grid implementation is a reference capability, not the engine's universal movement model.

## Repository role and maturity assumptions

Repository role: `capability-provider`.

The repository implements reusable runtime/tooling capability. Smoke behavior, scenario, map, and asset fixtures are bounded dogfood used only to validate the capability.

Assumptions:

- implementation-ready, artifact-first, headless-first, CLI/API-first;
- Milestones 001–011 are implemented;
- commands, queries, events, scenarios, maps, runtime inspection, and review packs exist;
- C# is the primary behavior language;
- existing stable-ID, diagnostic, status, exit-code, artifact, review, and deterministic-ordering rules remain authoritative.

## Execution mode

`ai-executed-broad`

Implementation must proceed in the focus-area order below.

## Scope

1. Compiled C# behavior contract and explicit registry.
2. Scenario-owned behavior activation.
3. `once` and `each-tick` lifecycle modes; smoke uses `once`.
4. Immutable snapshot reads per behavior phase.
5. Intent collection before mutation.
6. Narrow pluggable spatial resolver/query contracts.
7. One `spatial.grid` implementation.
8. Module-owned `GridPosition`.
9. Approved tile semantics plus explicit map-cell override.
10. Conservative unresolved/blocked default.
11. Accepted and rejected movement evidence.
12. Deterministic random-source contract with focused tests.
13. Scenario validation, runtime inspection, review-pack, and wrapper integration.
14. One end-to-end behavior/grid smoke journey.

## Non-goals

Do not implement runtime C# compilation, reflection discovery, source-generated registration, external behavior assemblies, F#, interactive editor integration, continuous motion, platformer physics, gravity, jumping, slopes, rigid-body collision, pathfinding, entity occupancy blocking, generalized behavior priorities, multiple active behaviors per entity/phase, renderer integration, packaged runtime, broad documentation synchronization, workflows, TBPs, issue templates, public docs, release docs, or guide migration.

## Focus areas

### 1. Behavior modules

Add compiled C# behavior modules that:

- read through immutable snapshot/query APIs;
- emit intents;
- never mutate world state directly;
- use stable behavior IDs;
- register explicitly in code;
- receive deterministic services only.

Required behavior: `behavior.player-move-east`.

### 2. Scenario activation and lifecycle

Extend scenario content with:

```json
{
  "runtime": { "spatialModule": "spatial.grid" },
  "behaviors": [
    {
      "id": "assignment.player-move-east",
      "entityId": "entity.player",
      "behaviorId": "behavior.player-move-east",
      "lifecycle": "once"
    }
  ]
}
```

Supported lifecycles: `once`, `each-tick`.

Reject duplicate assignment IDs, multiple active behaviors for one entity/phase, unsupported behavior IDs, unsupported lifecycles, missing target entities, and unknown spatial modules.

Required authored scenario:

```text
game/scenarios/smoke/behavior-grid-movement-smoke.json
behavior.grid-movement-smoke
```

### 3. Execution phases

Required order:

```text
1. create immutable snapshot
2. run scheduled behaviors against that snapshot
3. collect intents
4. sort intents deterministically
5. resolve through selected domain modules
6. validate and apply accepted commands
7. emit events and diagnostics
8. evaluate assertions
9. write inspection evidence
```

All behaviors in one phase observe the same pre-command snapshot.

### 4. Intent/command separation

Behavior emits `MoveIntent`. The spatial resolver returns either an accepted `MoveEntityCommand` or a rejected domain resolution. Behavior must not emit position mutation directly.

Movement rejection is a normal domain outcome and may be asserted without CLI exit code `3`.

### 5. Pluggable spatial boundary

Use narrow contracts for movement-intent resolution, spatial-position query, and authored spatial-semantic query. The engine core must not expose grid-specific APIs.

Initial module ID: `spatial.grid`. Registration is explicit; no reflection scanning.

### 6. Grid reference module

The grid module owns `GridPosition { X, Y }` and supports one-cell cardinal movement.

Resolution order:

```text
map-cell override
→ approved referenced-tile physical behavior
→ blocked/unresolved
```

Check map bounds. Visual labels never imply walkability. Entity occupancy is deferred.

### 7. Deterministic randomness

Add a scenario-seeded deterministic random-source contract. No `Random.Shared` or wall-clock seed. The main smoke remains non-random; use focused tests only.

### 8. Runtime evidence

Runtime inspection must expose behavior assignments, selected spatial module, execution phase, snapshot fingerprint, intents, queried cell/tile semantics, resolver result, accepted command or rejection, events, final grid position, assertions, and diagnostics.

Recommended event IDs:

```text
behavior.started
behavior.completed
behavior.intent-emitted
spatial.movement-accepted
spatial.movement-rejected
entity.grid-position-changed
```

Review packs must include the new behavior/spatial evidence.

### 9. Engineering wrappers

Add:

```bash
./eng/behavior-smoke.sh
./eng/grid-spatial-smoke.sh
./eng/m012-smoke.sh
```

`behavior-smoke.sh` validates registration, activation, lifecycle, intent emission, and determinism.

`grid-spatial-smoke.sh` validates accepted and rejected movement, bounds, semantic precedence, and conservative fallback.

`m012-smoke.sh` executes the complete scenario-to-review-pack journey.

## Implementation constraints

- Product behavior belongs behind runtime/scenario APIs, not only shell wrappers.
- Behavior modules are compiled C# and explicitly registered.
- Behavior context is read-only except for intent emission.
- One behavior per entity per phase.
- Intent resolution order is stable.
- Grid position is module-owned, not a universal engine type.
- Physical semantics come only from approved metadata or explicit map overrides.
- Missing semantics are not silently walkable.
- Reuse existing map, scenario, runtime-inspection, and review-pack logic.
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
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/specs/runtime-principles.md
docs/specs/minimal-deterministic-runtime.md
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/behavior-modules.md
docs/specs/map-content-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/review-pack-contract.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/pluggable-spatial-runtime-contract.md
docs/specs/grid-spatial-module-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/behavior-spatial-execution-artifact-contract.md
docs/decisions/ADR-0015-behaviors-emit-intents-and-spatial-modules-resolve-them.md
docs/milestones/MILESTONE-012-deterministic-behavior-modules-and-pluggable-grid-spatial-runtime.md
```

Do not read external guide documents for implementation.

## Files or areas likely affected

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Tools
tests/unit/Agentic2D.Tests.Unit
game/scenarios/smoke/behavior-grid-movement-smoke.json
eng/behavior-smoke.sh
eng/grid-spatial-smoke.sh
eng/m012-smoke.sh
```

Optional shared projects only when dependency boundaries justify them:

```text
src/Agentic2D.Behaviors
src/Agentic2D.Spatial.Grid
```

Do not create one project per command or behavior.

## Validation tiers and concrete commands

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
```

Required direct checks:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/behavior-grid-movement-smoke.json --output artifacts/content/behavior-grid-movement-smoke
dotnet run --project src/Agentic2D.Tools -- scenario run behavior.grid-movement-smoke --output artifacts/scenarios/behavior-grid-movement-smoke
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario behavior.grid-movement-smoke --map map.smoke --output artifacts/runtime/behavior-grid-movement-smoke
dotnet run --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/m012
```

## Acceptance criteria

1. Compiled C# behaviors execute through a stable contract and explicit registry.
2. Behavior code has snapshot/query access and intent emission only.
3. No behavior directly mutates world state.
4. All behaviors in a phase observe one immutable snapshot.
5. Intents resolve in deterministic order.
6. Spatial contracts remain free of grid-specific core APIs.
7. `behavior.grid-movement-smoke` validates and runs.
8. Scenario-owned activation, `once`, and `each-tick` are implemented and tested.
9. Duplicate/unknown behavior and module assignments produce stable diagnostics.
10. `GridPosition` is grid-module-owned.
11. Accepted east movement reaches the expected destination.
12. Out-of-bounds movement is rejected without state change.
13. Map-cell override takes precedence over approved tile semantics.
14. Missing semantics reject conservatively.
15. Visual labels never grant movement.
16. Rejection is a normal domain result with evidence.
17. Deterministic random service is scenario-seeded and repeatable.
18. Runtime inspection exposes assignment, intent, resolver, semantic source, resolution, events, final grid position, assertions, and diagnostics.
19. Review packs include behavior/spatial evidence.
20. New wrappers validate meaningful state and all Milestone 011 gates still pass.
21. Direct project-truth docs are updated; ADR-0015 and Milestone 012 are indexed after acceptance.
22. No excluded scripting, physics, discovery, occupancy, pathfinding, renderer, guide, workflow, TBP, issue-template, public-doc, or release-doc work is introduced.

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
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/specs/behavior-modules.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/review-pack-contract.md
```

Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-13-m012-index-and-crosslink-sync.md
.guide-sync/pending/2026-07-13-m012-human-review-and-abstraction-followup.md
```

The implementation agent must not read these files.

## Human review requirements

Reviewers must verify that behavior cannot bypass intents, grid concepts did not leak into universal core contracts, phase snapshots and ordering are visible, semantic sources are explicit, rejection is not misclassified, unresolved semantics are conservative, deterministic randomness is credible, and a future continuous/platformer module could be added without rewriting core phases.

## Out-of-scope guide migration work

No guide migration is included. Do not change `.guide-profile.json`, copy guide documents, add prompt templates, or require implementation agents to read guide metadata.
