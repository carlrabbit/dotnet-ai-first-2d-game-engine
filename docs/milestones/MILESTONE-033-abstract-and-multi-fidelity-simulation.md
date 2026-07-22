# Milestone 033 — Abstract and Multi-Fidelity Simulation

## Goal

Add the game’s defining engine capability: one persistent simulation world in which inactive regions run through a high-speed discrete-event model while one selected region runs through the existing M032 detailed executor.

```text
shared M031 world and semantic rules
├── M032 detailed fixed-step executor
└── M033 abstract discrete-event executor
        ↓
region fidelity orchestration
        ↓
abstract ↔ detailed reconciliation
        ↓
bounded equivalence, conservation, persistence, and switching proof
```

Primary acceptance question:

> Can several persistent regions continue autonomous work, logistics, and fixed needs through a deterministic standalone discrete-event engine, while repeatedly switching one region into and out of detailed execution without duplicating, losing, or silently changing authoritative outcomes?

M033 completes the technical multi-fidelity thesis. It does not add the broader environmental-infrastructure game loop or operations surface planned for M034.

## Repository role and maturity assumptions

```text
repository role: capability-provider
bounded dogfood: multi-region version of the M032 forest-logistics simulation
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation
maturity: implementation-ready, artifact-first
execution mode: ai-executed-broad
```

M031 and M032 are complete and authoritative. The repository now provides one persistent partitioned simulation world, semantic time, commands, factual events, activities, reservations, persistence, inspection, derived work opportunities, deterministic worker selection, detailed navigation, logistics, fixed needs, and one reviewed detailed region.

M033 is mixed provider/dogfood work:

- provider responsibility: discrete-event scheduling, abstract execution, abstract travel, fidelity ownership, transition/reconciliation, standalone hosting, and equivalence validation;
- bounded dogfood responsibility: three small forest-logistics regions with one detailed region at a time.

M030 remains deferred. M033 must not depend on promoted assets or consumer asset integration.

## Execution mode

`ai-executed-broad`

Implement as six coherent transformations:

1. deterministic discrete-event scheduler, guarded triggers, cancellation/invalidation, and standalone host;
2. abstract spatial graph, duration models, abstract work/logistics/need execution, and long-horizon advancement;
3. explicit region fidelity state and execution ownership;
4. abstract-to-detailed materialization and detailed-to-abstract conversion;
5. mixed-fidelity persistence, repeated switching, and failure recovery;
6. equivalence/conservation evidence, resumable validation, performance baselines, and blocking human review.

## Scope

### Optional standalone discrete-event subsystem

Provide an optional first-class engine subsystem that runs inside the graphical host, ordinary headless scenario host, standalone accelerated host, tests, and benchmarks.

It depends on M031 simulation contracts and M032 shared work/logistics/needs semantics, but not rendering, raylib, input, or the detailed route executor.

Required capabilities:

- deterministic priority queue;
- stable equal-time ordering;
- guarded trigger scheduling;
- cancellation or versioned invalidation;
- `advance-to`, `advance-by`, and next-event execution;
- queue inspection;
- bounded safety limits;
- canonical persistence;
- standalone headless execution;
- structured receipts and diagnostics.

### Scheduled triggers

Use the M031 scheduled-trigger boundary. A trigger contains or resolves:

```text
ScheduledTriggerId
due simulation instant
stable ordering key
owner region
owner activity/entity
trigger kind
expected revisions
causation/correlation
typed payload
status
```

Delivery revalidates current ownership and state, issues a shared semantic command, records factual domain events after commit, and schedules only the next meaningful transition.

Scheduled triggers are not domain events and do not directly own arbitrary mutation.

### Queue invalidation and cancellation

Prefer guarded/versioned invalidation over arbitrary queue removal.

Required behavior:

- activity revision mismatch makes a trigger stale;
- fidelity change invalidates or transfers executor ownership;
- cancelled activity cannot complete later;
- destroyed target produces explicit stale/invalid outcome;
- duplicate delivery cannot duplicate completion;
- stale triggers remain inspectable;
- optional compaction remains deterministic.

### Abstract activity executor

Provide abstract execution for all M032 families:

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

Plan one stage at a time. Abstract execution uses the same commands, events, activities, reservations, resource quantities, storage rules, needs, opportunity derivation, and worker-selection rules as detailed execution.

### Abstract travel

