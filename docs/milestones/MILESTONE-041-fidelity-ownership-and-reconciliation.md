# Milestone 041 — Fidelity Ownership and Reconciliation

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
| Execution prerequisite | M040 COMPLETE with current `m040-smoke --verify` |

M041 is the second of three corrective milestones closing the incomplete historical M033 capability:

```text
M040 shared semantics + real independent executors
  ↓
M041 transactional fidelity ownership + reconciliation
  ↓
M042 mixed-fidelity equivalence + long-horizon continuation
```

Historical M033 remains historical and is not reopened.

## Goal

Make detailed ↔ abstract executor ownership transfer a real transactional engine capability.

A fidelity switch preserves authoritative gameplay semantics and converts only executor-owned continuation:

```text
shared semantic state
entities / resources / inventory / needs
activities / reservations / commands/events
              │ unchanged
              │
   ┌──────────┴──────────┐
   │                     │
detailed continuation  abstract continuation
grid/route/progress     node/edge/trigger/duration
   └──────── transaction ────────┘
```

At completion, supported in-progress work can cross either direction and continue under the target executor without semantic duplication, loss, ownership overlap, stale-work mutation, or partial transition state.

## Primary Acceptance Question

> Can a stable region/activity continuation move between the real M040 detailed and abstract executors through one transactional ownership boundary while shared gameplay semantics remain unchanged?

M041 does not prove long-horizon cross-mode equivalence. M042 owns that.

## Preconditions

Implementation begins only after:

```powershell
pwsh ./eng/suite.ps1 m040-smoke --verify
```

passes against current repository fingerprints and M040's completion audit is COMPLETE.

M041 uses the actual M040 executors and continuation contracts. It must not recreate synthetic M033 executors for transition testing.

## Target State

### Stable fidelity and ownership

Participating regions have stable fidelity `Detailed` or `Abstract`.

At every stable M041 boundary:

- exactly one region is detailed;
- every other participating region is abstract;
- every active executor continuation has exactly one current owner;
- detailed systems advance only detailed-owned continuation;
- abstract trigger handlers advance only abstract-owned continuation.

Fidelity and execution epoch/revision are persistent orchestration authority.

### Canonical paired switch

The bounded orchestration operation selects an abstract target to become detailed while the current detailed region becomes abstract:

```text
A Detailed + B Abstract
        ↓ one transaction
A Abstract + B Detailed
```

The swap commits atomically. No stable zero-detailed or dual-detailed state is valid.

### Semantic gameplay state is not converted

A switch alone MUST NOT change:

- entity identity/lifecycle;
- resource quantities;
- inventory quantities/capacity;
- storage contents/capacity;
- need levels/integration instant;
- designation meaning/revision;
- semantic activity kind/stage/status/revision;
- reservation identity/quantity/status/revision;
- semantic target/destination;
- gameplay factual command/event history.

A switch may change only fidelity/orchestration state, executor continuation, derived route/queue state, and transition diagnostics/events.

No harvest, deposit, need satisfaction, activity completion, or equivalent gameplay fact is emitted merely because fidelity changed.

### Transition handoff

Each source executor produces an immutable transition-only handoff sufficient for target preparation.

The concrete type is implementation-owned, but it represents as applicable:

```text
region
actor/activity + revision/stage
semantic target/destination
executor phase
source continuation revision
source location/progress
remaining interaction duration
mapping/spatial/graph guards
route/trigger references
transition causality
```

The handoff is staging data, not a second gameplay authority, and is not retained as independently mutable authority after commit.

### Transition lifecycle

The coordinator has one serialized lifecycle equivalent to:

```text
stable → preparing → reconciling → validating → committing → stable
```

Failure before commit returns to the complete prior stable state.

Only one transition may be active at once.

### Prepare and commit atomically

A paired switch:

1. identifies current detailed source and abstract target;
2. fences executor commits for both regions;
3. snapshots stable pre-transition ownership/continuation;
4. builds both source handoffs without live mutation;
5. prepares detailed→abstract continuation;
6. prepares abstract→detailed continuation;
7. stages scheduler cancellations/additions;
8. stages detailed route/position continuation replacement;
9. validates mapping, ownership, guards and references;
10. atomically commits both regions' fidelity/epoch/continuation/queue state;
11. emits transition evidence after commit;
12. releases the fence.

No live partial queue/route/ownership mutation is visible during preparation.

If scheduler or detailed continuation storage lacks bounded staging support, extend those subsystems. Do not simulate atomicity via live mutation plus compensating rollback.

### Execution epoch fencing

At transition start, both switching regions are fenced at one committed semantic boundary.

