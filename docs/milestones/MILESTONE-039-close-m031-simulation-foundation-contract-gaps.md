# Milestone 039 — Close the M031 Simulation Foundation Contract Gaps

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-broad |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Repository role | capability-provider |
| Repository profiles | artifact-first-agentic-authoring; runtime-tool; game-simulation |
| Maturity | implementation-ready; artifact-first |
| Scope size | medium-large |
| Implementation autonomy | high within this contract |
| Documentation sync | direct authority only; broader normalization deferred |
| Local validation | Tier 1 focused implementation + Tier 2 resumable closure suite |
| Integration validation | Tier 2 aggregate verifier + repository standard gate |
| Human review | none |

M039 is a corrective engine-foundation milestone inserted before the next regular gameplay/presentation milestone. It closes implementation gaps in the still-authoritative M031 simulation contract. Historical M031 completion material remains historical and is not reopened.

## Goal

Make the existing optional simulation foundation actually satisfy the architectural and semantic contract that later detailed and abstract executors depend on.

The completed foundation must have:

```text
one runtime entity/component authority
+ typed game-defined simulation components
+ one semantic world/clock
+ atomic semantic commands
+ factual post-commit events with real causality
+ enforced activity semantics
+ authoritative reservations
+ classification-correct persistence
+ genuine fresh-process continuation proof
```

The goal is closure and correction, not a new simulation architecture or a feature expansion.

## Why this milestone exists

Current project authority already requires typed runtime components, validated atomic mutation, explicit persistence classifications, semantic command/event separation, valid activity transitions, authoritative reservations, and fresh-process persistence proof.

The current implementation does not fully realize those contracts. In particular, current code uses one `SimulationEntity` component containing a `string -> JsonElement` component bag, treats persistence classifications mainly as metadata, performs multi-component domain changes as separate mutations plus a later fact record, accepts caller-supplied reservation capacity, does not enforce activity-kind stage graphs, and labels an in-process round trip as a fresh-process proof.

M039 closes those gaps. It does not rewrite historical M031 planning or review records.

## Target State

### Runtime and component authority

The existing runtime entity/component world remains the sole authoritative owner of runtime entity identity and component instances.

The simulation layer composes that runtime and owns simulation semantics:

```text
EntityComponentWorld
  ├── stable entity identity
  ├── typed component stores
  ├── validated component mutation
  └── deterministic component queries/snapshots
          ↑
SimulationWorld
  ├── regions and simulation membership semantics
  ├── authoritative semantic clock
  ├── typed simulation component registration metadata
  ├── activities and reservations
  ├── atomic semantic command coordination
  ├── factual domain events
  ├── canonical persistence
  └── semantic inspection
          ↑
game simulation modules
  ├── typed CLR component types
  ├── domain command handlers/policies
  ├── activity-kind transition policies
  └── reservation subject policies
```

A `SortedDictionary<string, JsonElement>` or equivalent untyped bag must not remain authoritative game/simulation component storage.

JSON remains valid at serialization, artifact, authored-data, and other explicit boundary layers. It is not the runtime component model.

The foundation uses the repository's existing `Agentic2D.Contracts.EntityId` semantics for authoritative entity references. Stable persisted identity remains the entity ID value, not CLR type names, component-store indexes, or object identity.

### Typed component registration

Game-defined simulation components are explicit typed CLR components registered with stable durable metadata.

Each persistent-capable registration resolves at least:

- stable component key;
- schema version;
- CLR/runtime type binding;
- persistence classification;
- deterministic codec or equivalent serialization/deserialization authority;
- inspection projection or equivalent deterministic semantic representation.

Registration order cannot affect stable keys, query order, canonical persistence, fingerprints, or command outcomes.

Duplicate component keys, duplicate incompatible type bindings, and incompatible registration metadata fail before authoritative world mutation.

The implementation may extend the existing runtime registration/mutation substrate where needed, but must not introduce a second component universe or a third-party/archetype ECS rewrite.

### Simulation entity lifecycle and region ownership

There is one authoritative entity identity space.

Simulation-specific lifecycle/region membership may be represented by typed simulation-owned state and derived indexes; implementation chooses the concrete mechanics.