Provide a coarse deterministic graph distinct from detailed pathfinding.

Required concepts:

- abstract location;
- stable area/portal nodes and edges;
- integer traversal cost;
- route summary;
- carrying/movement modifiers;
- infrastructure/access extension point;
- graph revision;
- disconnected diagnostics.

Proof nodes may include housing, forest, storage, food, water, rest, and region entry.

Ignore transient individual obstacles, exact grid routes, animation, and momentary collision avoidance. Honor durable connectivity and access restrictions.

### Duration models

Deterministic typed duration models cover abstract travel, harvest, pickup, deposit, eating, drinking, resting, and bounded retry delay.

Inputs are inspectable and based only on semantic state and fixed authored policy. No wall-clock time, hidden randomness, personalities, or skills.

### Lazy integration and threshold scheduling

Support deterministic lazy need integration and schedule only relevant warning, mandatory, and satisfaction thresholds. No per-frame abstract updates.

### Region fidelity state

Every participating region has authoritative persistent fidelity:

```text
detailed
abstract
```

Store fidelity, executor owner, transition status/revision, transition instants/diagnostics, and spatial/graph revisions.

Exactly one region is detailed. All others are abstract.

### Execution ownership

At every simulation instant:

- one executor owns each region;
- one executor owns each active activity stage;
- detailed systems update only the detailed region;
- abstract triggers deliver only for abstract regions;
- transitions are serialized and transactional;
- global commands/events remain deterministically ordered.

### Abstract-to-detailed transition

Required steps:

1. pause abstract delivery at a transition boundary;
2. collect authoritative entities, activities, reservations, needs, inventory, and queue ownership;
3. invalidate or transfer pending triggers;
4. materialize detailed spatial state;
5. map abstract travel progress to a deterministic plausible grid position;
6. project to a valid reachable cell;
7. preserve semantic destination, activity progress, inventory, reservations, revisions, and causality;
8. rebuild detailed routes;
9. validate invariants;
10. commit fidelity atomically;
11. emit transition evidence.

Exact hypothetical detailed route reproduction is not required. Position must be deterministic, valid, plausible, and economically neutral.

### Detailed-to-abstract transition

Required steps:

1. stop detailed updates at a fixed-step boundary;
2. capture authoritative semantic state;
3. discard/convert transient path and presentation state;
4. map exact position to abstract node/edge;
5. convert remaining travel/interaction progress into semantic duration;
6. schedule one next guarded trigger;
7. rebuild abstract derived state;
8. validate ownership and reservations;
9. commit fidelity atomically;
10. emit transition evidence.

Unsafe transient states replan from the nearest valid semantic boundary and produce diagnostics. Never silently complete or duplicate work.

### Transition transaction

A transition either completes fully or leaves the region in its previous valid fidelity. Mixed ownership is forbidden.

### Mixed world advancement

Provide one world-time orchestrator:

- detailed region advances through fixed steps;
- abstract regions advance through due triggers;
- authoritative command/event outcomes share global deterministic ordering;
- accelerated standalone mode may jump directly between events;
- graphical mode retains one detailed region.

No cross-region hauling in M033.

### Standalone host

Provide a product/repository-consistent entry point equivalent to:

```bash
agentic2d simulation run <scenario-or-save>   --until <instant-or-duration>   --mode abstract   --output <directory>
```

Support run, advance, inspect, queue inspection, save, load, and compare as appropriate to existing CLI conventions.

### Persistence

Persist fidelity, queue, sequence, trigger statuses needed for continuation, abstract locations, activity ownership, semantic progress, needs, resources, reservations, and transition diagnostics.

Do not persist pathfinder frontier, graphical state, native handles, or half-committed transition state.

### Equivalence policy

Classify behavior:

```text
rule-equivalent
bounded-approximate
presentation-only
abstract-optimization
```

Zero tolerance applies to conservation, identity, lifecycle, reservation integrity, single completion, command semantics, and executor ownership.

Declared bounded tolerances apply to travel duration, local congestion, exact arrival ordering, and interruption timing.

### Observer neutrality

Compare all-abstract, periodically switched, mostly detailed, and bounded continuously detailed control runs. Report systematic load/unload productivity or safety effects rather than concealing them.

### Bounded proof world

Provide three regions, each containing two generic workers, bounded resources, storage, fixed need sources, designations, an abstract graph, and a detailed grid.

