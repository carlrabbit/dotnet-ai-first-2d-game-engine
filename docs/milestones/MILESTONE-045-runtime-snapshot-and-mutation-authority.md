# Milestone 045 — Runtime Snapshot and Mutation Authority

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-broad |
| Baseline implementation model | GPT-5.6 Luna |
| Repository role | capability-provider |
| Profiles | artifact-first-agentic-authoring; runtime-tool; game-simulation |
| Maturity | implementation-ready; artifact-first |
| Validation | resumable-sharded, active Windows epoch |
| Human review | none |
| Execution prerequisite | current M039–M044 contracts remain passing; historical M013 remains immutable |

M045 closes the remaining M013 **runtime-foundation** gaps. M039 already established `EntityComponentWorld` as the sole authoritative component-value owner. M045 does not replace it; it completes the read/snapshot and mutation boundaries around it.

## Goal

Establish one coherent runtime interaction model:

```text
authoritative EntityComponentWorld
        |
        +--> immutable typed phase snapshot
        |        |
        |        +--> behaviors / queries / spatial resolution
        |
        +<-- staged typed runtime commands
                 |
                 +--> one validated transaction/commit boundary
                 +--> factual mutation/event evidence
```

At completion, behavior/domain/spatial evaluation reads one immutable typed snapshot; generic component access cannot silently bind to the wrong stable key; lifecycle plus component/provenance mutations can commit atomically; spatial resolvers emit proposals rather than mutate the live world; rejected evidence preserves caller tick/identity; and snapshot fingerprints are semantic/canonical.

## Primary Acceptance Question

> Can every runtime evaluator read the same immutable typed world view and can every accepted entity/component change cross one explicit validated runtime commit boundary without ambiguous component identity or compensating rollback?

## Problems Being Corrected

1. `EntityComponentSnapshot` is primarily serialized evidence rather than a typed read model.
2. `BehaviorSnapshot` carries only tick/fingerprint/entity IDs.
3. grid and continuous spatial resolvers hold the live world and call `world.Set()`.
4. `CommitBatch()` cannot atomically compose lifecycle, provenance, set/remove operations.
5. M014 instantiation compensates partial mutation by destroying the entity.
6. `SimulationWorld.CreateEntityWithComponent*` can leave partial runtime state when the component step rejects.
7. multiple stable IDs may share one CLR type while generic access silently uses the first registration.
8. rejected mutation evidence can record tick 0 and lose caller command identity.
9. generic snapshot fingerprints can depend on ordinary serializer/insertion ordering.

## Target Architecture

### Immutable typed runtime snapshot

Provide one detached phase-scoped snapshot with semantics equivalent to:

```text
Tick
Fingerprint
Exists(entityId)
EntityIds
TryGet<T>(entityId)
TryGetByTypeId(entityId, stableTypeId)
Query<T>()
Query<T1,T2>()
QueryByTypeId(stableTypeId)
ComponentsFor(entityId)
```

Rules:

- later live mutation cannot alter an earlier snapshot;
- returned mutable CLR values are defensive copies;
- entity/component/query ordering is ordinal and deterministic;
- all behaviors in one behavior phase receive the same snapshot instance or semantically identical immutable view captured at that phase boundary;
- no live mutation API is exposed through the snapshot.

### Behavior snapshot

`BehaviorSnapshot` exposes the immutable typed runtime snapshot rather than a weaker duplicate state model. Tick/fingerprint may remain convenient projections.

Behavior code can perform typed lookup/query without receiving `EntityComponentWorld`.

### Stable component identity and generic binding

Stable component type ID is semantic identity. CLR type is binding information.

Generic operations such as `Set<T>`, `TryGet<T>`, and `Query<T>` may resolve a CLR type only when exactly one registered stable component family binds that CLR type.

If several stable type IDs intentionally share one CLR boundary type:

- registration is permitted where infrastructure needs it;
- generic type-only access rejects/throws deterministically as ambiguous;
- callers use explicit stable-key/type-erased APIs.

"First registration wins" is forbidden. Registration order cannot change generic binding semantics.

### Canonical snapshot encoding

Each descriptor used for snapshot fingerprinting provides canonical semantic encoding. Ordinary `System.Text.Json` is acceptable for simple immutable record shapes whose property/order semantics are already canonical. Unordered dictionaries/sets must be normalized before fingerprinting.