Required semantics remain:

```text
created
active
inactive
destroyed
```

and:

```text
create
activate
deactivate
reactivate
transfer region
destroy
```

Region transfer preserves identity.

World-scoped versus region-owned classification is explicit and validated. A region-owned active entity has exactly one valid region. World-scoped state is not created through an implicit "region = null means anything" loophole.

Destroyed identity is not silently reusable within the world lineage.

Entity destruction cannot leave a world that captures successfully but fails its own load invariants. Active activities/reservations/references involving the destroyed entity are resolved atomically to a valid deterministic state or the destruction command is rejected before commit.

### Atomic semantic commands

A semantic command is the unit of domain mutation.

A command may stage changes across:

- multiple typed components;
- activity state;
- reservation state;
- lifecycle/region state;
- deterministic sequence state;
- factual domain events.

The command commits all authoritative mutation and its factual events atomically, or commits none of them.

The exact transaction implementation is executor-owned. It may add bounded transaction/staging support to the existing runtime, use an immutable staged state, or use another local mechanism that preserves the same authority and semantics.

The implementation must not emulate atomicity by applying live mutations one by one and then attempting best-effort rollback.

A deterministic injected or synthetic failure after one or more staged operations must prove:

```text
rejected command
+ unchanged authoritative state
+ unchanged activities/reservations
+ no factual success event
```

Current game/simulation flows that represent one semantic action as separate `SetComponent` calls followed by `RecordFact` must be migrated to the corrected semantic command boundary where those changes are part of one domain fact.

`RecordFact` may remain as an internal post-commit/event helper if useful. It must not remain a public game-rule escape hatch that can assert a factual success independently of the mutation that supposedly caused it.

### Command and event identity/cause

The existing typed command/event/correlation/causation concepts must become real runtime semantics rather than decorative declarations.

Each command result resolves:

- stable command ID;
- type key;
- issued/completed simulation instant;
- correlation;
- causation where applicable;
- expected/current revision information where applicable;
- emitted event IDs;
- structured diagnostics.

Factual events are emitted only after successful commit and resolve:

- stable event ID;
- type key;
- simulation instant;
- deterministic sequence;
- affected IDs;
- correlation;
- causation;
- typed or canonically serialized payload.

`SimulationCommandResult.EventIds` or its replacement contains the actual emitted event IDs for that command.

Correlation/causation cannot be hardcoded to an M031 proof constant. A root command may establish deterministic root correlation/causation according to implementation policy; child commands/events preserve causal linkage.

Rejected commands may consume deterministic command-sequence identity if that policy is retained, but they emit no factual success events and do not alter authoritative semantic state.

### Activities

Activities remain explicit mode-independent semantic state.

Activity kinds have registered or otherwise explicit transition authority. An activity transition cannot accept an arbitrary stage/status combination merely because the activity revision matches.

The foundation must validate:

- activity kind;
- current stage/status;
- requested next stage/status;
- expected revision;
- actor/target lifecycle prerequisites required by the registered policy;
- terminal-state rules.

Detailed and abstract executors continue to converge through the same semantic activity command boundary.

The closure does not require one universal declarative graph format. Game modules may supply typed policy/validator code. What is required is that invalid transitions are rejected by shared authority rather than prevented only by caller convention.

### Reservations

Reservations remain authoritative concurrency-control state.

A reservation request names the semantic subject, reservation kind, quantity and guards. The caller does not provide authoritative subject capacity as an arbitrary trusted integer.

Capacity/availability and subject revision are derived through registered subject policy from authoritative typed state.

Required semantics:

- deterministic conflict resolution;
- positive quantity only;
- no over-capacity reservation;
- authoritative subject/revision validation;
- atomic acquisition;
- idempotent release;
- atomic create-activity-plus-initial-reservations;
- explicit invalidation/resolution when subjects disappear or become invalid;
- no active reservations owned by a terminal activity.

A terminal activity operation (`completed`, `cancelled`, or `failed`) must resolve its owned active reservations in the same semantic transaction or reject before producing a terminal state.

### Persistence classification

Persistence classification becomes executable policy rather than metadata.