Run at least thirty simulated days, repeatedly switching during travel, harvesting, carrying, need activity, blocked, and idle states. Include mixed-fidelity save/load and control comparisons.

## Non-goals

Do not implement environmental infrastructure networks, construction execution, full operations UI, cross-region transport, multiple detailed regions, seamless rendered streaming, physical inter-region travel, personalities, skills, health, combat, weather, seasons, ecology, procedural world generation, distributed simulation, networking, multithreaded event delivery, archetype ECS rewrite, dynamic plugins, exact cross-mode trace identity, M030 integration, release export, broad guide migration, TBPs, issue templates, or workflow YAML.

## Focus Area 1 — Scheduler and standalone host

Authority:

- `docs/specs/discrete-event-simulation-contract.md`;
- `docs/architecture/multi-fidelity-simulation-architecture.md`;
- ADR-0044.

Required outcomes: deterministic queue, guarded triggers, statuses, advancement, safety limits, inspection, persistence, and a no-graphics standalone host.

Blocking cases: equal-time order, stale revision, cancellation, destroyed target, duplicate delivery, restore, interrupted save, safety stop, and graphics isolation.

## Focus Area 2 — Abstract work, logistics, needs, and travel

Authority:

- `docs/specs/abstract-activity-and-travel-contract.md`;
- M031/M032 contracts;
- M033 scenario.

Required outcomes: coarse travel graph, duration models, abstract execution for every M032 activity family, lazy needs, threshold triggers, shared selection/commands, and deterministic long-horizon execution.

## Focus Area 3 — Fidelity ownership and transitions

Authority:

- `docs/specs/region-fidelity-and-reconciliation-contract.md`;
- architecture and ADR-0044.

Required outcomes: authoritative fidelity, one owner, serialized transition state, trigger transfer/invalidation, materialization, abstraction, route/trigger rebuild, and rollback.

## Focus Area 4 — Persistence and recovery

Required outcomes: queue and mixed-fidelity persistence, fresh-process continuation, no half-transition state, deterministic reconstruction, and atomic failure preservation.

## Focus Area 5 — Equivalence, conservation, and observer neutrality

Authority:

- `docs/specs/multi-fidelity-equivalence-contract.md`;
- scenario and artifact contracts.

Required outcomes: authored tolerances, independent conservation, control runs, divergence report, switch-count analysis, and deterministic reruns.

## Focus Area 6 — Evidence, performance, and review

Authority:

- `docs/artifacts/multi-fidelity-simulation-artifact-contract.md`;
- M033 review request.

Required outcomes: queue/transition traces, position mappings, comparison reports, graphical switch evidence, acceleration baseline, bounded review pack, and resumable validation.

## Implementation constraints

- M031/M032 shared semantics remain authoritative; do not fork rules for abstract mode.
- Discrete-event simulation is optional and standalone-capable.
- Multi-fidelity reconciliation is a separate optional integration capability.
- Use explicit compile-time composition; no dynamic plugin loader.
- Trigger handlers normally issue commands.
- Schedule one meaningful transition at a time.
- Transitions are serialized transactions.
- Abstract travel does not run detailed pathfinding per trip.
- Position reconciliation is deterministic and explicit, not exact-route-equivalent.
- One region is detailed.
- Measure queue/event throughput, stale triggers, transitions, save/load, divergence, and acceleration.
- Do not introduce multithreading, distributed execution, storage rewrite, or generalized flow solver without blocking evidence.
- Reuse M032 rendering for graphical switch evidence.
- Keep public contracts reusable and proof content game-local.

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
13. `docs/specs/autonomous-work-and-detailed-logistics-contract.md`;
14. `docs/specs/detailed-grid-navigation-and-activity-execution-contract.md`;
15. `docs/architecture/autonomous-detailed-region-execution-architecture.md`;
16. `docs/decisions/ADR-0043-work-opportunities-are-derived-and-detailed-execution-is-an-optional-simulation-strategy.md`;
17. `docs/specs/discrete-event-simulation-contract.md`;
18. `docs/specs/abstract-activity-and-travel-contract.md`;
19. `docs/specs/region-fidelity-and-reconciliation-contract.md`;
20. `docs/specs/multi-fidelity-equivalence-contract.md`;
21. `docs/architecture/multi-fidelity-simulation-architecture.md`;
22. `docs/decisions/ADR-0044-discrete-event-simulation-is-standalone-capable-and-region-fidelity-is-authoritative.md`;
23. `docs/scenarios/m033-multi-region-equivalence-and-switching.md`;
24. `docs/artifacts/multi-fidelity-simulation-artifact-contract.md`;
25. current product CLI and persistence documents directly referenced by the new specs;
26. this milestone document.