Fingerprint excludes CLR assembly identity, paths, timestamps, PIDs, allocation/storage indexes, and unordered-container insertion order.

### Bounded runtime transaction

Extend the existing batch concept into one bounded transaction capable of staging:

```text
CreateEntity
DestroyEntity
SetProvenance
SetComponent
RemoveComponent
```

Required behavior:

1. validate every operation and precondition without changing live authority;
2. reject duplicate/conflicting operations deterministically;
3. stage the full result;
4. reject the whole transaction if any operation fails;
5. commit lifecycle, provenance and component authority as one visible boundary;
6. emit factual lifecycle/component success evidence only for committed operations;
7. rejected transactions emit rejection evidence only and leave the authoritative fingerprint unchanged.

This is not a general database transaction framework.

### Atomic construction/destruction

M014 authored instantiation and `SimulationWorld.CreateEntityWithComponent*` use the transaction where they require all-or-nothing creation.

A failing initial component/provenance step leaves:

```text
entity absent
components absent
provenance absent
no entity.created success event
no component-added success event
```

Create-then-destroy compensation is not atomicity proof.

A staged destroy removes all runtime components/provenance at commit and cannot coexist with contradictory post-destroy mutations in the same transaction.

### Evaluator → command boundary

Behavior modules emit intents. Domain/spatial resolution consumes immutable snapshot + authored/static data and returns:

```text
accepted/rejected domain resolution
+
zero or more proposed runtime mutations
```

Resolvers do not apply those mutations. An execution coordinator orders proposals deterministically and submits them to the runtime transaction/commit boundary.

`GridSpatialResolver` and `ContinuousKinematicSpatialResolver` therefore must not mutate a live `EntityComponentWorld` during resolution.

M046 owns continuous collision/outcome semantics; M045 preserves current spatial outcomes except where truthfulness requires correcting rejected application.

### Factual rejection evidence

Every attempted command/transaction preserves the caller-supplied stable identity and requested tick/phase.

Rejected evidence includes the failing diagnostic, emits no success event, and leaves state unchanged. Tick 0 fallback for later rejected operations is forbidden.

### Simulation composition

`SimulationWorld` retains semantic clock/events, activities, reservations, tombstones, regions and higher-level transactions. It composes runtime transactions; these semantics do not move into the ECS.

### Phase semantics

```text
capture S
→ evaluate all behaviors against S
→ collect intents/proposals
→ resolve/commit in deterministic phase order
→ capture S2 for next phase
```

No behavior observes another behavior's same-phase mutation.

## Scope

- typed immutable runtime snapshot;
- behavior integration;
- canonical snapshot fingerprint semantics;
- unambiguous generic component binding;
- lifecycle + provenance + heterogeneous component transaction;
- atomic construction/destruction where required;
- correct rejected-command evidence;
- grid/continuous mutation-boundary migration;
- M014 instantiation migration;
- SimulationWorld atomic construction migration;
- machine-derived M045 validation.

## Non-goals

Do not replace `EntityComponentWorld`, introduce archetype/sparse-set ECS, add parallel system scheduling, source-generated registries, reflection discovery, a general command bus/event-sourcing framework, or change M040–M044 semantics. Do not redesign continuous collision rules; that is M046. No human review.

## Resolved Decisions

1. `EntityComponentWorld` remains the sole component store.
2. One immutable typed runtime snapshot is the evaluator read boundary.
3. `BehaviorSnapshot` exposes that view rather than a weaker duplicate.
4. Generic CLR access requires an unambiguous stable-ID binding.
5. Same-CLR multi-ID descriptors require explicit stable-key access.
6. Snapshot fingerprints use canonical semantic descriptor encoding.
7. A bounded runtime transaction stages lifecycle, provenance and component set/remove before commit.
8. Atomic construction cannot use compensating destruction.
9. Spatial resolvers are read-only evaluators and emit mutation proposals.
10. Rejected evidence preserves actual tick and command/transaction identity.
11. `SimulationWorld` retains higher-level semantic authority.
12. Human review is none.

## Required Authority

Read after `AGENTS.md` and this milestone:

