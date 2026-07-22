# Milestone 034 — Environmental Infrastructure and Operations Surface

## Goal

Turn the M031–M033 simulation platform into a coherent early-access game loop centered on work coordination, logistics, and environmental infrastructure:

```text
player plans infrastructure and designates production
→ construction and delivery work is derived
→ water, food, storage, shelter, and maintenance systems operate
→ workers autonomously satisfy fixed needs
→ shortages, wear, and bottlenecks create pressure
→ player observes, prioritizes, expands, and repairs
→ all regions continue through detailed or abstract execution
```

Primary acceptance question:

> Can a player establish and sustain a small multi-region settlement by planning water, food, shelter, storage, and maintenance infrastructure, while understanding shortages and work bottlenecks through a usable operations surface rather than direct worker control?

M034 creates the first coherent early-access game loop and the first multi-session internal-play build. M035 remains responsible for heavy-scale optimization, long-duration stability, compatibility hardening, and formal heavy-internal-test readiness.

## Repository role and maturity assumptions

```text
repository role: capability-provider
bounded dogfood: one small multi-region settlement game
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation, UI/component
maturity: implementation-ready, artifact-first
execution mode: ai-executed-broad
```

M031, M032, and M033 are complete and authoritative. The repository provides a persistent simulation world, semantic commands/events and activities, autonomous work/logistics, detailed execution, standalone abstract execution, and transactional region-fidelity switching.

M034 is mixed provider/dogfood work:

- provider responsibility: reusable construction, infrastructure, production, maintenance, alert, projection, and operations capabilities;
- bounded dogfood responsibility: one three-region settlement scenario using those capabilities.

M030 remains deferred. M034 uses procedural or existing authorized placeholder presentation.

## Execution mode

`ai-executed-broad`

Implement as six coherent transformations:

1. planned construction, material delivery, completion, cancellation, and infrastructure lifecycle;
2. water collection, storage, hauling, consumption, shortage, and reserve planning;
3. farmland, crop growth, harvest, food storage/consumption, and shelter/comfort capacity;
4. infrastructure condition, wear, maintenance, failure, repair, and road/path improvement;
5. world/region operations surface with plans, policies, stock/flow/backlog, alerts, explanations, region switching, and save/resume;
6. sustained multi-region proof, artifacts, resumable validation, and blocking human review.

## Scope

### Settlement boundary

Support three persistent regions, exactly one detailed region, abstract execution elsewhere, generic workers, fixed food/water/comfort needs, and indirect control through plans, designations, priorities, reserve policies, and region switching.

### Construction planning and lifecycle

Required structure capabilities:

```text
water collector
water storage
food storage
general material storage
farm support
shelter/comfort
maintenance service point
road/path improvement
```

Construction plans include stable identity, structure definition, footprint, material demand/delivery, required work, progress, priority, revision, state, and blocking reason.

Lifecycle:

```text
planned
→ material demand
→ delivery
→ construction
→ completed structure
→ operational
→ degraded/failed
→ repaired
```

Placement validates bounds, terrain, occupancy, interaction positions, access, and conflicting plans. Cancellation conserves delivered materials through one explicit deterministic rule.

### Water infrastructure

Water is the primary environmental subsystem:

```text
water source
→ collector/production cycle
→ water stack/container
→ hauling
→ finite storage
→ worker consumption
```

Required states include enabled, disabled, damaged, full, empty, blocked, low reserve, and unavailable. Use integer or explicit fixed-point quantities. No pipe/fluid network is required.

### Food and farmland

Replace fixed replenishing food fixtures with:

```text
prepare
→ plant
→ supply water
→ grow by semantic time
→ mature
→ harvest
→ haul
→ food storage
→ consume
```

Growth and work must function in both fidelities. No seasons, soil chemistry, cooking, recipes, nutrition classes, disease, or spoilage.

### Shelter and comfort

Replace fixed comfort fixtures with infrastructure capacity. Structures expose finite capacity, access, comfort value, condition modifier, enabled state, and reservations. Comfort affects only the fixed need and work availability.

### Wear and maintenance

Infrastructure has deterministic condition, wear, maintenance warning, failure threshold, degraded capacity/throughput, repair demand, and explicit failure. At least water infrastructure and one shelter/storage structure must degrade and fail in the proof.