Do not read the external guide repository, `.guide-profile.json`, `.guide-sync/`, copied guides, prompt templates, or `docs/research/` for implementation. Read `.review/` only for M033 review.

## Files or areas likely affected

```text
src/Agentic2D.Contracts/
src/Agentic2D.Engine/
src/Agentic2D.Simulation/
src/Agentic2D.Entities/
src/Agentic2D.Behaviors/
src/Agentic2D.Spatial.Grid/
src/Agentic2D.ScenarioRunner/
src/Agentic2D.Validation/
src/Agentic2D.Tools/
src/Agentic2D.Rendering/
src/Agentic2D.DebugClient.Raylib/
src/Agentic2D.Engineering/
tests/unit/Agentic2D.Tests.Unit/
authored scenario/content fixtures
eng/
docs indexes
README.md
.review/
artifacts/
```

## Validation tiers and concrete commands

### Tier 0

```bash
./eng/format.sh --verify
./eng/docs-check.sh
./eng/check.sh
```

### Tier 1

```bash
./eng/test-filter.sh DiscreteEvent
./eng/test-filter.sh ScheduledTrigger
./eng/test-filter.sh AbstractActivity
./eng/test-filter.sh AbstractTravel
./eng/test-filter.sh RegionFidelity
./eng/test-filter.sh RegionReconciliation
./eng/test-filter.sh MultiFidelityPersistence
./eng/test-filter.sh MultiFidelityEquivalence
./eng/test-filter.sh StandaloneSimulationHost
```

### Tier 2

```bash
./eng/discrete-event-scheduler-smoke.sh
./eng/abstract-activity-smoke.sh
./eng/abstract-travel-smoke.sh
./eng/abstract-needs-smoke.sh
./eng/region-fidelity-smoke.sh
./eng/region-reconciliation-smoke.sh
./eng/multi-fidelity-persistence-smoke.sh
./eng/multi-fidelity-equivalence-smoke.sh
./eng/standalone-simulation-smoke.sh
./eng/m033-multi-region-smoke.sh
./eng/m033-region-switch-graphics-smoke.sh
```

Graphics smoke reports passed, failed, or explicit skipped-not-graphics-capable. A skip cannot satisfy blocking review.

### Tier 3 regression

```bash
./eng/m031-smoke.sh --verify
./eng/m032-smoke.sh --verify
./eng/entity-runtime-smoke.sh
./eng/behavior-smoke.sh
./eng/grid-spatial-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/persistence-diagnostics-smoke.sh
./eng/gameplay-integrated-smoke.sh
./eng/render-projection-smoke.sh
./eng/input-replay-smoke.sh
./eng/m028-smoke.sh --verify
./eng/m029-smoke.sh --verify
```

Use the current authoritative invocation where an older suite lacks `--verify`.

### Tier 4 resumable M033 suite

```bash
./eng/m033-smoke.sh
./eng/m033-smoke.sh --plan-json
./eng/m033-smoke.sh --shard <id>
./eng/m033-smoke.sh --verify
```

Required shards:

```text
documentation
scheduler-ordering
trigger-invalidation
standalone-host
abstract-work-logistics
abstract-needs
abstract-travel
fidelity-ownership
abstract-to-detailed
detailed-to-abstract
transition-rollback
mixed-fidelity-persistence
equivalence-conservation
observer-neutrality
long-horizon
graphical-switch-proof
m031-m032-regression
engine-regression
asset-train-regression
human-review
integrated
```

Only `--verify` establishes aggregate success.

### Tier 5 human review

```bash
./eng/review-list.sh --milestone M033
./eng/review-show.sh review.m033.abstract-and-multi-fidelity-simulation
./eng/review-check.sh --milestone M033
```

## Validation execution mode

```text
Tier 0: direct
Tier 1: direct and focused
Tier 2 structural: direct
Tier 2 long-horizon: resumable or bounded standalone
Tier 2 graphical: graphics-capable
Tier 3: direct where bounded or resumable shard
Tier 4: resumable-sharded
Tier 5: human-review
```

## Resumable validation contract

