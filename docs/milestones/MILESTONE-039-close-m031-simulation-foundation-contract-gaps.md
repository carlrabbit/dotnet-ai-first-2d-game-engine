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

M039 is a corrective engine-foundation milestone inserted before the next regular gameplay/presentation milestone.

This revision is the corrective planning authority for PR 22. It supersedes the first M039 implementation direction where `SimulationWorld` introduced a private heterogeneous `componentValues` store and where typed M032 migration was reverted after regressions.

Historical M031 milestone/review records remain historical and are not reopened.

## Goal

Make the optional simulation foundation satisfy the repository's one-runtime-component-authority contract while preserving the semantic behavior of current M031/M032/M033/direct-M035 consumers.

The target architecture is:

```text
EntityComponentWorld
  owns entity identity
  owns every runtime component instance
  owns typed component validation/query/storage
  owns heterogeneous component batch commit support
          ↑
          │ shared registered descriptors
          │
SimulationWorld
  owns simulation semantics
  regions/lifecycle
  semantic clock
  activities/reservations
  semantic transactions
  command/event causality
  persistence policy
  inspection
          ↑
          │ typed component APIs
          │
simulation/game modules
```

The closure must not replace the JSON component bag with a second `object` component bag.

## Why this correction exists

PR 22 confirmed that M032 is deeply coupled to JSON-shaped component projections. The attempted fix also exposed a more fundamental issue:

```text
SimulationWorld
  private Dictionary<component-key, Dictionary<entity-id, object>>
```

became the real simulation component store while `EntityComponentWorld` continued to own only the `SimulationEntity` wrapper.

That violates the existing M013/M031 project contract that the runtime owns component instances.

The correction therefore treats the M032 failures as a consumer migration problem after the runtime boundary is corrected, not as justification for retaining JSON-shaped gameplay access or a `SimulationWorld`-owned shadow store.

## Target State

### One authoritative component universe

`EntityComponentWorld` is the sole runtime authority for entity existence/identity, registered component descriptors, authoritative component values, component validation, deterministic queries, component mutation/batch commit, and component snapshots.

`SimulationWorld` MUST NOT own any parallel component-value dictionary, object store, JSON store, or other mutable component universe.

Forbidden as authoritative component state:

```text
SimulationEntity.Components : string -> JsonElement
SimulationWorld.componentValues : string -> object store
any equivalent shadow component store outside EntityComponentWorld
```

JSON/object representations may exist only as derived/boundary representations for persistence encoding, inspection/artifacts, CLI/content boundaries, and diagnostics.

### Runtime descriptor boundary

The runtime gains one explicit type-erased descriptor abstraction over the same stores used by the generic typed API.

The exact concrete type/API is implementation-owned, but a descriptor resolves at least stable component type ID, CLR runtime type, owner, validator, canonical serializer and canonical deserializer.

The runtime supports explicit registration only. Dynamic assembly scanning/plugin discovery is not introduced.

The existing ergonomic domain API remains typed. A bounded non-generic/type-erased infrastructure surface may exist for persistence, inspection, explicit heterogeneous transaction staging/commit, and descriptor lookup by stable type ID.

Both generic and type-erased surfaces MUST operate on the same `EntityComponentWorld` stores.

### Durable identity versus CLR binding

Durable component identity is based on stable component key, schema version and codec/version metadata where compatibility-relevant.

CLR `Type` is runtime binding information.

Assembly-qualified type names, assembly versions, load paths, or equivalent CLR deployment identity MUST NOT be persisted as canonical component identity, determine save compatibility, or affect canonical registration fingerprints.

Persistence/load resolves stable component keys through registered descriptors rather than `Type.GetType(assemblyQualifiedName)` as durable authority.

### Immutable authoritative components

Authoritative runtime component values are immutable values from the perspective of consumers.

A component read must not expose a reference that can be modified to bypass runtime validation, mutation evidence/events, semantic transaction authority, or deterministic evidence.

Preferred forms are immutable records, readonly record structs, or equivalent immutable value objects. Boundary DTOs may be mutable only if authoritative storage is defensively isolated from them.