Canonical SimulationWorld persistence includes only authoritative persistent semantic state plus required compatibility/sequence metadata.

The following classifications have these behaviors:

| Classification | M039 canonical save behavior |
|---|---|
| `authoritative-persistent` | encoded and restored |
| `derived-rebuildable` | omitted from canonical authority and deterministically rebuilt when a registered rebuild path applies |
| `active-mode-transient` | omitted unless a separate explicit continuation contract makes that exact state authoritative |
| `presentation-only` | never part of SimulationWorld gameplay authority or canonical save |
| `external-handle` | never persisted |

Canonical world fingerprints cover authoritative semantic state. Omitted transient/presentation/external state cannot perturb the authoritative fingerprint.

The M039 validation fixture must exercise classifications rather than merely list them in metadata.

### Persistence compatibility boundary

M039 deliberately creates a compatibility break for the unreleased/internal simulation persistence model.

The new canonical SimulationWorld save schema is:

```text
agentic2d.simulation-world-save.v2
```

The minimum supported SimulationWorld schema after M039 is v2.

`agentic2d.simulation-world-save.v1` is not migrated. Loading it fails clearly with a stable incompatible/unsupported-schema diagnostic.

Rationale:

- the v1 shape serializes the flawed nested JSON component bag;
- the repository does not currently publish `Agentic2D.Simulation` as a package/release compatibility promise;
- old M031/M032/M033 generated artifacts are reproducible validation output, not player save compatibility authority;
- preserving v1 would force the corrected runtime to retain the architecture being removed.

Any committed/generated current reference artifacts used by validation are regenerated to v2. Historical Git content is not rewritten.

Persisted envelopes that embed the SimulationWorld save and therefore change compatibility meaning must version with the break. The current M033 multi-fidelity save becomes:

```text
agentic2d.multi-fidelity-save.v2
```

and explicitly validates the nested v2 SimulationWorld save.

The generic M035 save-compatibility policy remains authoritative for future compatibility work. M039 sets its current SimulationWorld minimum-supported boundary to v2; it does not invent a general migration framework for v1.

### Fresh-process equivalence

"Fresh process" means a real process boundary.

Required proof shape:

```text
control:
process A
  create scenario
  run to completion
  emit canonical final fingerprint

resume:
process B
  create scenario
  run to checkpoint
  write canonical v2 save
  terminate successfully

process C
  load the checkpoint
  continue to completion
  emit canonical final fingerprint

compare:
control fingerprint == resumed fingerprint
semantic invariants equal
```

The producer and consumer of the checkpoint must be separate OS process invocations.

The aggregate evidence records runner-observed process/launch provenance, checkpoint hash, exit status, and resulting fingerprints. Process IDs/launch identity are proof provenance only and are excluded from semantic fingerprints.

A scenario writer may not set a `freshProcessProof = true` constant and thereby establish success.

### Evidence integrity

Generated evidence must derive capability claims from executed observations.

The M039 closure must remove or correct current M031 evidence paths that self-assert results such as fresh-process execution or persistence-classification completeness without actually observing them.

A generated boolean/report field is not proof merely because it is named `passed`, `complete`, or `freshProcessProof`.

The engineering verifier consumes current fingerprinted shard receipts and validates required domain evidence. Only the verifier establishes aggregate M039 machine success.

### Current consumer migration

M039 owns migration of current in-repository code that directly consumes the flawed M031 storage/mutation semantics.

At minimum this includes:

- M031 bounded wood workflow and its current focused evidence;
- M032 detailed autonomous work/logistics where it reads/writes SimulationWorld components or performs semantic resource changes;
- M033 abstract/multi-fidelity simulation where it reads/writes SimulationWorld components or persists SimulationWorld state;
- M035 scale/readiness fixtures that directly create, persist, inspect, or validate SimulationWorld components/state.

The migration preserves the bounded semantic behavior those consumers already claim, while moving them onto typed components and atomic semantic commands.

M034's independent settlement-state implementation is not migrated into SimulationWorld by this closure. Auditing M034 integration is separate work.

All other direct SimulationWorld consumers discovered in the live solution must be adapted if needed for the corrected foundation to compile and satisfy current contracts.

### Source compatibility

