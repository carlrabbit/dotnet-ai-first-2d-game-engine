# Milestone 031 — Simulation World and Semantic Foundation

## Goal

Establish the reusable authoritative simulation foundation required by the third game and later multi-fidelity execution:

```text
persistent partitioned entity/component world
+ deterministic simulation time
+ commands and factual domain events
+ explicit activities and reservations
+ structured persistence and inspection
→ one headless wood-workflow proof
```

M031 evolves the existing runtime rather than introducing a parallel game framework. It creates the project truth and implementation boundaries on which later milestones can add autonomous work selection, detailed path execution, discrete-event simulation, and region fidelity switching.

Primary acceptance question:

> Can the engine host, mutate, inspect, save, reload, and deterministically continue a multi-region simulation world through shared semantic commands, events, activities, and reservations without rendering or a future abstract-simulation executor?

## Repository role and maturity assumptions

```text
repository role: capability-provider
bounded dogfood: one headless third-game wood workflow
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation
maturity: implementation-ready, artifact-first
execution mode: ai-executed-broad
```

The repository already provides deterministic runtime commands/events, runtime entities and typed components, behavior modules, spatial modules, scenario execution, persistence diagnostics, structured artifacts, and resumable validation infrastructure.

M031 is a capability-provider milestone. The wood workflow is bounded dogfood used to prove provider capability; it is not a complete game implementation.

M030 remains deferred. M031 does not consume M029-promoted assets and does not reopen M030.

## Execution mode

`ai-executed-broad`

Implement as five coherent transformations:

1. normalize simulation vocabulary, ownership, identity, and lifecycle;
2. extend the ECS world with deterministic game-defined registration and region partitioning;
3. add deterministic simulation time, ordering, command, and domain-event semantics;
4. add explicit activities, stages, revisions, and reservations;
5. integrate persistence, inspection, artifacts, resumable validation, and one bounded headless workflow.

The implementation agent may organize code differently where repository conventions require it, but must preserve the authority and dependency rules in the required documents.

## Scope

### Authoritative simulation world

Extend the existing runtime entity/component model into one authoritative simulation world that supports:

- stable world, region, entity, activity, reservation, command, and event identities;
- deterministic game-defined component registration;
- entity creation, activation, deactivation, region transfer, and destruction;
- explicit region membership;
- deterministic region-filtered component queries;
- authoritative component mutation through existing validated runtime boundaries;
- component persistence classification;
- component-level change observations for internal invalidation and inspection;
- explicit deterministic system phases;
- headless operation without rendering, native graphics, or audio.

Use one world with explicit region partitions. M031 must not create one independent ECS world per region.

### Simulation time and deterministic ordering

Add project-level semantic types and policies for:

- `SimulationInstant`;
- `SimulationDuration`;
- one authoritative simulation clock;
- deterministic ordering keys;
- deterministic command order;
- deterministic event order;
- deterministic random-stream derivation where randomness is exercised;
- canonical state fingerprints independent of dictionary or registration iteration order.

M031 remains compatible with the existing fixed-tick runtime. It must not assume that all future simulation advancement occurs through fixed ticks.

### Commands, domain events, and scheduled-trigger boundary

Preserve and clarify:

```text
command
  requested validated state transition

domain event
  factual outcome emitted after authoritative mutation

scheduled trigger
  future execution input owned by a later optional subsystem
```

M031 implements the shared command and domain-event semantic contracts needed by both detailed and future abstract execution.

M031 defines only the scheduled-trigger extension boundary and durable identity requirements. It does not implement a discrete-event priority queue, abstract executor, event coalescing, lazy-flow integration, or region catch-up.

### Activities and reservations

Implement explicit mode-independent activity state:

- stable activity ID;
- actor;
- activity kind;
- current stage;
- target references;
- start and update instants;
- stage progress in semantic units;
- revision;
- status;
- interruption/cancellation reason;
- completion result;
- causal references.

Implement reservations with:

- stable reservation ID;
- owner activity;
- reserving entity;
- reserved entity/resource/capacity reference;
- quantity or capacity where applicable;
- acquisition instant;
- revision or version guard;
- release reason;
- deterministic conflict resolution;
- idempotent release;
- stale-reference diagnostics.

Activities and reservations may be stored as dedicated runtime domain stores or suitable authoritative components. Do not reduce multi-stage activities to uncontrolled marker-component churn.

### Region partitioning

Every region-owned simulation entity has one authoritative `RegionId`.

Required behavior:

- deterministic query by region;
- region transfer as one validated operation;
- no entity visible in two regions simultaneously;
- world-scoped entities allowed only through an explicit documented classification;
- region deletion or unloading does not silently destroy durable entities;
- future fidelity state may be represented, but no fidelity transition is implemented in M031.

### Persistence and restoration

Persist enough authoritative state to save, terminate, load into a fresh process, and continue:

- world and region identity;
- runtime entities;
- authoritative persistent components;
- activity state;
- reservations;
- simulation clock;
- deterministic sequencing/random state used by the proof;
- command/event continuation metadata required for equivalence.

Classify state as:

```text
authoritative-persistent
derived-rebuildable
active-mode-transient
presentation-only
external-handle
```

Never persist native handles, graphical state, exact future detailed paths, reflection metadata, repository-local absolute paths, or generated artifact paths as gameplay authority.

Load must be transactional. Invalid or incompatible data fails without partially mutating the destination world.

### Inspection and artifacts

Provide structured inspection for:

- world summary;
- regions;
- entities and component families;
- simulation clock;
- activities and stages;
- reservations;
- command outcomes;
- domain events;
- lifecycle transitions;
- persistence classifications;
- canonical state fingerprint;
- validation diagnostics.

Required schemas and artifact paths are authoritative in `docs/artifacts/simulation-foundation-artifact-contract.md`.

### Bounded wood-workflow proof

Provide one headless scenario that proves the shared semantics without implementing autonomous work selection or real pathfinding:

```text
create world and two regions
→ create one worker, one tree, and one storage entity
→ designate or issue a bounded harvest request
→ create activity and reserve tree
→ advance semantic stages through explicit commands
→ harvest three wood
→ reserve storage capacity
→ transfer/deposit wood
→ release reservations
→ save
→ load into a fresh process
→ continue one additional command
→ verify canonical equivalence and conservation
```

The scenario may use a deterministic test driver for stage advancement. It must not pretend to be the future detailed or abstract executor.

## Non-goals

Do not implement:

- autonomous work generation, worker scoring, or work selection;
- detailed pathfinding, route following, collision avoidance, or interaction positions;
- discrete-event priority queue or abstract activity executor;
- multi-fidelity region activation/deactivation or reconciliation;
- farming, water networks, environmental infrastructure, or player-facing needs;
- player operations UI;
- M030 asset integration, curated presentation, Linux game export, or game audiovisual review;
- ECS archetype or sparse-set rewrite without measured evidence;
- multithreaded system execution;
- automatic scheduler derivation from component read/write declarations;
- runtime-loaded dynamic plugins or assembly discovery;
- scheduled events represented as ECS entities by default;
- event sourcing as the authoritative persistence model;
- one ECS world per region;
- generic workflow YAML, TBPs, issue templates, or copied guide documents;
- broad guide migration or documentation synchronization.

## Focus Area 1 — ECS world, registration, lifecycle, and regions

Authority:

- `docs/specs/simulation-world-and-semantic-foundation-contract.md`;
- `docs/architecture/simulation-foundation-architecture.md`;
- `docs/decisions/ADR-0042-simulation-foundation-is-an-optional-first-class-engine-capability.md`;
- existing entity/component, behavior, spatial, and runtime contracts listed under required authority.

Required outcomes:

- deterministic explicit or generated registration for game-defined components;
- stable component type keys independent of CLR assembly-qualified-name persistence;
- deterministic entity lifecycle and region transfer;
- region-filtered queries;
- persistence classification metadata;
- internal change observations distinct from public domain events;
- structured diagnostics for duplicate IDs, unknown component keys, invalid lifecycle transitions, and cross-region inconsistencies.