1. `docs/specs/entity-component-runtime-contract.md`
2. `docs/specs/runtime-snapshot-and-mutation-authority-contract.md`
3. `docs/decisions/ADR-0051-close-m031-with-typed-components-and-atomic-semantic-transactions.md`
4. `docs/decisions/ADR-0057-evaluation-reads-immutable-runtime-snapshots-and-mutation-commits-transactionally.md`
5. `docs/architecture/runtime-snapshot-and-mutation-architecture.md`
6. `docs/specs/simulation-world-and-semantic-foundation-contract.md`
7. `docs/engineering/command-contract.md`
8. `docs/engineering/validation-tiers.md`

Inspect M013/M014 and current M039–M044 consumers only as necessary. Historical M013 records remain immutable.

## Validation

Execution mode: `resumable-sharded`.

```text
artifacts/runtime/M045/
artifacts/validation/m045-smoke/
```

```powershell
pwsh ./eng/suite.ps1 m045-smoke --plan-json
pwsh ./eng/suite.ps1 m045-smoke --shard descriptor-identity-and-canonical-encoding
pwsh ./eng/suite.ps1 m045-smoke --shard immutable-typed-snapshot
pwsh ./eng/suite.ps1 m045-smoke --shard behavior-phase-snapshot
pwsh ./eng/suite.ps1 m045-smoke --shard lifecycle-component-transaction
pwsh ./eng/suite.ps1 m045-smoke --shard spatial-command-boundary
pwsh ./eng/suite.ps1 m045-smoke --shard rejected-mutation-evidence
pwsh ./eng/suite.ps1 m045-smoke --shard snapshot-determinism
pwsh ./eng/suite.ps1 m045-smoke --shard simulation-integration-regression
pwsh ./eng/suite.ps1 m045-smoke --shard evidence-integrity
pwsh ./eng/suite.ps1 m045-smoke --shard predecessor-regression
pwsh ./eng/suite.ps1 m045-smoke --verify
```

Then:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

Only current aggregate verification establishes success.

### Shard boundaries

`descriptor-identity-and-canonical-encoding`: stable IDs, duplicate rejection, ambiguous generic-binding rejection, explicit keyed access for aliases, registration-order independence, canonical unordered-value fingerprinting.

`immutable-typed-snapshot`: typed lookup/query, deterministic ordering, defensive detachment and type-erased/typed consistency.

`behavior-phase-snapshot`: multiple same-phase behaviors see the same immutable state and cannot observe same-phase commits until next phase.

`lifecycle-component-transaction`: atomic create + provenance + heterogeneous components; destroy cleanup; set/remove; failing initial component leaves no entity/success evidence.

`spatial-command-boundary`: grid/continuous resolve against snapshots, emit proposals, and mutate only through coordinator/runtime commit.

`rejected-mutation-evidence`: caller tick/ID and diagnostics survive rejection; no success event/state change.

`snapshot-determinism`: semantically equivalent worlds built in different insertion/registration orders have equal queries/fingerprints.

`simulation-integration-regression`: current SimulationWorld, M014 and representative M040–M044 consumers use the corrected boundary.

`evidence-integrity`: pass/fail comes from runtime state/snapshots/events, not artifact existence or producer booleans.

`predecessor-regression`: focused current M039–M044 runtime-authority verification remains passing.

## Completion Audit

Before `COMPLETE`, confirm typed immutable snapshots; same-phase behavior view; detached snapshots; ambiguous CLR binding rejection; canonical fingerprints; staged lifecycle/provenance/component transaction; no partial M014/SimulationWorld construction; proposal-only spatial resolvers; accurate rejected evidence; all shards/verifier; build/test/format/check; and untouched historical M013 records.

## Escalation

Return to planning only if implementation requires replacing ECS storage, changing one-authoritative-store semantics, materially changing M039–M044 contracts, introducing a general scheduler/command bus, materially changing continuous collision semantics, or adding human review.

Concrete interfaces, transaction structures, snapshot types, mutation proposal records and bounded migration mechanics are implementation-owned.

## Terminal Outcome

Terminate with exactly one:

```text
Milestone status: COMPLETE
```

or:

```text
Milestone status: BLOCKED
```

`AWAITING HUMAN REVIEW` does not apply.