There is no source-compatibility requirement for the current unreleased M031 SimulationWorld API surface.

Implementation may change/remove internal public-looking records, methods, signatures, and proof helpers that exist only inside this repository if doing so is necessary to restore the authoritative contracts.

The implementation must preserve semantic project contracts, stable durable IDs/keys where still valid, and current in-repository consumer behavior required by this milestone.

Do not retain obsolete adapters solely to preserve the flawed JSON-bag API.

## Scope

### Foundation closure

- replace nested untyped simulation component authority with typed runtime-owned component authority;
- align authoritative entity references with existing `EntityId` semantics;
- make registration metadata executable;
- establish atomic semantic command transactions;
- establish real command/event identity and causal linkage;
- enforce activity transition authority;
- make reservation capacity/revision authoritative;
- close lifecycle/reservation/reference holes;
- implement persistence classifications;
- introduce v2 SimulationWorld persistence and v2 multi-fidelity embedding;
- prove genuine fresh-process continuation;
- replace self-asserted validation evidence with executed proof.

### Current consumer adaptation

Adapt M031, M032, M033, and direct M035 consumers to the corrected foundation as required for existing bounded semantic behavior and validation.

### Engineering support

Add a resumable machine-only M039 closure suite and the minimum engineering/tooling support required to generate truthful fresh-process and fault-path evidence.

### Direct documentation

Update current authority that would otherwise contradict the implemented result, including command indexes and current save/schema statements.

## Non-goals

Do not:

- rewrite the underlying ECS into archetypes, sparse sets, or a third-party ECS;
- optimize component storage based on speculative performance concerns;
- implement multithreaded simulation;
- add a dynamic plugin/assembly-scanning component system;
- migrate M034's independent settlement model into SimulationWorld;
- redesign M033 discrete-event scheduling or region-fidelity gameplay;
- add new gameplay, needs, logistics, infrastructure, presentation, terrain, animation, or asset features;
- implement the deferred Human Integration milestone;
- preserve M031 v1 save compatibility;
- create a generic historical-save migration framework;
- reopen or rewrite historical M031 review/completion records;
- require human inspection of JSON/reports to establish machine-verifiable correctness;
- add graphical validation or human review;
- perform broad documentation normalization;
- add public package/release compatibility promises;
- copy external guide documents into the repository.

## Decisions and Constraints

1. Existing M031 specification and simulation-foundation architecture are desired semantic authority; M039 fixes implementation to match them rather than replacing them with a new model.
2. `EntityComponentWorld` remains the sole runtime entity/component authority.
3. Game-defined simulation components are typed CLR components; string-keyed JSON is boundary representation only.
4. No compatibility shim may preserve an authoritative nested JSON component universe.
5. `SimulationWorld` remains the semantic composition boundary for regions, clock, activities, reservations, persistence, inspection, and executor convergence.
6. Atomic semantic commands span every authoritative store they affect and emit facts only after commit.
7. Activity validity and reservation subject capacity/revision are shared-authority concerns, not caller convention.
8. Current in-repository M032/M033/M035 direct consumers migrate in this milestone; unrelated M034 architecture does not.
9. SimulationWorld save v2 is a deliberate clean break; v1 is rejected, not migrated.
10. M033 multi-fidelity save v2 embeds/validates SimulationWorld v2.
11. Generated historical validation output may be discarded/regenerated; historical Git records and completed review records remain immutable.
12. Fresh-process proof requires separate process invocations and runner-observed provenance.
13. M039 is entirely machine-verifiable and has no human-review gate.
14. Active development platform is Windows under the existing platform epoch; portable semantics remain cross-platform and Linux-specific revalidation may remain deferred according to platform authority.
15. No model escalation is required: implementation mechanics are intentionally executor-owned once these project-level decisions are fixed.

## Baseline Executor Readiness

This milestone is `ready` for GPT-5.6 Luna.

Planning has resolved:

- component/storage architecture;
- authoritative entity identity boundary;
- atomic mutation semantics;
- command/event causality semantics;
- activity validation ownership;
- reservation capacity/revision ownership;
- terminal/lifecycle invariant expectations;
- persistence classifications;
- save compatibility break and schema versions;
- current consumer migration boundary;
- historical evidence/review treatment;
- acceptance and validation policy;
- fresh-process proof semantics;
- human-review applicability;
- constrained execution shape.