### SimulationWorld component access

Simulation/game code accesses authoritative components through typed APIs or typed semantic query services.

Gameplay/selection/logistics logic MUST NOT depend on:

```text
entity.Components["component..."].GetProperty(...)
```

`SimulationEntity` may remain as an identity/lifecycle/region inspection projection if useful, but it does not own or expose mutable authoritative component storage.

A separate read-only inspection projection may serialize typed components to stable keyed JSON for tooling.

### Component granularity

M039 does not redesign M032/M033 domain decomposition.

M032 may retain approximately the current logical families such as worker, harvestable, storage, designation, need source, dormant/sentinel and transient route state.

M033 may retain its current worker/resource/fidelity families.

Do not split components into speculative ECS micro-components merely to make the migration look more ECS-like.

### Heterogeneous atomic component batch

`EntityComponentWorld` gains enough bounded infrastructure to validate and commit a heterogeneous set of typed component mutations atomically.

Required semantics:

1. identify all entities/components to mutate;
2. resolve descriptors and typed values;
3. validate every mutation against current state/descriptor rules;
4. stage without making changes visible;
5. reject with zero component mutation if any staged mutation is invalid;
6. commit the complete component batch atomically.

Exact mechanics remain implementation-owned.

Do not implement apparent atomicity as sequential live writes plus compensating rollback.

### Semantic transaction boundary

`SimulationWorld` owns the larger semantic transaction because a game fact may span multiple typed component mutations, activities, reservations, lifecycle/region state, sequence state and factual domain events.

Conceptually:

```text
Simulation semantic command
  -> validate domain/activity/reservation preconditions
  -> stage EntityComponentWorld component batch
  -> stage SimulationWorld semantic state
  -> validate complete transaction
  -> commit component + semantic state
  -> publish factual events
```

A semantic fact such as harvest/deposit/resource transfer is not represented by live `SetComponent`, live `SetComponent`, then `RecordFact`.

A rejected command commits nothing and emits no factual success event.

### Command/event identity and causality

Retain PR 22's useful command/event direction.

A successful semantic command result exposes the actual factual event IDs emitted by that command.

Correlation/causation uses deterministic semantic IDs, not milestone-proof constants.

Rejected commands produce diagnostics but no factual success events.

### Activities and reservations

Retain the M039 closure goals: registered/shared activity transition authority, stale revision rejection, atomic create-activity-plus-initial-reservations, deterministic reservation conflict handling, terminal cleanup, and destroy/reference invariants.

Reservation capacity and subject revision/guard semantics MUST come from authoritative typed state or an explicitly registered semantic policy that reads authoritative typed state.

A reservation policy MUST NOT use a constant such as `18` for a storage entity when authoritative storage capacity exists on the subject component.

Caller-supplied capacity is not authority.

### Persistence

The compatibility decision remains:

```text
SimulationWorld: agentic2d.simulation-world-save.v2
minimum supported: v2
v1: explicitly rejected
M033 multi-fidelity: agentic2d.multi-fidelity-save.v2
```

Persistence enumerates registered descriptors and reads typed values directly from `EntityComponentWorld`.

For each persisted component value, canonical save authority resolves stable component key, component schema version and canonical encoded payload.

Load validates save/envelope metadata, resolves stable component key to descriptor, decodes to the registered CLR type, validates all runtime/semantic references, stages complete state, and commits only after complete validation.

Persistence classifications remain executable: authoritative-persistent is stored/restored; derived-rebuildable is omitted/rebuilt; active-mode-transient is omitted unless separately authoritative; presentation-only and external-handle are never SimulationWorld save authority.

### Inspection boundary

Inspection is a derived read-only projection.

It may expose component data as keyed JSON for tooling/artifacts. That representation MUST be produced from typed ECS-owned state and MUST NOT be fed back into gameplay as the normal read/mutation model.

### M032 migration

M032 is migrated coherently rather than partially.

The migration covers initial entity/component creation, work designation inspection, work opportunity derivation, worker selection/evaluation, navigation inputs derived from components, resource/inventory mutation, designation mutation, needs interruption, persistence continuation, and evidence projections.