### Roads/path improvement

A road/path improvement changes both detailed movement cost and linked abstract travel-edge cost through one shared authored modifier.

### Policies

Settlement policies include minimum reserve, desired reserve, capacity target where relevant, work/infrastructure priority, and enabled state for wood/material, water, and food.

Policies influence work derivation but never assign individuals.

### Operations surface

World-level functions:

- list regions and fidelity;
- population/work availability;
- critical stocks/reserves;
- active alerts;
- activate region;
- save/load;
- pause/resume/step/bounded speed.

Region-level functions:

- plan/cancel structures;
- edit designations;
- set priorities/reserves;
- enable/disable infrastructure;
- inspect structures, farms, storage, workers, activities, reservations, and routes;
- show stocks, capacities, recent flows, backlog, blocked work, needs, maintenance, alerts, and event journal;
- explain idle, blocked, selected, interrupted, degraded, and failed states.

Required alerts:

```text
water reserve low
water unavailable
food reserve low
food unavailable
comfort capacity insufficient
storage full
construction blocked
maintenance due
infrastructure failed
no eligible worker
no reachable source/destination
region transition failed
```

Alerts have stable keys, severity, subject references, first/last instant, status, explanation, and suggested action category.

The UI may remain an enhanced raylib operations/debug client, but normal play must not require raw JSON.

### Persistence

Persist plans, delivered materials, structures, condition, farms/crops, water/food/storage, policies, alerts where authoritative, activities/reservations, region fidelity, and abstract triggers.

Fresh-process load must continue construction, growth, production, consumption, maintenance, shortages, failures, alerts, and mixed fidelity.

### Bounded proof

Create:

```text
region.river
region.fields
region.home
```

Each region is locally sustainable; cross-region transport is not added.

Required sequence:

1. begin with insufficient infrastructure;
2. designate extraction/storage;
3. build water collection/storage;
4. establish farmland/food storage;
5. construct comfort capacity;
6. set reserves/priorities;
7. recover from water shortage;
8. recover from storage bottleneck;
9. recover from maintenance failure;
10. switch regions during construction, growth, collection, carrying, and repair;
11. save/load during shortage or failure;
12. sustain declared reserves for fourteen simulated days after stabilization.

## Non-goals

Do not implement cross-region hauling/trade, vehicles, pipe/electrical/heat/waste networks, weather/seasons/temperature, contamination, cooking/spoilage, skills/personality/health/mood/social/combat, research/economy, procedural world generation, multiple detailed regions, commercial UI polish, modding, multiplayer, multithreading, ECS storage rewrite, M030 integration, Linux export closure, final performance optimization, or heavy-internal-test readiness declaration.

## Focus Area 1 — Construction and lifecycle

Authority:

- `docs/specs/construction-and-infrastructure-lifecycle-contract.md`;
- architecture and ADR in this package.

Required: placement, plans, delivery, progress, completion, cancellation, repair, persistence, and both execution strategies.

Blocking cases: overlap, inaccessible footprint, partial-delivery cancellation, reservation conflict, duplicate completion, fidelity transition, and save/load at each stage.

## Focus Area 2 — Water and reserves

Authority: `docs/specs/environmental-resource-and-flow-contract.md`.

Required: production, storage, hauling, consumption, reserves, shortage, capacity, damaged state, flow evidence, both fidelities, and zero-tolerance conservation.

## Focus Area 3 — Food, farming, and comfort

Authority: `docs/specs/settlement-production-and-needs-infrastructure-contract.md`.

Required: full crop loop, food reserves, shelter/comfort capacity, conflicts, growth across fidelity transitions, and persistence.

## Focus Area 4 — Wear, maintenance, failure, and roads

Required: wear, inspection/repair work, degraded throughput, failure, repair, alerts, and shared detailed/abstract travel modifier.

## Focus Area 5 — Operations and diagnostics

Authority: `docs/specs/settlement-operations-surface-contract.md`.

Required: dashboards, tools, priorities, reserves, stocks/flows/backlog, alerts, journal, explanations, region switching, and save/resume.

## Focus Area 6 — Sustained proof and review

Authority:

- `docs/scenarios/m034-settlement-infrastructure-and-operations.md`;
- `docs/artifacts/settlement-infrastructure-and-operations-artifact-contract.md`;
- M034 review request.

Required: shortages/recovery, maintenance failure/repair, fourteen-day stabilized run, graphical evidence, resumable suite, and blocking review.

## Implementation constraints

- Use M031–M033 world, time, commands/events, activities/reservations, work selection, both executors, fidelity, persistence, and equivalence.
- Do not create parallel job, queue, inventory, region, or time models.
- Infrastructure and policy are authoritative domain state.
- UI reads projections and issues commands; it does not mutate stores directly.
- Use integer or scaled fixed-point quantities.
- Every proof transition must work in detailed and abstract modes.
- Prefer a few parameterized reusable structures over a large catalog.
- Measure infrastructure, flow, alert, projection, save/load, and fourteen-day throughput; M035 owns broad optimization.
- Public contracts remain reusable; dogfood structure definitions stay authored content.

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
10. M031 simulation-foundation spec, architecture, and ADR;
11. M032 autonomous-work, detailed-navigation, architecture, and ADR;
12. M033 discrete-event, abstract-execution, fidelity/reconciliation, equivalence, architecture, and ADR;
13. relevant existing inventory, interaction, map, input, render, gameplay-event, persistence, and inspection contracts;
14. `docs/specs/construction-and-infrastructure-lifecycle-contract.md`;
15. `docs/specs/environmental-resource-and-flow-contract.md`;
16. `docs/specs/settlement-production-and-needs-infrastructure-contract.md`;
17. `docs/specs/settlement-operations-surface-contract.md`;
18. `docs/architecture/environmental-infrastructure-and-operations-architecture.md`;
19. `docs/decisions/ADR-0045-environmental-infrastructure-is-domain-state-and-operations-ui-is-a-read-only-command-surface.md`;
20. `docs/scenarios/m034-settlement-infrastructure-and-operations.md`;
21. `docs/artifacts/settlement-infrastructure-and-operations-artifact-contract.md`;
22. this milestone document.

Resolve exact M031–M033 paths through `docs/SPECS.md`; do not read unrelated milestone bodies.