Implementation remains free to choose local:

- concrete typed component records/structs;
- registration API shape;
- transaction/staging implementation;
- runtime extension mechanics;
- activity-policy representation;
- reservation-policy representation;
- exact test class/file structure;
- process-runner implementation details;
- refactoring order;
- artifact serialization types;
- supporting source/doc edits.

Those choices must stay within this contract and existing authority.

## Required Authority

Read in this order after `AGENTS.md` and this milestone:

1. `docs/specs/entity-component-runtime-contract.md`
2. `docs/specs/simulation-world-and-semantic-foundation-contract.md`
3. `docs/architecture/simulation-foundation-architecture.md`
4. `docs/decisions/ADR-0016-runtime-owns-entities-components-and-spatial-modules-own-spatial-semantics.md`
5. `docs/decisions/ADR-0051-close-m031-with-typed-components-and-atomic-semantic-transactions.md`
6. `docs/specs/save-compatibility-and-recovery-contract.md`
7. `docs/specs/autonomous-work-and-detailed-logistics-contract.md`
8. `docs/specs/detailed-grid-navigation-and-activity-execution-contract.md`
9. `docs/specs/discrete-event-simulation-contract.md`
10. `docs/specs/abstract-activity-and-travel-contract.md`
11. `docs/specs/region-fidelity-and-reconciliation-contract.md`
12. `docs/specs/multi-fidelity-equivalence-contract.md`
13. `docs/specs/runtime-health-and-diagnostics-contract.md`
14. `docs/engineering/command-contract.md`
15. `docs/engineering/validation-tiers.md`
16. `eng/platform-verification.json`
17. `docs/engineering/platform-verification.md`

Inspect current M031/M032/M033/M035 source and focused tests as needed.

Do not read `.guide-profile.json`, `.guide-sync/`, the external guide repository, planning conversation, or historical M031 review material during ordinary implementation unless needed to verify the explicit "do not rewrite historical records" obligation.

## Acceptance Criteria

### Typed component authority

- Simulation/game components participating in authoritative runtime state are real typed runtime components registered through the existing runtime authority.
- No authoritative `string -> JsonElement` component bag remains inside SimulationWorld entities.
- Registration binds stable key, schema version, CLR/runtime type, persistence classification, codec, and deterministic inspection representation.
- Registration-order permutation produces identical registration fingerprint/semantic output.
- Duplicate/incompatible registrations fail before authoritative mutation.
- Current M032/M033/M035 direct consumers no longer depend on the removed component bag.

### Entity and partition semantics

- Existing `EntityId` semantics are used for authoritative simulation entity references.
- Region-owned and world-scoped state are explicitly validated.
- active -> inactive -> active is supported deterministically.
- Region transfer preserves identity.
- Destroying an entity involved in active semantic state cannot leave a capturable-but-unloadable world.
- Tombstoned identity is not silently reused.

### Atomic semantic commands

- At least one current resource-transfer/harvest/deposit semantic path changes multiple authoritative values and emits its factual event through one atomic command.
- A deterministic failure inserted after at least one staged mutation proves zero authoritative commit and zero factual success events.
- Command results contain the actual emitted event IDs.
- Event correlation/causation is derived from real command context rather than hardcoded M031 proof constants.
- Current M032 and M033 semantic resource changes no longer implement one domain fact as unrelated live `SetComponent` mutations followed by an independently recorded success fact.

### Activities and reservations

- Invalid activity stage/status transitions are rejected even when revision is otherwise current.
- Stale revision remains safely rejected.
- Initial activity plus initial reservation acquisition is atomic.
- Reservation availability/capacity is derived from authoritative subject state through registered policy; a caller cannot increase capacity by passing a larger integer.
- Subject revision/guard semantics are enforced.
- Release remains idempotent.
- Completed, cancelled, and failed activities cannot retain active reservations.
- Entity destruction/reservation invalidation paths preserve loadable referentially valid state.

### Persistence classification and schema