M032 semantics remain the same unless current behavior directly contradicts current project authority.

The 11 regressions observed during the reverted PR-22 experiment are migration failures to resolve, not permission to retain JSON-shaped authoritative access.

### M033 and direct M035 migration

M033 must not remain nominally typed while continuing to create components from anonymous-object JSON, read `SimulationEntity.Components`, or mutate through JSON-shaped atomic commands.

M033 is migrated to the same typed runtime/component transaction boundary.

Direct M035 `SimulationWorld` fixtures are adapted where they touch the corrected foundation.

M034 remains out of scope except incidental compile support.

### Evidence integrity

All existing M039 receipts produced before this corrective authority are invalid as milestone-completion evidence.

The implementation MUST rerun all seven M039 shards and the verifier.

The validation suite MUST derive pass predicates from actual state/behavior.

Evidence fields may summarize observed predicates only when shard pass/fail logic verifies the corresponding condition.

In particular, `typed-component-authority` MUST mechanically establish that no authoritative component values live outside `EntityComponentWorld`; `activities-and-reservations` must actually exercise terminal cleanup and authoritative capacity/guards; and `persistence-classification` must construct and observe the required classification cases rather than merely listing them.

## Scope

### Runtime foundation correction

- add explicit registered type-erased component descriptor support inside `EntityComponentWorld`;
- retain generic typed APIs over the same stores;
- add bounded heterogeneous atomic component batch support;
- enforce immutable/read-only component access;
- make stable component keys/schema/codec metadata durable identity, not CLR assembly names.

### Simulation foundation correction

- remove PR-22 shadow component storage;
- remove authoritative JSON component exposure;
- bind simulation registration/persistence/inspection to runtime descriptors;
- retain/finalize atomic semantic commands, causality, activity/reservation/lifecycle fixes;
- retain v2 persistence and fresh-process proof direction.

### Consumer migration

- fully migrate M031;
- fully migrate M032;
- fully migrate M033;
- migrate direct M035 `SimulationWorld` consumers as needed;
- leave M034 architecture independent.

### Validation correction

- correct M039 probes that self-assert architectural/semantic properties;
- rerun every M039 shard after implementation;
- retain real fresh-process producer/consumer proof.

## Non-goals

Do not replace `EntityComponentWorld` with an archetype/sparse-set/third-party ECS, create a second runtime/component store, keep a `SimulationWorld Dictionary<string, object>` shortcut, keep gameplay dependent on JSON property lookup, perform dynamic assembly scanning/plugin discovery, make assembly-qualified type names persistence identity, redesign M032 component granularity without proven need, optimize ECS layout speculatively, add multithreaded simulation, change M032/M033 gameplay merely to simplify migration, migrate M034, preserve SimulationWorld v1, add new gameplay/presentation features, add human review, or rewrite historical M031 milestone/review records.

## Decisions and Constraints

1. `EntityComponentWorld` is the sole owner of authoritative runtime component values.
2. A type-erased descriptor/infrastructure API is permitted and required where generic APIs cannot express persistence/inspection/heterogeneous transactions.
3. Generic and type-erased APIs operate on the same runtime stores.
4. `SimulationWorld` owns simulation semantics, not component values.
5. Durable identity is stable component key/schema/codec metadata; CLR type/assembly identity is runtime-only.
6. Authoritative component values are immutable/read-only to consumers.
7. JSON is a boundary/inspection representation, not gameplay component authority.
8. M032/M033 migrate to typed reads and typed semantic transactions; regressions are fixed rather than bypassed.
9. Heterogeneous component batches validate fully before commit.
10. Simulation transactions span component plus semantic state and publish facts only after commit.
11. Reservation capacity/revision is derived from authoritative typed state/policy, never trusted caller constants.
12. SimulationWorld v2 and M033 multi-fidelity v2 compatibility decisions remain unchanged.
13. Existing pre-correction M039 receipts are invalid for completion and must be regenerated.
14. M039 remains entirely machine-verifiable.

## Baseline Executor Readiness

This corrective revision is `ready` for GPT-5.6 Luna.