Blocking cases include:

- registration order permutation;
- duplicate component key;
- unknown persisted component key;
- create/destroy/recreate identity handling;
- transfer between regions;
- failed transfer rollback;
- world-scoped versus region-scoped classification;
- deterministic query order;
- no renderer dependency.

## Focus Area 2 — Deterministic time, commands, and domain events

Authority:

- `docs/specs/simulation-world-and-semantic-foundation-contract.md`;
- `docs/specs/runtime-principles.md`;
- `docs/specs/minimal-deterministic-runtime.md`;
- `docs/specs/deterministic-behavior-runtime-contract.md`.

Required outcomes:

- semantic time types with checked arithmetic and canonical serialization;
- authoritative clock;
- deterministic phase and message order;
- commands request mutation;
- command handlers validate and atomically commit or fail;
- domain events record completed facts only after commit;
- command failure emits diagnostics and no factual success event;
- stable causal/correlation references;
- extension boundary for later scheduled triggers without queue implementation.

Blocking cases include:

- equal-time deterministic ordering;
- overflow/underflow;
- invalid command rollback;
- event emitted only after successful commit;
- replay of the same authored input yields the same semantic state fingerprint;
- no dependency on frame time or wall-clock time.

## Focus Area 3 — Activities, stages, revisions, and reservations

Authority:

- `docs/specs/simulation-world-and-semantic-foundation-contract.md`;
- `docs/scenarios/m031-headless-wood-workflow.md`.

Required outcomes:

- explicit activity state and lifecycle;
- stage transitions through validated commands;
- monotonic revision changes for invalidation;
- cancellation and interruption reasons;
- deterministic reservation acquisition;
- resource/entity/capacity reservation support sufficient for the proof;
- idempotent release;
- stale activity or reservation commands fail safely;
- no double ownership or negative reserved quantity.

Blocking cases include:

- two activities competing for one target;
- stale activity revision;
- duplicate completion command;
- target destroyed while reserved;
- reservation release after cancellation;
- capacity over-reservation;
- save/load during every activity stage.

## Focus Area 4 — Persistence, inspection, and artifact evidence

Authority:

- `docs/specs/simulation-world-and-semantic-foundation-contract.md`;
- `docs/artifacts/simulation-foundation-artifact-contract.md`;
- existing persistence and runtime-inspection authority listed below.

Required outcomes:

- versioned canonical save envelope;
- transactional load;
- deterministic serialization independent of in-memory iteration order;
- explicit persistence classification;
- canonical world fingerprint;
- world, region, activity, reservation, and event inspection;
- stable diagnostics;
- artifacts under `artifacts/simulation/M031/`;
- no absolute repository path or native handle in authoritative save state.

Blocking cases include:

- fresh-process round trip;
- order-invariance;
- malformed save;
- unknown schema version;
- unknown component key;
- interrupted write preserving previous valid save;
- inspection of valid and invalid states;
- authoritative versus derived-state classification.

## Focus Area 5 — Bounded dogfood and provider proof

Authority:

- `docs/scenarios/m031-headless-wood-workflow.md`;
- `docs/artifacts/simulation-foundation-artifact-contract.md`.

Required outcomes:

- one two-region headless fixture;
- one complete harvest-and-deposit activity;
- three units of wood conserved;
- all reservations released at successful completion;
- save/load at an intermediate stage;
- continuation after fresh-process load;
- same final canonical fingerprint for direct and save/load paths;
- structured evidence sufficient for automated and human review.

This proof must remain bounded. Do not implement the full third game in this milestone.

## Implementation constraints

### Dependency direction

```text
existing runtime/contracts
        ↑
simulation foundation contracts and implementation
        ↑
game-defined components and bounded wood proof
```

Game rules may depend on shared simulation contracts. Shared runtime rules must not depend on the future discrete-event executor, third-game UI, or rendering.

### Composition

Use explicit compile-time composition consistent with repository conventions.