- `SimulationWorld.SaveSchema` is `agentic2d.simulation-world-save.v2`.
- A validation fixture containing authoritative, derived, transient, presentation, and external-handle classifications proves the required inclusion/omission behavior.
- Authoritative persistent typed state survives save/load with stable semantic value.
- Derived state used by the fixture is rebuilt through registered authority where applicable.
- transient/presentation/external state is absent from canonical save authority and cannot alter the authoritative world fingerprint.
- v1 SimulationWorld save is rejected clearly and does not mutate the destination.
- current M033 multi-fidelity persistence uses `agentic2d.multi-fidelity-save.v2` and validates its nested SimulationWorld v2 state.
- full load validation occurs before authoritative commit and rejects malformed/unknown/incompatible required component state without partial destination mutation.

### Genuine fresh-process proof

- The M039 fresh-process shard launches separate producer and consumer OS processes.
- The producer writes a v2 checkpoint and terminates before the consumer loads it.
- Runner-generated evidence records distinct launch/process provenance, checkpoint hash, exit results, and direct/resumed fingerprints.
- Direct and resumed final authoritative fingerprints and declared semantic invariants are equal.
- No scenario/artifact writer can establish fresh-process success by writing a constant boolean.

### Evidence integrity

- Current M031 persistence/classification evidence no longer self-asserts unexecuted properties.
- Every aggregate closure claim is backed by a current validation receipt and/or domain artifact produced from executed observations.
- The M039 verifier fails for missing/stale receipts, missing evidence, fingerprint mismatch, failed domain evidence, or a fresh-process proof that does not show separate invocations.

### Current consumer regression

- M031 bounded wood workflow still demonstrates deterministic resource conservation using the corrected semantic command path.
- M032 detailed-region bounded headless scenario preserves its existing autonomous-work/logistics semantic outcomes through typed components and corrected commands.
- M033 bounded multi-region/multi-fidelity headless scenario preserves deterministic scheduler/fidelity outcomes and v2 persistence.
- M035 direct SimulationWorld scale/readiness fixtures compile and execute against the corrected typed foundation.
- M034 remains behaviorally untouched except for incidental compile/supporting edits that do not migrate its independent model.

### Cleanup and documentation

- Obsolete JSON-bag-only component APIs/helpers are removed unless retained solely as non-authoritative inspection/serialization adapters with no gameplay mutation authority.
- Obsolete proof code that fabricates `freshProcessProof`, classification completeness, or equivalent capability claims is removed or changed to consume executed evidence.
- Current engineering command documentation indexes the M039 closure suite and no canonical command description claims the old false fresh-process proof.
- Current simulation persistence documentation states v2/current minimum and explicit v1 incompatibility.
- Historical M031 milestone and completed `.review/records/review.m031...` record remain unchanged.

## Validation

### Execution mode

`resumable-sharded`

### Active-platform plan

Windows is the active development platform.

Run:

```powershell
pwsh ./eng/suite.ps1 m039-smoke --plan-json
pwsh ./eng/suite.ps1 m039-smoke --shard typed-component-authority
pwsh ./eng/suite.ps1 m039-smoke --shard semantic-command-atomicity
pwsh ./eng/suite.ps1 m039-smoke --shard activities-and-reservations
pwsh ./eng/suite.ps1 m039-smoke --shard persistence-classification
pwsh ./eng/suite.ps1 m039-smoke --shard fresh-process-equivalence
pwsh ./eng/suite.ps1 m039-smoke --shard current-consumer-regression
pwsh ./eng/suite.ps1 m039-smoke --shard evidence-integrity
pwsh ./eng/suite.ps1 m039-smoke --verify
```

Bash exposes the same suite/shard semantics through:

```bash
./eng/suite.sh m039-smoke --plan-json
./eng/suite.sh m039-smoke --shard <id>
./eng/suite.sh m039-smoke --verify
```

### Shard contract