Planning has resolved the newly exposed material questions: heterogeneous descriptor ownership, allowance of a type-erased runtime surface, prohibition of `SimulationWorld` shadow storage, immutable component semantics, durable keys versus CLR assembly identity, M032/M033 migration direction, heterogeneous batch ownership, persistence/load descriptor behavior, and treatment of the failed PR-22 validation evidence.

Implementation owns concrete descriptor types, batch API shape, immutable component types, exact refactoring sequence, test organization, and local process/tooling mechanics.

## Required Authority

Read after `AGENTS.md` and this milestone:

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

Inspect current PR-22 source/tests as needed.

Do not read the external guide repository, `.guide-profile.json`, `.guide-sync/`, or the planning conversation during ordinary implementation.

## Acceptance Criteria

### Sole runtime component authority

- Every authoritative simulation/game component instance participating in M031/M032/M033/direct-M035 is stored in `EntityComponentWorld`.
- No `SimulationWorld` shadow component-value store exists.
- `SimulationEntity` or equivalent semantic entity projection contains identity/lifecycle/region semantics only; any component JSON it exposes is clearly derived inspection data, not authoritative state.
- A machine test fails if a second authoritative simulation component store is introduced.

### Descriptor and typed API

- Explicit runtime component descriptors resolve stable type ID, CLR type, validation and deterministic codec semantics.
- Generic typed APIs and type-erased infrastructure APIs address the same underlying registration/store.
- Stable component registration/fingerprint semantics do not include assembly-qualified names, assembly versions, paths, or process-specific CLR metadata.
- Registration-order permutation remains deterministic.
- Duplicate/incompatible registrations reject before mutation.

### Immutable component state

- M031/M032/M033 authoritative component types are immutable values or defensively copied so consumer mutation cannot modify stored authoritative state.
- A focused test proves that reading a component cannot mutate runtime state without a runtime/semantic mutation command.

### Heterogeneous batch atomicity

- The runtime can stage at least two different typed component families in one batch.
- A deterministic failure after staging multiple mutations commits none.
- Successful batch commit updates all component values as one runtime commit boundary.
- No compensating rollback after visible live writes is used as the atomicity mechanism.

### Semantic transaction

- At least one M032 resource transfer/harvest/deposit path stages multiple typed component changes and its factual event under one semantic command.
- Failure commits no component/activity/reservation mutation and no factual success event.
- Command results contain actual emitted event IDs and real causal linkage.
- `RecordFact` cannot be used as a normal gameplay escape hatch to assert success independently of its state transition.

### Typed M032

- M032 creation registers and stores real CLR component values.
- Work opportunity derivation and worker selection read typed components.
- Designation changes use typed mutation.
- Resource/inventory/needs flows use typed semantic transactions.
- Persistence continuation restores typed state.
- Existing deterministic M032 semantic outcomes pass without JSON gameplay access.
- The previously observed 11 regressions are resolved rather than excluded/reverted.

### Typed M033/direct M035

- M033 creation, reads, mutations and persistence use typed runtime components.
- M033 does not use `SimulationEntity.Components`/`JsonElement.GetProperty` for authoritative gameplay state.
- Direct M035 `SimulationWorld` fixtures compile and execute against the corrected foundation.

### Reservations and activities

- Activity transition policy remains enforced.
- Reservation capacity/guard logic demonstrably reads authoritative typed subject state or registered policy over that state.
- A caller cannot increase storage/resource capacity by supplying a larger constant.
- Terminal activities leave no active reservations.
- Entity destruction preserves valid/loadable semantic state.

### Persistence and compatibility

- SimulationWorld v2 persists stable component keys/schema/payloads, not CLR assembly identity.
- Load resolves stable keys through registered descriptors and reconstructs typed component values.
- Persistence classification inclusion/omission/rebuild is mechanically exercised.
- v1 remains clearly rejected.
- M033 multi-fidelity v2 embeds valid SimulationWorld v2.
- malformed/unknown/incompatible component payloads reject transactionally.

### Inspection

- Machine-readable JSON inspection remains available where current tooling requires it.
- Inspection is derived from typed ECS-owned state.
- Gameplay logic does not consume inspection JSON as its normal component API.

