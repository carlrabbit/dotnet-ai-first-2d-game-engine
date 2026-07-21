# Milestone 032 — Autonomous Detailed-Region Work and Logistics Vertical Slice

## Goal

Turn the completed M031 simulation foundation into the first player-visible autonomous simulation loop in one detailed active region:

```text
player designations and fixed settlement policy
→ derived work opportunities
→ deterministic worker selection
→ reservations and explicit activities
→ detailed grid pathfinding and movement
→ semantic interaction execution
→ carried-resource logistics
→ storage and consumption outcomes
→ structural and graphical evidence
```

Primary acceptance question:

> Can a player designate extraction and storage areas, then observe generic autonomous workers repeatedly select, reach, execute, haul, deposit, and resume work while fixed basic needs interrupt safely and every decision remains explainable?

M032 uses only detailed active-region execution. Abstract discrete-event execution and multi-fidelity region switching remain deferred to M033.

## Repository role and maturity assumptions

```text
repository role: capability-provider
bounded dogfood: one third-game detailed-region vertical slice
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation, UI/component
maturity: implementation-ready, artifact-first
execution mode: ai-executed-broad
```

M031 is complete and authoritative. The repository now provides one persistent partitioned simulation world, semantic time, activities, reservations, deterministic commands/events, canonical persistence, inspection, and a headless wood-workflow proof.

M032 is mixed provider/dogfood work:

- provider responsibility: reusable autonomous-work, detailed execution, pathfinding, logistics, explanation, and projection capabilities;
- bounded dogfood responsibility: one playable forest-to-storage region using generic workers and procedural/existing placeholder presentation.

M030 remains deferred. M032 must not consume promoted assets as a new game integration milestone.

## Execution mode

`ai-executed-broad`

Implement as six coherent transformations:

1. player designations, demand discovery, and derived work opportunities;
2. deterministic eligibility, scoring, assignment, and decision explanations;
3. detailed grid pathfinding, interaction positions, route following, and invalidation;
4. staged detailed activity execution and semantic interaction effects;
5. carried-resource logistics, storage, and fixed basic-needs interruptions;
6. minimal designation/inspection presentation, artifacts, resumable validation, and blocking human review.

## Scope

### One detailed active region

M032 operates one selected region in detailed fixed-step execution. The active region includes exact grid positions, deterministic pathfinding, configured occupancy, interaction positions, route following, semantic activity progress, player designations, and minimal visual projection.

Other regions remain persistent but are not advanced by a new abstract executor. M032 must not imply background simulation.

### Generic workers

Workers have no personalities, traits, preferences, relationships, skill progression, or player-assigned schedules. Allowed mechanical state includes region/grid position, movement/work capability, carrying capacity, fixed needs, inventory, activity, availability, detailed execution state, and presentation binding.

Workers select work autonomously. Direct player assignment is not normal gameplay.

### Player designations

Provide reusable designation concepts and a bounded player surface for:

```text
resource-extraction area
storage area
farmland area definition only
construction area definition only
```

Only extraction and storage require complete gameplay execution in M032.

Required operations: create, inspect, enable/disable, change priority, remove, resolve affected work, and emit deterministic events/artifacts. Screen coordinates and drag state are presentation state only.

### Work opportunity derivation

World state and designations derive current opportunities for:

```text
harvest resource
haul resource stack
deposit carried resource
satisfy food need
satisfy water need
satisfy comfort/rest need
```

A work opportunity is derived inspectable state unless later evidence justifies durable identity. It includes a deterministic key, family, region, target/destination, quantity, source designation/policy, eligibility, priority, prerequisites, blocking reason, and derivation fingerprint.

Regeneration must not duplicate resources, activities, or reservations.

### Deterministic worker selection

Required factors:

1. active/available worker;
2. active region;
3. capability;
4. target/destination validity;
5. reservation availability;
6. mandatory need policy;
7. designation/work priority;
8. estimated path cost;
9. continuation/interruption cost;
10. stable tie-break.

No opaque AI, machine learning, hidden randomness, or nondeterministic heuristic is permitted.

Selection evidence includes selected opportunity, candidate list, eligibility, factor values, rejection codes, tie-break, reservation result, path estimate, and resulting activity.

### Work coordination lifecycle