| Shard | Required proof |
|---|---|
| `typed-component-authority` | sole runtime component authority, typed registration/binding, registration determinism, region/lifecycle identity behavior, no authoritative JSON bag |
| `semantic-command-atomicity` | multi-store atomic command, deterministic failure rollback, no success fact on failure, real command/event IDs and causal linkage |
| `activities-and-reservations` | activity transition policy, stale revision, authoritative subject capacity/revision, atomic initial acquisition, terminal cleanup, destroy/reference validity |
| `persistence-classification` | SimulationWorld v2, classification inclusion/omission/rebuild, transactional invalid-load behavior, v1 rejection, nested multi-fidelity v2 compatibility |
| `fresh-process-equivalence` | real producer/consumer process boundary, checkpoint hash/provenance, direct-versus-resumed semantic/fingerprint equivalence |
| `current-consumer-regression` | bounded M031/M032/M033 and direct M035 foundation consumers retain required headless outcomes on corrected foundation |
| `evidence-integrity` | old self-asserted proof paths cannot establish pass; required domain artifacts are observation-derived and consistent with current receipts |

### Receipt location

```text
artifacts/validation/m039-smoke/<shard>.json
```

### Fingerprint scope

Use the repository's existing engineering-host model:

- repository fingerprint;
- suite fingerprint;
- command fingerprint;
- input fingerprint;
- result/evidence fingerprint.

Any change within a shard's declared input/evidence authority invalidates the relevant receipt according to existing EngineeringHost behavior.

### Aggregate verifier

Only:

```powershell
pwsh ./eng/suite.ps1 m039-smoke --verify
```

or the Bash equivalent establishes aggregate M039 machine validation success.

Partial shard output, a direct script exit code, generated JSON with a `passed` field, historical M031 approval, or old M031 suite output is not aggregate M039 success.

### Required domain evidence

The suite must produce or validate current observation-derived evidence under:

```text
artifacts/simulation/M039/
```

covering at minimum:

- typed component registration/authority;
- semantic transaction rollback/commit;
- activity/reservation invariants;
- persistence classification and schema;
- fresh-process equivalence/provenance;
- current-consumer regression;
- aggregate closure summary.

Concrete JSON types/files are implementation-owned as long as the suite declares them as evidence and the verifier can validate the acceptance criteria without human interpretation.

### Repository standard validation

After/following the resumable suite, run:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

Use native Bash equivalents on Linux.

Do not use old M031 human-review approval as an M039 gate.

### Capability-provider versus consumer validation

M039 is capability-provider closure with bounded in-repository dogfood regression.

Provider authority is the corrected reusable simulation foundation.

M031/M032/M033/M035 cases are bounded regression consumers proving existing engine code still composes against it. They do not authorize new game features.

## Human Review

Applicability: `none`

Reason: every M039 completion criterion is architectural, semantic, persistence, compatibility, deterministic, or evidence-integrity behavior that automation can decide.

No `.review/pending/` request is created.

Do not reopen historical M031 review.

There is no M039 `review-check` completion gate.

## Constrained Execution

M039 validation is intentionally resumable because the closure crosses engine, simulation, persistence, and several bounded consumer fixtures.

The implementation agent must:

1. run `--plan-json`;
2. execute each required shard in a separate invocation;
3. resolve ordinary shard failures and rerun affected shards;
4. run `--verify`;
5. treat only the verifier as aggregate suite success.

Do not use backgrounding, detached processes, shell timeout inflation, or partial child logs as aggregate proof.

The `fresh-process-equivalence` shard itself is explicitly authorized to spawn bounded foreground child processes as part of the test. Those children are test subjects managed synchronously/asynchronously by the shard runner and must terminate before the shard completes. This is not background execution.

No graphics capability is required.

Inactive Linux platform verification remains governed by the current platform epoch. M039 must remain portable; absence of fresh Linux execution does not block ordinary completion while Windows is active.

## Direct Documentation Impact

Implementation must update directly contradicted current authority, including as applicable:

- `README.md` current capability wording if it still describes the old persistence/component implementation;
- `docs/specs/simulation-world-and-semantic-foundation-contract.md` with the concrete v2/current compatibility statement when needed;
- `docs/specs/save-compatibility-and-recovery-contract.md` only where the current minimum-supported SimulationWorld boundary must be explicit;
- `docs/architecture/simulation-foundation-architecture.md` only if implementation exposes a direct contradiction to its current typed-runtime layering;
- `docs/ENGINEERING.md`;
- `docs/engineering/command-contract.md`;
- any M032/M033/M035 spec sentence directly contradicted by migration to typed/atomic foundation semantics.