Receipts:

```text
artifacts/validation/m033-smoke/plan.json
artifacts/validation/m033-smoke/receipts/<shard-id>.json
artifacts/validation/m033-smoke/verify.json
```

Fingerprint scope includes M033 source, relevant M031/M032 dependencies, tests, fixtures, authority docs, engineering wrappers/host, projects, indexes, tolerance configuration, evidence manifests, and review state.

Each receipt records suite/shard/schema, fingerprint, command/environment, timestamps, result, relevant event/transition counts, evidence, and safety-limit status.

`--verify` fails for missing/stale receipts, failed semantics, missing graphical proof, pending review, incomplete long-horizon/conservation evidence, excess divergence, stale-trigger mutation, ambiguous ownership, or partial aggregate inference.

## Acceptance criteria

1. Discrete-event simulation runs without rendering/input dependencies.
2. Equal-time ordering is deterministic.
3. Stale/cancelled/duplicate triggers cannot duplicate outcomes.
4. Queue state persists and resumes in a fresh process.
5. Abstract execution supports every M032 work/logistics/need activity.
6. Abstract travel uses a coarse graph, not detailed per-trip pathfinding.
7. Needs use semantic time and threshold scheduling.
8. Detailed and abstract modes share selection and semantic commands.
9. Every region/activity has one executor owner.
10. Exactly one region is detailed.
11. Abstract-to-detailed transition preserves semantic authority and materializes valid positions.
12. Detailed-to-abstract transition preserves progress and schedules the correct next trigger.
13. Transition failure rolls back without mixed ownership.
14. Stale triggers cannot fire after activation.
15. Detailed systems never advance abstract regions.
16. Mixed-fidelity save/load works with pending triggers, carrying, needs, and completed transitions.
17. No half-transition state is persisted.
18. Three regions run at least thirty simulated days deterministically.
19. Repeated switching works during travel, work, carrying, need, blocked, and idle states.
20. Conservation, reservation, lifecycle, ownership, and single-completion invariants have zero divergence.
21. Approximate timing/outcomes remain within declared tolerances.
22. Observer-neutrality evidence reports bounded load/unload effects.
23. Standalone acceleration and transition baselines exist.
24. Structural artifacts satisfy the artifact contract.
25. Graphical evidence makes transitions understandable.
26. M031/M032 and earlier regressions pass.
27. Current M033 receipts verify.
28. Blocking review is approved.
29. No M034 infrastructure scope, multiple detailed regions, archetype rewrite, multithreading, dynamic plugins, or M030 integration is introduced.
30. Direct docs reflect implemented truth.
31. No external-guide dependency is introduced.

## Direct documentation impact

Update `docs/TERMINOLOGY.md`, `docs/SPECS.md`, `docs/ENGINEERING.md`, `docs/ARTIFACTS.md`, `docs/SCENARIOS.md`, `README.md`, and affected M031/M032 persistence/execution/inspection documents where current truth changes.

## Deferred documentation synchronization hints

Included:

```text
.guide-sync/pending/2026-07-22-m033-multi-fidelity-simulation-sync.md
```

Implementation agents must not read or resolve it.

## Human-review requirements

```text
applicability: required
completion effect: blocking
review classes: architecture, semantic, visual, artifact-quality
canonical review ID: review.m033.abstract-and-multi-fidelity-simulation
owning milestone: M033
owning milestone path: docs/milestones/MILESTONE-033-abstract-and-multi-fidelity-simulation.md
reviewer role: repository user or designated engine architect
acceptable completion decision: approved
```

Review subject: standalone abstract simulation and the plausibility, continuity, transparency, and measured neutrality of switching persistent regions between abstract and detailed execution.

No implicit waiver. Completed approval becomes historical evidence. M034 owns any later infrastructure/game-loop review.

## Constrained-runtime handling

1. run `./eng/m033-smoke.sh --plan-json`;
2. execute structural shards separately;
3. run long-horizon/control comparisons as resumable shards;
4. run graphical switch proof in a graphics-capable environment;
5. generate the review pack;
6. complete review;
7. rerun `human-review` after approval;
8. run `./eng/m033-smoke.sh --verify`;
9. report completion only from successful verification.

Do not background simulations, inflate timeouts, or treat partial event logs as aggregate success.

## Out-of-scope guide migration work

No guide migration is part of M033. External guides remain planning input only; repository documents contain project truth.