Acceptable conceptual shape:

```text
AddSimulationFoundation()
AddGameDefinedComponents()
AddM031WoodWorkflowProof()
```

Do not introduce generalized runtime plugin discovery.

### Mutation authority

- authoritative state changes occur only through validated runtime mutation boundaries;
- behavior modules continue to read immutable snapshots and emit intents;
- spatial modules do not own entities;
- rendering remains read-only;
- component change observations do not replace domain events;
- event handlers must not mutate state outside approved command boundaries.

### Performance posture

Add bounded baseline evidence, but do not optimize speculatively.

Capture at least:

- entities;
- components;
- regions;
- query operations;
- activity/reservation counts;
- command/event counts;
- save/load size and elapsed time;
- allocations where existing performance infrastructure supports them.

Timing remains advisory and same-machine only. Semantic correctness is authoritative.

### Public API posture

M031 may add reusable public contracts required by the milestone. Names and shapes must follow repository conventions and the authority documents in this package. Do not expose implementation stores, serializer internals, or future multi-fidelity assumptions as public API.

### Compatibility

Preserve existing scenarios and capability contracts. Where existing persistence or runtime formats require extension, use explicit schema-version handling and diagnostics rather than silent reinterpretation.

## Required authority documents

The implementation agent must read only this list before implementation, plus files directly referenced by these authorities when necessary to understand an existing type:

1. `AGENTS.md`;
2. `README.md`;
3. `docs/ENGINEERING.md`;
4. `docs/engineering/command-contract.md`;
5. `docs/engineering/validation-tiers.md`;
6. `docs/TERMINOLOGY.md`;
7. `docs/SPECS.md`;
8. `docs/specs/runtime-principles.md`;
9. `docs/specs/minimal-deterministic-runtime.md`;
10. `docs/specs/entity-component-runtime-contract.md`;
11. `docs/specs/deterministic-behavior-runtime-contract.md`;
12. `docs/specs/pluggable-spatial-runtime-contract.md`;
13. `docs/specs/runtime-inspection-contract.md`;
14. `docs/engineering/persistence-architecture.md`;
15. `docs/engineering/human-review-workflow.md`;
16. `docs/specs/simulation-world-and-semantic-foundation-contract.md`;
17. `docs/architecture/simulation-foundation-architecture.md`;
18. `docs/decisions/ADR-0042-simulation-foundation-is-an-optional-first-class-engine-capability.md`;
19. `docs/scenarios/m031-headless-wood-workflow.md`;
20. `docs/artifacts/simulation-foundation-artifact-contract.md`;
21. this milestone document.

Do not read the external guide repository, `.guide-profile.json`, `.guide-sync/`, copied guides, prompt templates, or `docs/research/` for implementation.

`.review/` is read only for the M031 human-review workflow.

## Files or areas likely affected

Exact paths may vary with repository conventions. Likely areas:

```text
src/Agentic2D.Contracts/
src/Agentic2D.Engine/
src/Agentic2D.Entities/
src/Agentic2D.Behaviors/
src/Agentic2D.ScenarioRunner/
src/Agentic2D.Validation/
src/Agentic2D.Tools/
src/Agentic2D.Engineering/
tests/unit/Agentic2D.Tests.Unit/
game/content/ or existing authored fixture locations
eng/
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/ARTIFACTS.md
docs/SCENARIOS.md
relevant existing specs and engineering indexes
.review/
artifacts/
```

Do not add implementation source or generated evidence from this planning ZIP.

## Validation tiers and concrete commands

### Tier 0 — Authority and repository structure

```bash
./eng/format.sh --verify
./eng/docs-check.sh
./eng/check.sh
```

Implementation must update active project indexes and direct documentation affected by the implemented truth.

### Tier 1 — Focused unit and contract validation

```bash
./eng/test-filter.sh SimulationFoundation
./eng/test-filter.sh SimulationWorld
./eng/test-filter.sh SimulationTime
./eng/test-filter.sh SimulationCommand
./eng/test-filter.sh SimulationActivity
./eng/test-filter.sh SimulationReservation
./eng/test-filter.sh SimulationPersistence
```