```text
derive
→ evaluate
→ select
→ reserve
→ create/start M031 activity
→ execute detailed stages
→ complete, interrupt, fail, or replan
→ release/transfer reservations
→ regenerate affected opportunities
```

Blocking changes include designation removal, target depletion, full destination, route invalidation, mandatory need, competing reservation, worker deactivation, and storage policy change. Silent deadlock is prohibited.

### Detailed pathfinding

Add deterministic four-directional grid pathfinding consistent with `spatial.grid`:

- map bounds and current walkability authority;
- registered entity occupancy;
- deterministic adjacent interaction-position selection;
- shortest path with stable equal-cost ties;
- path-not-found diagnostics;
- spatial revision and route fingerprint;
- invalidation on relevant map, occupancy, target, or destination changes;
- bounded replanning;
- no domain mutation by pathfinding.

This is detailed execution support, not the future abstract travel model.

### Movement and detailed executor

Detailed movement uses fixed-step semantic progress. Route state includes actor/activity, destination, ordered cells, current index, route revision, blocked/replan count, and status.

Provide a detailed execution strategy for:

```text
travel
harvest
pick up
carry
deposit
eat
drink
rest
```

The executor owns transient route/progress state. At semantic boundaries it issues validated M031 commands. It never mutates resource, inventory, storage, needs, reservations, or activity completion directly.

### Logistics

Required concepts: harvestable source, integer resource quantity, world stack, worker inventory, storage acceptance/capacity, quantity/capacity reservation, pickup, carry, deposit, overflow rejection, and conservation inspection.

Complete loop:

```text
designated tree
→ harvest activity
→ wood becomes authoritative stack/inventory
→ haul opportunity
→ storage selected
→ reservations
→ pickup
→ carry
→ deposit
→ reservations released
```

### Fixed basic needs

Provide identical fixed policies for food, water, and comfort/rest:

- deterministic accumulation by simulation time;
- warning and mandatory thresholds;
- bounded fixed sources in the proof region;
- autonomous need opportunities;
- mandatory interruption of interruptible work;
- bounded non-interruptible commit windows;
- correct reservation handling;
- satisfaction through commands/events.

No death, health, mood, personality, individualized tuning, or player-controlled thresholds.

### Explainability

For every worker decision expose current activity, selected opportunity, candidate opportunities, factor values, rejected alternatives, reservations, path estimate, interruption policy, and last replan/failure.

### Minimal presentation and input

Use existing input, render projection, animation, sound, and raylib debug-client capability.

Required functions:

- select extraction/storage tools and create bounded areas;
- enable/disable/remove designation;
- select worker/target and inspect current activity/reason;
- pause/resume/single-step and bounded speed where supported;
- distinguish terrain, trees, storage, workers, carried wood, and designation overlays;
- minimal walking/facing;
- semantic overlays for harvest, pickup, deposit, eat, drink, and rest;
- explicit route/debug overlay;
- need and blocked/replan indicators.

Use procedural shapes, checked-in smoke assets, or existing legal placeholders. Do not require M029 promotion or reopen M030.

### Persistence

Persist designations, capabilities/needs, inventories/stacks, storage, current activity, reservations, and semantic intent. Detailed paths and presentation state are transient or rebuildable unless a bounded continuation field is explicitly approved.

After load, routes may be recalculated; no resource or reservation may duplicate.

### Bounded vertical-slice proof

Provide one region with two generic workers, at least six trees, extraction and storage designations, finite wood storage, food/water/rest sources, one temporary path blockage, one need interruption, one designation change, and save/load during carrying.

The deterministic replay must reach a declared stored-wood target without duplication or deadlock.

## Non-goals

Do not implement abstract simulation, region catch-up, fidelity switching, cross-region logistics, multiple detailed regions, complete farming/construction, production networks, environmental infrastructure, personalities, skills, health, combat, direct task assignment, player-controlled needs, advanced crowd simulation, diagonal/navmesh movement, vehicles, multithreaded pathfinding, ECS archetype rewrite, dynamic plugins, complete operations UI, audiovisual polish, M030 integration, Linux release closure, guide migration, TBPs, issue templates, or workflow YAML.

## Focus Area 1 — Designations and derived work

Authority:

- `docs/specs/autonomous-work-and-detailed-logistics-contract.md`;
- `docs/architecture/autonomous-detailed-region-execution-architecture.md`;
- `docs/scenarios/m032-detailed-forest-logistics-vertical-slice.md`.