Do not read the external guide repository, `.guide-profile.json`, `.guide-sync/`, copied guides, prompt templates, or `docs/research/`. Read `.review/` only for M034 review.

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
authored scenario/content fixtures
eng/
active docs indexes
.review/
artifacts/
```

## Validation tiers and commands

### Tier 0

```bash
./eng/format.sh --verify
./eng/docs-check.sh
./eng/check.sh
```

### Tier 1

```bash
./eng/test-filter.sh ConstructionPlan
./eng/test-filter.sh InfrastructureLifecycle
./eng/test-filter.sh EnvironmentalResource
./eng/test-filter.sh WaterFlow
./eng/test-filter.sh CropProduction
./eng/test-filter.sh ComfortInfrastructure
./eng/test-filter.sh Maintenance
./eng/test-filter.sh SettlementAlert
./eng/test-filter.sh OperationsProjection
./eng/test-filter.sh InfrastructurePersistence
```

### Tier 2

```bash
./eng/construction-lifecycle-smoke.sh
./eng/water-infrastructure-smoke.sh
./eng/farm-production-smoke.sh
./eng/comfort-capacity-smoke.sh
./eng/maintenance-failure-smoke.sh
./eng/road-travel-modifier-smoke.sh
./eng/settlement-alert-smoke.sh
./eng/operations-surface-smoke.sh
./eng/infrastructure-persistence-smoke.sh
./eng/m034-settlement-smoke.sh
./eng/m034-settlement-graphics-smoke.sh
```

Graphics command must report passed, failed, or explicit skip. A skip cannot satisfy review.

### Tier 3

```bash
./eng/m031-smoke.sh --verify
./eng/m032-smoke.sh --verify
./eng/m033-smoke.sh --verify
./eng/gameplay-integrated-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/persistence-diagnostics-smoke.sh
./eng/render-projection-smoke.sh
./eng/input-replay-smoke.sh
./eng/m028-smoke.sh --verify
./eng/m029-smoke.sh --verify
```

Use current authoritative invocation where older suites differ.

### Tier 4

```bash
./eng/m034-smoke.sh
./eng/m034-smoke.sh --plan-json
./eng/m034-smoke.sh --shard <id>
./eng/m034-smoke.sh --verify
```

Required shards:

```text
documentation
construction-plans
construction-execution
water-flow
food-farming
comfort-capacity
wear-maintenance
roads-travel-modifiers
policies-alerts
operations-projection
operations-input
mixed-fidelity-infrastructure
persistence-resume
shortage-recovery
sustained-fourteen-day
graphical-play-proof
m031-m033-regression
engine-regression
asset-train-regression
human-review
integrated
```

### Tier 5

```bash
./eng/review-list.sh --milestone M034
./eng/review-show.sh review.m034.environmental-infrastructure-and-operations
./eng/review-check.sh --milestone M034
```

## Validation execution mode

Tier 0–2 structural are direct; sustained and aggregate work are resumable-sharded; graphical proof is graphics-capable; completion includes human review.

Receipts:

```text
artifacts/validation/m034-smoke/plan.json
artifacts/validation/m034-smoke/receipts/<shard-id>.json
artifacts/validation/m034-smoke/verify.json
```

Fingerprint scope includes M034 source/tests/content/authority, relevant M031–M033 dependencies, engineering commands, projects, indexes, evidence manifests, and review state.

Only `--verify` establishes aggregate success. It fails for missing/stale receipts, incomplete sustained run, incomplete conservation evidence, failed recovery, cross-fidelity semantic mismatch, absent graphics, pending review, or contradictory integrated evidence.

## Acceptance criteria

1. Plans validate and persist.
2. Delivery/construction use autonomous work and both executors.
3. Cancellation/completion conserve materials.
4. Completed plans become operational structures.
5. Water production/storage/hauling/consumption conserve quantity.
6. Reserve policies create understandable work pressure and alerts.
7. Farmland supports prepare, plant, water, grow, harvest, haul, and consume.
8. Crop growth survives fidelity transitions.
9. Comfort infrastructure replaces fixed proof fixtures.
10. Condition wears deterministically.
11. Maintenance prevents or repairs failure.
12. Failure changes declared capacity/throughput explicitly.
13. Roads affect detailed and abstract travel through linked authority.
14. World/region operations show stocks, capacities, flows, backlog, needs, maintenance, alerts, and fidelity.
15. Player can plan/cancel, set policies, switch regions, and save/load through supported input.
16. Alerts are deterministic and causal.
17. Normal play needs no raw JSON.
18. Save/load resumes all active infrastructure processes and mixed fidelity.
19. Water shortage, storage bottleneck, and maintenance failure are recovered.
20. Three regions sustain declared reserves for fourteen days after stabilization.
21. Conservation and reservation integrity have zero unexplained divergence.
22. Structural and graphical evidence satisfy contracts.
23. M031–M033 and earlier regressions pass.
24. M034 receipts verify.
25. Blocking review is approved.
26. No cross-region logistics, complex networks, character simulation, M030 integration, or M035 hardening scope is introduced.
27. Direct documentation reflects project truth.
28. No external guide access is required.

## Direct documentation impact

Update terminology, specs, engineering commands, artifacts, scenarios, README capability surface, and affected M031–M033/domain documents where implemented truth changes.

## Deferred documentation synchronization hints

`.guide-sync/pending/2026-07-22-m034-infrastructure-operations-sync.md` is included and must not be read by the implementation agent.

## Human-review requirements

```text
applicability: required
completion effect: blocking
review classes: gameplay, UX, visual, semantic, artifact-quality
review ID: review.m034.environmental-infrastructure-and-operations
reviewer: repository user
acceptable decision: approved
```

Review subject: coherence and usability of planning/constructing infrastructure, managing reserves, diagnosing bottlenecks, switching regions, and recovering from shortage/failure.

No implicit waiver. M035 owns its own readiness review.

## Constrained-runtime handling

1. Run `--plan-json`.
2. Execute each structural shard separately.
3. Run shortage/recovery and fourteen-day shards separately.
4. Run graphics in a capable environment.
5. Generate review pack.
6. Complete review.
7. Rerun human-review shard.
8. Run `--verify`.
9. Report completion only from verifier.

## Out-of-scope guide migration work

No guide migration is part of M034. Repository documentation contains project truth only.