Tests must include order permutation, stale revision, rollback, conflict, malformed persistence, and fresh-process continuation cases.

### Tier 2 — Focused capability smoke

Create and run repository-consistent wrappers:

```bash
./eng/simulation-world-smoke.sh
./eng/simulation-time-smoke.sh
./eng/simulation-command-event-smoke.sh
./eng/simulation-activity-reservation-smoke.sh
./eng/simulation-persistence-smoke.sh
./eng/simulation-inspection-smoke.sh
./eng/m031-wood-workflow-smoke.sh
```

Each command must validate meaningful state and emit structured evidence. Success-only placeholders are prohibited.

### Tier 3 — Regression

```bash
./eng/entity-runtime-smoke.sh
./eng/behavior-smoke.sh
./eng/grid-spatial-smoke.sh
./eng/continuous-spatial-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/scenario-smoke.sh
./eng/persistence-diagnostics-smoke.sh
./eng/m027-smoke.sh
./eng/m028-smoke.sh
./eng/m029-smoke.sh
```

Where M029 remains unimplemented in the target checkout, record it as `not-available` in the plan and do not fabricate success. M028 and all implemented earlier milestones remain blocking regressions.

### Tier 4 — Resumable milestone suite

```bash
./eng/m031-smoke.sh
./eng/m031-smoke.sh --plan-json
./eng/m031-smoke.sh --shard <id>
./eng/m031-smoke.sh --verify
```

Required shards:

```text
documentation
component-registration
entity-lifecycle
region-partition
simulation-time-ordering
commands-domain-events
activities-reservations
persistence-roundtrip
inspection-artifacts
wood-workflow
runtime-regression
asset-train-regression
human-review
integrated
```

Only `./eng/m031-smoke.sh --verify` establishes aggregate milestone success.

### Tier 5 — Human review gate

```bash
./eng/review-list.sh --milestone M031
./eng/review-show.sh review.m031.simulation-world-and-semantic-foundation
./eng/review-check.sh --milestone M031
```

The milestone cannot complete until the review record is approved and the review shard receives a current passing receipt.

## Validation execution mode

```text
Tier 0: direct
Tier 1: direct and focused
Tier 2: direct and focused
Tier 3: direct where bounded; delegated into resumable shards where long
Tier 4: resumable-sharded
Tier 5: human-review
```

## Resumable validation contract

Suite:

```bash
./eng/m031-smoke.sh
```

Plan:

```bash
./eng/m031-smoke.sh --plan-json
```

Shard:

```bash
./eng/m031-smoke.sh --shard <id>
```

Receipts:

```text
artifacts/validation/m031-smoke/plan.json
artifacts/validation/m031-smoke/receipts/<shard-id>.json
artifacts/validation/m031-smoke/verify.json
```

Fingerprint scope must include:

- implementation source involved in M031;
- unit and integration tests involved in M031;
- `eng/` wrappers and the tested engineering host;
- M031 authority documents;
- directly updated active indexes;
- authored proof fixtures;
- relevant project and solution files;
- review request/record state for the human-review shard.

Each receipt must record:

- suite and shard ID;
- schema version;
- current fingerprint;
- command;
- start/end timestamps;
- exit status;
- evidence paths;
- tool/runtime versions required by existing engineering conventions.

`--verify` must fail when:

- any required shard receipt is missing;
- any receipt fingerprint is stale;
- any shard failed or was skipped without an explicitly permitted reason;
- aggregate artifact validation fails;
- the required review is not approved;
- the integrated proof does not match focused evidence.

Partial child output, an individual shard log, or a successful subset is never aggregate success.

## Acceptance criteria

M031 is accepted only when all are true:

1. One authoritative runtime world contains at least two stable regions.
2. Game-defined components register deterministically with stable persisted keys.
3. Entity lifecycle and region transfer are validated and transactional.
4. Region-filtered queries are deterministic and return no duplicate or cross-owned entity.
5. Simulation time is semantic, deterministic, serializable, and independent of wall-clock/frame timing.
6. Commands request validated mutation; factual domain events are emitted only after successful commit.
7. Activities have explicit stages, progress, status, revision, and causal identity.
8. Reservations resolve conflicts deterministically, cannot over-reserve, and release idempotently.
9. Stale activity/reservation commands fail without mutation.
10. Persistent, derived, transient, presentation, and external-handle state are explicitly classified.
11. Save/load is versioned, canonical, transactional, and supports fresh-process continuation.
12. Direct and save/load wood-workflow paths produce the same final canonical fingerprint.
13. Three units of wood are conserved and all successful-path reservations are released.
14. Structured artifacts satisfy the artifact contract and contain no forbidden absolute/native authority.
15. Existing runtime, behavior, spatial, inspection, persistence, M028, and available M029 regressions pass.
16. The milestone suite verifies from current fingerprinted receipts.
17. The blocking M031 review is approved.
18. No archetype rewrite, abstract event engine, multi-fidelity transition, dynamic plugin system, or game UI is introduced.
19. Active documentation and indexes reflect implemented project truth.
20. No implementation agent dependency on the external guide repository, `.guide-profile.json`, or `.guide-sync/` is introduced.

## Direct documentation impact

The implementation must update project truth where implementation establishes it:

- `docs/TERMINOLOGY.md` with final simulation terms;
- `docs/SPECS.md` with the new permanent spec;
- `docs/ENGINEERING.md` with implemented commands and resumable-suite status;
- `docs/ARTIFACTS.md` with M031 artifact families;
- `docs/SCENARIOS.md` with the wood proof;
- `README.md` current capability and solution shape where materially changed;
- affected existing runtime/entity/persistence documents when implementation changes current truth.

Do not copy this milestone body into permanent specs. Permanent truth belongs in the linked spec, architecture, decision, scenario, and artifact documents.

## Deferred documentation synchronization hints

The package includes:

```text
.guide-sync/pending/2026-07-21-m031-simulation-foundation-sync.md
```

This is deferred planning/migration metadata. The implementation agent must not read it and does not need to resolve it for M031 completion.

## Human-review requirements

```text
applicability: required
completion effect: blocking
review class: architecture, semantic, artifact-quality
canonical review ID: review.m031.simulation-world-and-semantic-foundation
owning milestone: M031
owning milestone path: docs/milestones/MILESTONE-031-simulation-world-and-semantic-foundation.md
reviewer role: repository user or designated engine architect
acceptable completion decision: approved
```

Review subject:

> The implemented simulation-world boundaries, public semantic contracts, persistence/inspection evidence, and bounded headless wood workflow.

Required evidence is defined in the review request and artifact contract. Automation decides structural correctness, determinism, conservation, and regression. Human review decides whether the resulting public concepts, diagnostics, and evidence are coherent enough to become the foundation for later detailed and abstract execution.

No implicit waiver. A waiver is allowed only if the pending request is explicitly amended before decision and records a bounded reason.

The completed record becomes historical evidence after M031 completes. Later milestones must declare their own review and must not reopen this milestone solely because later commits change the repository.

## Constrained-runtime handling

Use the resumable suite whenever the aggregate run may exceed the execution environment.

Required procedure:

1. run `./eng/m031-smoke.sh --plan-json`;
2. inspect the plan for the complete required shard set;
3. execute every shard in a separate foreground invocation;
4. preserve receipts under the canonical receipt location;
5. complete and record the human review;
6. rerun the `human-review` shard if its prior receipt predates the approved record;
7. run `./eng/m031-smoke.sh --verify`;
8. report aggregate success only from `--verify`.

Do not use detached/background execution, timeout inflation, or partial logs as evidence.

## Out-of-scope guide migration work

No guide migration is part of M031.

The external guide repository is planning input only. Target repository documents must contain project truth and must not cite external guides as operational authority.

Legacy copied guides or research material remain non-authoritative unless active repository documentation explicitly states otherwise.