Required outcomes: stable designation identity/cells, deterministic commands/events, opportunity derivation/invalidation, harvest/haul/deposit/need families, persistence, inspection, and diagnostics.

Blocking cases: overlap, removal during active work, disabled designation, depleted target, full storage, stale derivation fingerprint, save/load, and deterministic regeneration.

## Focus Area 2 — Selection and explanation

Required outcomes: generic capability model, explicit candidate evaluation, deterministic ordering/ties, reservation-before-start, continuation/interruption policy, and structured explanation.

Blocking cases: two workers choose one target, equal tie, no eligible worker, mandatory need, lost reservation, invalidation between selection and commit, and replay.

## Focus Area 3 — Navigation, occupancy, and movement

Authority:

- `docs/specs/detailed-grid-navigation-and-activity-execution-contract.md`;
- existing grid/spatial/query contracts.

Required outcomes: deterministic shortest paths, interaction positions, occupancy, route artifacts, fixed-step movement, invalidation/replan, diagnostics, and no domain mutation.

Blocking cases: equal-cost ties, unreachable target, one reachable adjacent cell, worker contention, temporary/permanent blockage, load reconstruction, and map edge.

## Focus Area 4 — Detailed activity execution and logistics

Required outcomes: detailed executor advances M031 activities, command-only semantic completion, harvest/pickup/carry/deposit, finite storage, reservations, conservation, interruption-safe carrying, and duplicate rejection.

## Focus Area 5 — Needs and interruption

Required outcomes: deterministic food/water/comfort accumulation, thresholds, need opportunities, interruption/re-evaluation, satisfaction commands/events, and explanation.

## Focus Area 6 — Playable projection, evidence, and review

Required outcomes: headless replay, structural frames, graphics-capable proof, designation/worker/activity/need inspection, route overlay, bounded review pack, and resumable validation. Structural evidence remains semantic authority; the graphical client remains an adapter.

## Implementation constraints

- Use M031 world, regions, time, commands/events, activities, reservations, persistence, fingerprints, and inspection. Do not fork them.
- Reusable capability belongs in provider modules; forest-specific content remains bounded dogfood.
- Opportunity derivation and evaluation are read-only until atomic assignment.
- Pathfinding reads spatial state and never owns entities, reservations, work choice, or completion.
- The detailed executor owns transient progress only.
- Determinism cannot depend on wall clock, task order, unordered iteration, or unstable queue ties.
- Capture performance baselines but do not add multithreading, archetypes, hierarchical navigation, or speculative caches.
- Persist semantic intent; rebuild routes/presentation after load.
- Add public contracts only where reusable.
- Do not duplicate renderer, animation, audio, or input systems.

## Required authority documents

The implementation agent must read only:

1. `AGENTS.md`;
2. `README.md`;
3. `docs/ENGINEERING.md`;
4. `docs/engineering/command-contract.md`;
5. `docs/engineering/validation-tiers.md`;
6. `docs/engineering/human-review-workflow.md`;
7. `docs/TERMINOLOGY.md`;
8. `docs/SPECS.md`;
9. `docs/specs/runtime-principles.md`;
10. `docs/specs/simulation-world-and-semantic-foundation-contract.md`;
11. `docs/architecture/simulation-foundation-architecture.md`;
12. `docs/decisions/ADR-0042-simulation-foundation-is-an-optional-first-class-engine-capability.md`;
13. `docs/specs/entity-component-runtime-contract.md`;
14. `docs/specs/deterministic-behavior-runtime-contract.md`;
15. `docs/specs/pluggable-spatial-runtime-contract.md`;
16. `docs/specs/grid-spatial-module-contract.md`;
17. `docs/specs/spatial-query-and-trigger-contract.md`;
18. `docs/specs/interaction-runtime-contract.md`;
19. `docs/specs/item-inventory-and-collection-contract.md`;
20. `docs/specs/gameplay-presentation-event-contract.md`;
21. `docs/specs/render-projection-contract.md`;
22. `docs/specs/input-action-map-contract.md`;
23. `docs/specs/tick-bound-input-frame-contract.md`;
24. `docs/specs/animation-selection-and-sampling-contract.md`;
25. `docs/specs/raylib-debug-client-contract.md`;
26. `docs/specs/autonomous-work-and-detailed-logistics-contract.md`;
27. `docs/specs/detailed-grid-navigation-and-activity-execution-contract.md`;
28. `docs/architecture/autonomous-detailed-region-execution-architecture.md`;
29. `docs/decisions/ADR-0043-work-opportunities-are-derived-and-detailed-execution-is-an-optional-simulation-strategy.md`;
30. `docs/scenarios/m032-detailed-forest-logistics-vertical-slice.md`;
31. `docs/artifacts/autonomous-detailed-region-artifact-contract.md`;
32. this milestone document.