Do not rewrite historical milestone documents merely to describe the corrected current implementation.

## Deferred Documentation Synchronization

Planning adds one focused `.guide-sync/pending/` hint concerning evidence-integrity guidance: machine evidence must not claim an execution property such as fresh-process or classification completeness when the artifact producer did not actually observe it.

M039 implementation does not read or resolve that hint.

Broad guide/documentation synchronization remains a separate pass.

## Historical Material Policy

Historical M031 milestone documentation and completed review records remain immutable evidence of what was accepted at that time.

M039 does not:

- delete or amend the completed M031 review record;
- change its human decision;
- pretend the historical review is stale;
- require the repository user to reapprove M031.

Generated M031 artifacts may be regenerated or replaced when canonical engineering commands now produce corrected truthful evidence.

## Completion Audit

### Continue implementation

Continue when any agent-resolvable obligation remains, including:

- a current direct consumer still uses the authoritative JSON bag;
- a semantic domain action still performs live partial mutations before its factual event;
- a classification is metadata-only;
- v1 is still silently loadable as current authority;
- fresh-process evidence is still self-asserted or same-process;
- a terminal activity can leak active reservations;
- command results do not resolve emitted events/causality;
- a required M039 shard/standard validation fails;
- direct documentation contradicts implemented current truth.

### COMPLETE

Use only when:

- every applicable acceptance criterion is satisfied;
- all required M039 shard receipts are current/passing;
- `m039-smoke --verify` passes;
- repository standard validation passes;
- required domain evidence exists and is verifier-consistent;
- direct documentation obligations are complete;
- historical M031 records are untouched;
- no human-review gate exists;
- no material planning decision remains.

### BLOCKED

Use only for an unavailable external capability or a newly discovered material decision that would change this ready contract.

Do not use `BLOCKED` for ordinary refactoring, failing tests, missing evidence, or consumer adaptations that are resolvable in the repository.

`AWAITING HUMAN REVIEW` is not a valid expected terminal state for M039 because human review does not apply.

## Escalation Boundary

Return to planning if implementation reveals that completion requires any of the following:

- preserving SimulationWorld v1 save compatibility;
- a public/package source-compatibility promise for the current Simulation API;
- replacing `EntityComponentWorld` with a new ECS architecture;
- changing the one-authoritative-world model;
- changing the detailed/abstract executor ownership model;
- changing M032/M033 gameplay semantics rather than adapting them;
- migrating M034 into the simulation foundation;
- introducing a generic migration/plugin/reflection framework;
- changing persistence classification meanings;
- weakening the atomic semantic command rule;
- making human review necessary to decide a criterion that planning classified as machine-verifiable.

Do not escalate:

- concrete typed component design;
- transaction implementation;
- test fixture design;
- process-launch mechanics for the fresh-process proof;
- local API refactoring;
- exact diagnostic IDs/messages where existing stable families suffice;
- exact artifact JSON layout;
- implementation sequence.

## Baseline-Executability Audit

Confirmed before `ready`:

- **architecture** — sole component authority and simulation layering are settled;
- **semantics** — atomic commands, factual events, activity validation, reservation ownership, lifecycle invariants are settled;
- **compatibility** — source compatibility is not required; SimulationWorld v1 is rejected; v2 schemas are fixed;
- **scope** — M031 foundation plus direct M032/M033/M035 consumers only; M034/new gameplay excluded;
- **acceptance** — every identified closure defect has an observable machine criterion;
- **validation** — resumable seven-shard suite, receipt authority, fresh-process proof, and standard gate are explicit;
- **human review** — none, because all criteria are mechanically decidable;
- **external dependencies** — existing .NET/runtime/engineering stack only; no new external service or graphics dependency;
- **constrained execution** — validation is resumable and fresh-process child work is bounded/foreground;
- **baseline model** — GPT-5.6 Luna can execute without inventing architecture, semantics, compatibility, scope, acceptance, or validation policy.

No unresolved material planning issue prevents `ready` status.