### Evidence integrity

- `typed-component-authority` mechanically establishes sole `EntityComponentWorld` ownership.
- `activities-and-reservations` pass logic verifies terminal cleanup and authoritative capacity/guard behavior rather than emitting constant claims.
- `persistence-classification` constructs/observes required classification cases rather than listing omitted classifications as constants.
- real fresh-process producer/consumer proof remains separate OS process evidence.
- all seven M039 receipts are regenerated after the corrective implementation.
- `m039-smoke --verify` rejects stale pre-correction receipts.

### Cleanup

- PR-22 `SimulationWorld.componentValues` or equivalent shadow store is removed.
- persisted/runtime use of assembly-qualified component names is removed.
- mutable M032/M033 authoritative component DTOs are replaced or made safely immutable.
- obsolete authoritative JSON-bag APIs/helpers are removed unless retained solely as read-only boundary adapters.
- historical M031 milestone/review records remain untouched.

## Validation

Execution mode: `resumable-sharded`.

Active platform: Windows.

Discard/ignore existing M039 receipts as completion evidence, then run all required shards:

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

Receipt location:

```text
artifacts/validation/m039-smoke/<shard>.json
```

Only `--verify` establishes aggregate success. Required evidence remains under `artifacts/simulation/M039/`. The verifier validates evidence contents, not only presence/status strings.

After M039 verification:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

No graphics validation is required.

## Human Review

Applicability: `none`.

All M039 acceptance criteria are mechanically decidable.

No M039 `.review` request is created and historical M031 review is not reopened.

## Direct Documentation Impact

Implementation updates directly contradicted authority only.

At minimum preserve consistency among this milestone, the entity-component runtime contract, the simulation-world contract, ADR-0051, current save compatibility authority, and current engineering command/suite documentation.

Do not perform broad documentation normalization.

## Deferred Documentation Synchronization

The existing M039 evidence-integrity `.guide-sync/pending/` hint remains sufficient. No new guide-sync hint is required by this correction.

## Completion Audit

### Continue implementation

Continue if any agent-resolvable gap remains, including component values outside `EntityComponentWorld`, gameplay JSON property lookup, partially reverted/excluded M032 migration, nominally typed but JSON-operated M033, mutable component references bypassing mutation authority, durable fingerprints depending on assembly-qualified CLR names, sequential live mutation plus rollback, reservation capacity constants substituting for authoritative state, self-asserted evidence, or failing validation.

### COMPLETE

Use only when all acceptance criteria pass, every regenerated shard receipt is current, `m039-smoke --verify` passes, repository standard validation passes, direct docs are consistent, and historical M031 review remains untouched.

### BLOCKED

Use only for unavailable external capability or a newly discovered material planning decision that changes this ready contract.

`AWAITING HUMAN REVIEW` does not apply.

## Escalation Boundary

Return to planning if implementation requires preserving a second component store, changing `EntityComponentWorld` as sole component authority, replacing the ECS architecture, changing v2/v1 compatibility policy, materially changing M032/M033 gameplay semantics, redesigning project-wide component granularity, migrating M034, introducing a plugin/reflection-discovery framework, weakening immutable/read-only component semantics, weakening atomic transaction semantics, or changing human-review/validation policy.

Do not escalate local descriptor types, generic/non-generic API signatures, batch data structures, immutable record choices, test structure, refactoring order, or bounded supporting edits.

## Baseline-Executability Audit

Confirmed:

- architecture: sole runtime store plus descriptor/batch boundary settled;
- semantics: immutable typed access and semantic transaction rules settled;
- compatibility: stable-key v2 persistence, no CLR durable identity, v1 rejection settled;
- scope: M031/M032/M033/direct-M035 only, M034/new gameplay excluded;
- acceptance: PR-22 failure mode and corrected state are mechanically observable;
- validation: all seven shards must be regenerated and verifier remains aggregate authority;
- human review: none;
- constrained execution: resumable and graphics-free;
- baseline model: GPT-5.6 Luna can implement without inventing new project policy.

No unresolved material planning issue prevents `ready`.