Do not read the external guide repository, `.guide-profile.json`, `.guide-sync/`, copied guides, prompt templates, or `docs/research/`. Read `.review/` only for M032 review.

## Files or areas likely affected

```text
src/Agentic2D.Contracts/
src/Agentic2D.Engine/
src/Agentic2D.Simulation/
src/Agentic2D.Entities/
src/Agentic2D.Behaviors/
src/Agentic2D.Spatial.Grid/
src/Agentic2D.Rendering/
src/Agentic2D.Input/
src/Agentic2D.ScenarioRunner/
src/Agentic2D.Validation/
src/Agentic2D.Tools/
src/Agentic2D.DebugClient.Raylib/
src/Agentic2D.Engineering/
tests/unit/Agentic2D.Tests.Unit/
existing authored fixture locations
eng/
docs indexes and affected active authority
.review/
artifacts/
```

## Validation tiers and concrete commands

### Tier 0 — Repository and authority

```bash
./eng/format.sh --verify
./eng/docs-check.sh
./eng/check.sh
```

### Tier 1 — Focused tests

```bash
./eng/test-filter.sh AutonomousWork
./eng/test-filter.sh WorkOpportunity
./eng/test-filter.sh WorkerSelection
./eng/test-filter.sh DetailedGridNavigation
./eng/test-filter.sh DetailedActivityExecution
./eng/test-filter.sh Logistics
./eng/test-filter.sh BasicNeeds
./eng/test-filter.sh DetailedRegionPersistence
./eng/test-filter.sh DetailedRegionProjection
```

### Tier 2 — Focused smoke

```bash
./eng/designation-work-smoke.sh
./eng/worker-selection-smoke.sh
./eng/detailed-grid-navigation-smoke.sh
./eng/detailed-activity-execution-smoke.sh
./eng/logistics-conservation-smoke.sh
./eng/basic-needs-interruption-smoke.sh
./eng/detailed-region-persistence-smoke.sh
./eng/detailed-region-projection-smoke.sh
./eng/m032-forest-logistics-smoke.sh
./eng/m032-detailed-region-graphics-smoke.sh
```

Graphics must report passed, failed, or explicit `skipped-not-graphics-capable`; it must not silently claim execution.

### Tier 3 — Regression

```bash
./eng/m031-smoke.sh --verify
./eng/entity-runtime-smoke.sh
./eng/behavior-smoke.sh
./eng/grid-spatial-smoke.sh
./eng/continuous-spatial-smoke.sh
./eng/spatial-query-trigger-smoke.sh
./eng/interaction-smoke.sh
./eng/gameplay-collection-atomicity-smoke.sh
./eng/gameplay-integrated-smoke.sh
./eng/render-projection-smoke.sh
./eng/input-replay-smoke.sh
./eng/animation-replay-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/persistence-diagnostics-smoke.sh
./eng/m028-smoke.sh --verify
./eng/m029-smoke.sh --verify
```

Use current command support. If an earlier suite has no `--verify`, run its authoritative bounded form. Never fabricate unavailable validation.

### Tier 4 — Resumable suite

```bash
./eng/m032-smoke.sh
./eng/m032-smoke.sh --plan-json
./eng/m032-smoke.sh --shard <id>
./eng/m032-smoke.sh --verify
```

Required shards:

```text
documentation
designations-opportunities
worker-selection
navigation-occupancy
detailed-executor
logistics-conservation
basic-needs
persistence-reconstruction
structural-projection
graphical-proof
m031-regression
engine-regression
asset-train-regression
human-review
integrated
```

Only `--verify` establishes aggregate success.

### Tier 5 — Human review