- old-epoch detailed continuation cannot commit after ownership changes;
- old-epoch abstract triggers cannot commit after ownership changes;
- due but undelivered triggers are invalidated/reconciled rather than delivered twice;
- already committed trigger outcomes remain facts and are not replayed.

A region/executor epoch or equivalent version guard enforces this even in a single-threaded runtime.

### Detailed → Abstract

Preserve shared semantic state and convert only detailed continuation.

For travel, map exact detailed position/route progress into a deterministic abstract node/edge/progress representation and schedule the guarded remaining travel transition.

Conversion must not:

- mark arrival solely because of switching;
- reset meaningful progress to the origin;
- jump to destination unless source position is already at the mapped destination boundary.

Mapping error must be bounded by declared mapping granularity and recorded.

For harvest/pickup/deposit/eat/drink/rest/retry, preserve semantic stage and convert current interaction progress to remaining semantic duration. The switch does not execute completion.

Carried inventory remains unchanged shared state.

Idle/blocked/interrupted states convert to deterministic abstract re-evaluation or retry continuation without synthetic completion.

### Abstract → Detailed

Preserve shared semantic state and invalidate old abstract triggers under the old epoch.

For travel, map abstract node/edge/progress to a deterministic valid detailed position, then rebuild a detailed route to the existing semantic destination.

A valid cell must satisfy current mapping, bounds, walkability and occupancy rules.

If the preferred cell is invalid, bounded deterministic repair may select another valid cell within the declared mapped area/segment. Repair cannot produce semantic arrival/completion, economic gain, or a different semantic destination.

If no valid materialization exists, transition rejects and prior abstract ownership remains intact.

For timed interactions, translate remaining scheduled duration into detailed progress without executing the completion command.

Carried inventory remains untouched.

Idle/blocked/interrupted states become deterministic detailed re-evaluation/continuation states.

### Spatial mapping authority

M041 requires explicit revisioned mapping between:

```text
abstract graph node/edge ↔ detailed region area/cell set
```

Mapping is reconciliation configuration/projection authority, not a duplicate gameplay world.

Equivalent state and mapping revision produce the same materialization. Mapping revision mismatch rejects/stales transition preparation.

### Reservations and semantic references

Reservations survive a successful switch unchanged unless ordinary gameplay semantics changed them before transition began.

A switch does not release/reacquire reservations merely to change executor.

Transition preparation validates actor, target, destination, activity and reservation references and rejects rather than repairing semantic authority implicitly.

### Activity-state coverage

Validation covers representative continuation categories:

```text
idle/no-work
travel-to-source
interaction/harvest-progress
carrying
travel-to-storage
deposit-progress
mandatory need activity
interrupted
blocked/retry
```

Both directions are exercised wherever the category can legally exist in both executors. Equivalent internal cases may share a mechanical proof, but coverage must be observation-derived.

### Failure and rollback

Bounded deterministic failure injection is required after preparation of:

- source handoff;
- target conversion/materialization;
- scheduler mutation staging;
- route/continuation staging;
- final validation before commit.

For every injected failure:

```text
post-failure stable authority == pre-transition stable authority
```

for semantic fingerprint, fidelity/owner/epoch, queue, detailed continuation, abstract continuation, activities and reservations.

Rollback is achieved by not committing staged changes.

### Persistence

Canonical save authority contains stable pre- or post-transition state only.

A save must never serialize `preparing`, `reconciling`, `validating` or partially committed transition state as a valid stable world.

If save is requested during an active transition, implementation may either complete/rollback foreground transition before capture or reject/defer save with a stable diagnostic. Half-transition persistence is forbidden.

M041 proves fresh-process continuation from:

1. a stable checkpoint immediately before a supported switch;
2. a stable checkpoint immediately after the switch.

After each load, the restored current executor must execute at least one real M040 continuation stage.

M042 owns broad mixed-fidelity long-horizon checkpoint equivalence.

## Scope

- stable region fidelity and execution epoch authority;
- atomic paired switch;
- transition-only immutable handoff;
- deterministic abstract↔detailed spatial mapping;
- detailed→abstract continuation conversion;
- abstract→detailed materialization/route reconstruction;
- interaction progress conversion;
- trigger invalidation/replacement;
- staged scheduler/route/ownership commit;
- bounded failure injection and rollback proof;
- stable pre/post-transition persistence;
- bounded repeated switching for stale continuation leakage.

## Non-goals

Do not implement or claim:

- long-horizon cross-mode equivalence;
- observer-neutrality thresholds;
- all-abstract/all-detailed comparison;
- 1,000+ switch stress as completion authority;
- multiple detailed regions;
- physical cross-region movement or hauling;
- graphical/animation continuity;
- graphics review;
- M034 infrastructure fidelity integration beyond regression;
- M035 campaign redesign;
- multithreaded transition execution;
- canonical save/resume of intentionally half-completed transition state.