```bash
./eng/review-list.sh --milestone M032
./eng/review-show.sh review.m032.autonomous-detailed-region-work-and-logistics
./eng/review-check.sh --milestone M032
```

## Validation execution mode

```text
Tier 0: direct
Tier 1: direct
Tier 2 structural: direct
Tier 2 graphical: graphics-capable or explicit skip
Tier 3: direct or resumable shard
Tier 4: resumable-sharded
Tier 5: human-review
```

A graphics-capable execution is required before approval. A skip cannot satisfy blocking review.

## Resumable validation contract

Receipts:

```text
artifacts/validation/m032-smoke/plan.json
artifacts/validation/m032-smoke/receipts/<shard-id>.json
artifacts/validation/m032-smoke/verify.json
```

Fingerprint scope includes M032 source/tests, relevant M031/runtime dependencies, engineering wrappers/host, authority documents, updated indexes, authored fixtures, project files, evidence manifests, and review state.

Each receipt records suite/shard/schema/fingerprint/command/timestamps/result/environment/evidence. Verification fails on missing/stale/failed receipts, absent graphical proof, unapproved review, incomplete conservation/invariants, contradictions, or partial-child inference.

## Acceptance criteria

1. One active region executes autonomous detailed work without direct assignment.
2. Extraction/storage designations are authoritative, deterministic, inspectable, and persistent.
3. Opportunities derive deterministically without duplicating activities/resources/reservations.
4. Generic workers select with explicit deterministic tie-breaking.
5. Explanations include selected/rejected candidates, factors, reservations, and path estimate.
6. Two workers cannot reserve one exclusive target.
7. Navigation respects map semantics, occupancy, interaction positions, and stable ties.
8. Invalidation causes bounded replan/interruption/failure, never silent deadlock.
9. Movement/progress use semantic fixed-step time.
10. Detailed execution completes authority only through commands.
11. Harvest/pickup/carry/deposit conserve integer quantities.
12. Storage compatibility/capacity and reservations are enforced.
13. Fixed needs accumulate deterministically and interrupt at mandatory thresholds.
14. Interruption leaks no reservation or resource.
15. Save/load while carrying reconstructs routes and continues without duplication.
16. Designation changes and temporary blockage recover coherently.
17. The scenario reaches its stored-wood target in replay.
18. Structural artifacts pass contract validation.
19. Graphical evidence is readable at normal play scale.
20. M031 and earlier regressions pass.
21. Current receipts verify and blocking review is approved.
22. No abstract executor, fidelity switching, infrastructure network, archetype rewrite, dynamic plugin, or M030 integration is introduced.
23. Direct docs reflect project truth.
24. No external-guide dependency is introduced.

## Direct documentation impact

Update `docs/TERMINOLOGY.md`, `docs/SPECS.md`, `docs/ENGINEERING.md`, `docs/ARTIFACTS.md`, `docs/SCENARIOS.md`, `README.md` where material, and affected active M031/spatial/inventory/interaction/input/render/persistence authority.

## Deferred documentation synchronization hints

Included: `.guide-sync/pending/2026-07-21-m032-autonomous-detailed-region-sync.md`. Implementation agents must not read or resolve it.

## Human-review requirements

```text
applicability: required
completion effect: blocking
classes: UX, visual, semantic, artifact-quality
review ID: review.m032.autonomous-detailed-region-work-and-logistics
owning milestone: M032
reviewer: repository user
acceptable decision: approved
```

Review the indirect-control designation flow, autonomous selection/explanation, movement/interaction readability, logistics continuity, need interruption, blockage recovery, and save/load continuation in the actual supported graphical client, preferably through normal Android-to-Ubuntu RDP where practical.

No implicit waiver. Later milestones own their own reviews and do not reopen this completed record solely because later commits change the repository.

## Constrained-runtime handling

1. run `--plan-json`;
2. execute each structural shard in a foreground invocation;
3. run graphical proof in a graphics-capable environment;
4. preserve current receipts;
5. inspect review pack and complete review;
6. rerun human-review shard after approval;
7. run `--verify`;
8. report completion only from verification.

Do not background work, inflate timeouts, or treat screenshots/logs without receipts as aggregate proof.

## Out-of-scope guide migration work

No guide migration is part of M032. Repository documentation contains project truth and must not cite external guide documents as implementation authority.