## Resolved Decisions

1. M041 implementation requires M040 COMPLETE.
2. Fidelity switching changes executor continuation, not gameplay semantics.
3. The canonical switch is an atomic paired detailed-region swap.
4. Exactly one region is detailed at stable boundaries.
5. Immutable handoff data is transition staging, not gameplay authority.
6. Old source continuation is non-executable after commit.
7. Execution epochs fence late source work.
8. Due-but-undelivered triggers are invalidated/reconciled, never delivered twice.
9. Detailed→abstract travel maps deterministically and schedules remaining guarded continuation.
10. Abstract→detailed travel materializes deterministically and rebuilds route.
11. Timed interaction progress preserves remaining semantic duration.
12. Shared resources/inventory/needs/activity/reservation state is not mutated merely by switching.
13. Scheduler, route and ownership changes stage and commit together.
14. Compensating live rollback is rejected.
15. Only stable pre/post-transition states are canonical save authority.
16. M042 owns equivalence/observer-neutrality/long-horizon closure.
17. Human review is none.

## Required Authority

Read after `AGENTS.md` and this milestone:

1. `docs/milestones/MILESTONE-040-shared-simulation-semantics-and-real-abstract-executor.md`
2. `docs/specs/shared-work-logistics-and-needs-semantics-contract.md`
3. `docs/decisions/ADR-0052-shared-simulation-semantics-are-executor-neutral.md`
4. `docs/specs/simulation-world-and-semantic-foundation-contract.md`
5. `docs/specs/detailed-grid-navigation-and-activity-execution-contract.md`
6. `docs/specs/discrete-event-simulation-contract.md`
7. `docs/specs/abstract-activity-and-travel-contract.md`
8. `docs/specs/region-fidelity-and-reconciliation-contract.md`
9. `docs/architecture/multi-fidelity-simulation-architecture.md`
10. `docs/decisions/ADR-0053-fidelity-switches-convert-executor-continuation-transactionally.md`
11. `docs/specs/save-compatibility-and-recovery-contract.md`
12. `docs/engineering/command-contract.md`
13. `docs/engineering/validation-tiers.md`
14. `eng/platform-verification.json`
15. `docs/engineering/platform-verification.md`

Inspect live M040 executor source/tests and current historical M033 coordinator only as needed.

Ordinary implementation must not read `.guide-profile.json`, `.guide-sync/`, external guides, prompt templates, planning conversation or `docs/research/` as authority.

## Acceptance Criteria

### Predecessor
- current `m040-smoke --verify` passes before M041 validation;
- M041 uses real M040 detailed and abstract executors.

### Ownership
- exactly one detailed region at every stable state;
- each active continuation has one current owner;
- old-epoch detailed/abstract work cannot commit after switch;
- no stable zero-owner or dual-owner continuation.

### Semantic invariance
A successful switch produces no switch-caused change to resources, inventory, storage, needs, designations, semantic activity state/revision, reservations, or gameplay factual event history.

### Detailed→abstract
- travel converts to coherent abstract progress plus guarded next trigger;
- interaction progress converts to remaining duration without completion;
- carrying remains unchanged;
- old detailed continuation is not executable;
- abstract executor subsequently executes a real M040 stage.

### Abstract→detailed
- travel materializes deterministically to a valid detailed position;
- route rebuild targets the existing semantic destination;
- old abstract triggers cannot deliver;
- interaction remaining duration becomes detailed progress without completion;
- detailed executor subsequently executes a real M040 stage.

### Mapping
- stable mapping ID/revision is explicit;
- same state/mapping yields same materialization;
- invalid preferred cell uses deterministic bounded repair or rejects;
- no valid cell rejects without changing stable state;
- revision mismatch rejects/stales preparation.

### State matrix
- executed evidence covers idle, travel, interaction, carrying, deposit, need, interrupted and blocked/retry categories.

### Atomicity
- queue changes, route/continuation changes and owner/epoch changes are staged before commit;
- observers cannot see half-committed transition authority.

### Rollback
Every injected failure boundary leaves semantic fingerprint, owner/fidelity/epoch, queue, continuation, activity and reservation state unchanged.

### Stale/same-instant behavior
- due-but-undelivered trigger cannot also complete after materialization;
- old detailed step cannot commit after abstraction;
- duplicate transition request is deterministic/idempotent or explicitly rejected;
- bounded rapid switching does not accumulate executable stale routes/triggers.

### Persistence
- half transition is never canonical stable save state;
- pre-switch fresh-process load can continue and switch;
- post-switch fresh-process load restores new owner and executes next target-executor stage;
- restored queue/route/reference state validates.

### Evidence integrity
- transition evidence derives from before/handoff/after observations;
- route rebuild, trigger invalidation, materialization and rollback claims are measured;
- stale/missing receipts fail verifier;
- M041 does not emit M042 equivalence conclusions.

## Validation

Execution mode: `resumable-sharded`.

Receipt root:

```text
artifacts/validation/m041-smoke/
```

Evidence root:

```text
artifacts/simulation/M041/
```

Precondition:

```powershell
pwsh ./eng/suite.ps1 m040-smoke --verify
```

Plan and shards:

```powershell
pwsh ./eng/suite.ps1 m041-smoke --plan-json

pwsh ./eng/suite.ps1 m041-smoke --shard ownership-and-epoch-fencing
pwsh ./eng/suite.ps1 m041-smoke --shard detailed-to-abstract
pwsh ./eng/suite.ps1 m041-smoke --shard abstract-to-detailed
pwsh ./eng/suite.ps1 m041-smoke --shard transition-state-matrix
pwsh ./eng/suite.ps1 m041-smoke --shard scheduler-route-atomicity
pwsh ./eng/suite.ps1 m041-smoke --shard rollback-fault-boundaries
pwsh ./eng/suite.ps1 m041-smoke --shard transition-persistence
pwsh ./eng/suite.ps1 m041-smoke --shard stale-and-rapid-switch
pwsh ./eng/suite.ps1 m041-smoke --shard m040-regression

pwsh ./eng/suite.ps1 m041-smoke --verify
```

Only `--verify` over current fingerprinted receipts establishes aggregate M041 success.

Shard boundaries are deliberately separate:

- `ownership-and-epoch-fencing`: one-owner invariants and late-work fencing;
- `detailed-to-abstract`: real detailed continuation conversion;
- `abstract-to-detailed`: real materialization and route reconstruction;
- `transition-state-matrix`: representative in-progress state coverage;
- `scheduler-route-atomicity`: staged queue/route/ownership transaction;
- `rollback-fault-boundaries`: failure-after-preparation equality proof;
- `transition-persistence`: separate-process stable pre/post-switch continuation;
- `stale-and-rapid-switch`: same-instant and bounded stale-leak checks;
- `m040-regression`: predecessor verifier plus focused executor regression.

Then run:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

No graphics validation is required.

## Human Review

Applicability: `none`.

Ownership, mapping, continuation conversion, rollback, persistence and stale-work behavior are mechanically decidable.

No `.review/` request is created.

## Direct Documentation Impact

This planning package establishes M041 milestone authority, ADR-0053, the corrected reconciliation contract, and staged multi-fidelity architecture.

Implementation updates directly contradicted command/artifact/index documentation as required by the actual completed implementation. No broad unrelated synchronization.

## Deferred Documentation Synchronization

No new `.guide-sync/pending/` hint is required.

## Constrained Execution

M041 is graphics-free and uses resumable validation. Fault cases and fresh-process children run as bounded foreground shard work. Detached/background output is not proof.

## Completion Audit

Continue implementation while any agent-resolvable gap remains, especially if transition still flips metadata without converting real M040 continuation, semantic state changes merely because fidelity changed, old route/trigger remains executable, queue/route/ownership mutate live before complete validation, compensation is used as rollback, progress resets/completes during switch, save can persist a half transition, or evidence asserts unmeasured claims.

Terminate `Milestone status: COMPLETE` only after every acceptance criterion, all current receipts, verifier, repository-standard validation and documentation obligations pass.

Use `Milestone status: BLOCKED` only for unavailable external capability, unmet M040 predecessor completion, or a newly discovered material planning decision.

`AWAITING HUMAN REVIEW` does not apply.

## Escalation Boundary

Return to planning only if implementation requires changing M040's shared-semantics/executor split, mutating gameplay semantics to switch fidelity, permitting multiple detailed regions, persisting half transitions as normal save authority, introducing multithreaded transition execution, defining M042 equivalence policy early, changing current compatibility policy, weakening atomic rollback/ownership, or changing human-review policy.

Concrete handoff types, mapping structures, staging mechanics, transaction object layout, route/trigger adapter APIs, fault hooks and test organization remain implementation-owned.

## Baseline-Executability Audit

Ready for GPT-5.6 Luna, conditional only on the explicit M040 execution prerequisite.

Architecture, semantics, persistence boundary, scope, rollback behavior, acceptance, validation and human-review policy are settled. Remaining choices are local implementation mechanics.
