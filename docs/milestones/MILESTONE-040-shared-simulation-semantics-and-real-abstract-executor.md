# Milestone 040 — Shared Simulation Semantics and Real Abstract Executor

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

M040 is the first of three corrective milestones closing the incomplete historical M033 capability:

```text
M040 shared semantics + real abstract executor
M041 fidelity ownership + reconciliation
M042 mixed-fidelity equivalence + continuation
```

Historical M033 remains historical and is not reopened.

## Goal

Establish one executor-neutral work/logistics/needs semantic model and two genuinely different execution strategies over it:

```text
                  shared semantic authority
             work / selection / assignment
            logistics / needs / activities
              reservations / commands
                       │
           ┌───────────┴───────────┐
           │                       │
    detailed executor        abstract executor
      grid + fixed step       DES + durations
```

M040 ends when the same authoritative semantic world can run either through the existing detailed executor or through an independent discrete-event abstract executor, without fidelity switching.

## Context

The M033 audit found:

- the scheduler is a useful foundation;
- M033 created duplicate worker/resource proof components instead of using the M032 semantic model;
- "detailed" and "abstract" both called the same synthetic `CompleteCycle(...)`;
- one daily trigger could create/activate/complete an activity;
- duration-model evidence did not drive execution;
- needs did not materially drive abstract behavior;
- several M033 artifacts asserted unmeasured reconciliation/equivalence claims.

M039 already corrected the lower runtime boundary: typed ECS-owned components, immutable/read-only component values, atomic semantic transactions, real event causality and executable persistence classification. M040 builds on that and does not revisit it.

## Target State

### Shared executor-neutral semantics

The following rules are shared project authority and do not branch on executor identity:

- designations;
- work-opportunity derivation;
- worker eligibility and deterministic selection;
- assignment/revalidation;
- activity legality;
- reservations and capacity;
- harvesting;
- inventory;
- pickup/carry/deposit;
- storage acceptance/capacity;
- fixed food/water/comfort needs;
- interruption/resumption;
- semantic commands/events;
- conservation and semantic diagnostics.

Executor strategy may supply typed reachability/cost and continuation inputs, but shared rules do not call a detailed or abstract executor directly.

Implementation may refactor existing M032 code into services/modules/types. Package/class layout is implementation-owned.

### One gameplay component model

Detailed and abstract execution use the same authoritative typed component families and SimulationWorld semantic state.

Do not preserve an independent M033/M040 worker/resource/storage/need gameplay model.

Existing M033 proof-only duplicate component types may be removed or retained only as non-authoritative evidence DTOs.

### Detailed executor

Detailed execution continues to own:

- grid position interpretation;
- deterministic detailed pathfinding;
- interaction cells;
- fixed-step movement;
- detailed transient route/progress;
- route invalidation/rebuild.

It reaches semantic boundaries and issues shared semantic commands.

M040 does not redesign detailed pathfinding or component granularity.

### Abstract executor

Abstract execution owns only executor continuation:

- abstract node/edge location;
- coarse route summary;
- typed deterministic duration calculation;
- next scheduled transition;
- lazy need checkpoints;
- revision-guarded abstract continuation.

It does not:

- use detailed grid pathfinding for ordinary travel;
- simulate frame movement;
- mutate gameplay outside shared semantic commands;
- script a whole workday in one trigger.

### Scheduler

Retain the existing deterministic `DiscreteEventScheduler` direction.

Ordering remains:

```text
due semantic instant
priority class
stable sequence
stable trigger ID
```

Triggers remain future inputs rather than factual domain events.

### One meaningful transition at a time

Abstract execution schedules the next meaningful semantic boundary only, for example:

```text
travel -> arrival
harvest-start -> harvest-complete
pickup-start -> pickup-complete
carry-travel -> storage-arrival
deposit-start -> deposit-complete
need-warning -> mandatory
need-satisfaction -> satisfied
retry -> re-evaluate
```

After delivery, re-read committed semantic state and plan the next transition.

The old pattern below is explicitly invalid as current abstract execution:

```text
family = Families[(day - 1) % Families.Length]
create activity
activate immediately
complete immediately
```

### Trigger guards

Before delivery can issue a shared command, revalidate applicable:

- static region/executor ownership;
- actor lifecycle;
- target/destination lifecycle;
- activity identity/revision/stage;
- reservation state/revision;
- subject/storage revision and capacity;
- graph revision;
- need revision;
- semantic command preconditions.

Stale, cancelled or duplicate delivery performs no factual success mutation.

Guard fields must not remain descriptive metadata only.

### Abstract travel and duration

Abstract travel uses a deterministic coarse graph independent of detailed pathfinding.

Support bounded multi-edge routes with stable nodes/edges, integer cost, access/revision and declared carrying/movement modifiers.

Typed deterministic duration policy drives actual scheduled due instants for:

- travel;
- harvest;
- pickup;
- deposit;
- eat;
- drink;
- rest;
- bounded retry.

Duration-model artifact text alone is not proof.

### Real abstract logistics

A bounded abstract harvest/haul/deposit flow must execute as multiple scheduled transitions:

```text
derive opportunity
→ select worker
→ assign/reserve
→ abstract travel to source
→ harvest command
→ inventory contains resource
→ abstract carry travel
→ storage arrival
→ deposit command
→ storage contains resource
→ terminal cleanup
```

Required invariants:

- integer conservation;
- no double completion;
- capacity respected;
- reservations do not create quantity;
- terminal activity has no active reservations;
- stale/rejected transitions do not mutate success state.

### Fixed needs

Food, water and comfort use the shared fixed need policy.

Abstract execution lazily integrates need state over semantic time and schedules warning/mandatory/satisfaction thresholds.

At least one M040 proof must show:

```text
ordinary work
→ mandatory need
→ legal interruption
→ need satisfaction
→ work re-evaluation
```

The need threshold must change actual execution behavior.

### Static execution mode

A bounded M040 run is created as detailed or abstract and remains in that mode for the run.

M040 MUST NOT implement or validate:

- detailed -> abstract conversion;
- abstract -> detailed conversion;
- materialization;
- route/trigger conversion at a switch;
- mixed-fidelity reconciliation.

Those are M041.

### Persistence and fresh-process continuation

Retain SimulationWorld v2 and current M033 scheduler/multi-fidelity v2 compatibility baseline.

M040 must prove abstract-only continuation:

```text
run to checkpoint
→ save world + scheduler + abstract continuation
→ producer process exits
→ separate consumer process loads
→ consumer advances beyond checkpoint to target
```

Compare with uninterrupted abstract execution from the same initial state.

Required exact equality at the common target includes authoritative semantic fingerprint and relevant resource/inventory/storage/need/activity/reservation state.

A child process that only deserializes and reports queue count does not satisfy this criterion.

### Rule parity, not equivalence

M040 requires same-state rule parity:

- same opportunities;
- same eligibility;
- same selection ordering;
- same reservation/capacity legality;
- same command legality;
- same need priority/interruption decision.

M040 does not require detailed and abstract timelines or final fingerprints to match. Cross-mode equivalence belongs to M042.

### Evidence integrity

Completion evidence is observation-derived.

Validation must fail if:

- abstract execution calls the detailed path planner in its ordinary travel path;
- duplicate abstract gameplay components replace shared semantics;
- the abstract run can complete without real stage transitions;
- needs do not affect execution;
- stale triggers mutate;
- requested horizon is ignored;
- fresh-process load does not continue;
- a passing artifact claim is a constant rather than an observed predicate.

## Scope

- establish reusable executor-neutral work/logistics/needs semantics;
- refactor current M032 semantics as required without changing intended M032 behavior;
- retain/harden the deterministic scheduler;
- replace synthetic M033 cycle logic with real stage-based DES execution;
- use shared authoritative typed components and commands;
- add abstract graph travel and typed durations;
- add lazy need threshold execution;
- add guarded next-transition planning;
- add abstract-only fresh-process continuation;
- add failure-capable M040 validation.

## Non-goals

Do not implement:

- fidelity switching/reconciliation;
- mixed-fidelity orchestrator;
- cross-mode equivalence tolerances;
- observer-neutrality analysis;
- M035 scale rework;
- cross-region hauling;
- M034 environmental infrastructure;
- rendering/UI/animation/audio;
- graphics review;
- ECS redesign;
- multithreaded/distributed scheduling;
- dynamic plugins;
- personality/skills/health/combat/weather/ecology.

## Resolved Decisions

1. Shared work/logistics/needs semantics are executor-neutral.
2. Detailed and abstract execution share the same authoritative typed gameplay state and semantic commands.
3. Executors own continuation mechanics only.
4. Abstract ordinary travel never calls detailed grid pathfinding.
5. Existing deterministic scheduler is retained.
6. Abstract execution schedules one next meaningful transition at a time.
7. Synthetic daily-cycle/family rotation is removed as abstract execution authority.
8. Duplicate M033 gameplay component families do not remain a second game model.
9. Need thresholds drive real abstract execution.
10. Trigger guard metadata is enforced.
11. M040 execution mode is static for a run.
12. SimulationWorld v2 and current scheduler/multi-fidelity v2 baseline remain.
13. Fresh-process proof must advance beyond checkpoint.
14. M041 owns transition/reconciliation.
15. M042 owns equivalence/observer-neutrality.
16. Human review is none.

## Required Authority

Read after `AGENTS.md` and this milestone:

1. `docs/specs/simulation-world-and-semantic-foundation-contract.md`
2. `docs/specs/entity-component-runtime-contract.md`
3. `docs/decisions/ADR-0051-close-m031-with-typed-components-and-atomic-semantic-transactions.md`
4. `docs/specs/shared-work-logistics-and-needs-semantics-contract.md`
5. `docs/specs/autonomous-work-and-detailed-logistics-contract.md`
6. `docs/specs/detailed-grid-navigation-and-activity-execution-contract.md`
7. `docs/architecture/autonomous-detailed-region-execution-architecture.md`
8. `docs/specs/discrete-event-simulation-contract.md`
9. `docs/specs/abstract-activity-and-travel-contract.md`
10. `docs/architecture/multi-fidelity-simulation-architecture.md`
11. `docs/decisions/ADR-0052-shared-simulation-semantics-are-executor-neutral.md`
12. `docs/specs/save-compatibility-and-recovery-contract.md`
13. `docs/engineering/command-contract.md`
14. `docs/engineering/validation-tiers.md`
15. `eng/platform-verification.json`
16. `docs/engineering/platform-verification.md`

Inspect current M032/M033 source/tests and M039 closure tests as needed.

Do not read `.guide-profile.json`, `.guide-sync/`, the external guide repository, prompt templates, planning conversation or `docs/research/` during ordinary implementation.

## Acceptance Criteria

### Shared semantics
- one authoritative implementation is used by both executor compositions;
- same semantic state/input produces the same rule decisions independent of executor;
- shared-rule code does not branch on executor identity;
- no separate abstract worker/resource/storage/need gameplay model exists.

### Detailed regression
- M032 remains a genuine detailed grid/fixed-step executor;
- current M032 semantic outcomes and required regression pass.

### Abstract executor
- abstract execution uses scheduler-driven semantic stages;
- harvest/haul/deposit requires multiple due transitions;
- source -> inventory -> storage uses shared semantic commands;
- abstract ordinary travel does not call detailed pathfinding;
- each completed trigger plans from newly committed state.

### Guards
- equal-time order deterministic;
- cancellation inspectable;
- stale activity/entity/subject/graph/need revisions cannot mutate success state;
- duplicate delivery cannot duplicate completion;
- unknown trigger explicit failure;
- safety limits explicit.

### Travel/duration
- bounded multi-edge abstract route works deterministically;
- carrying/access/revision inputs affect route/duration by explicit policy;
- typed durations drive actual due instants.

### Needs
- need state is lazily integrated from semantic time;
- mandatory threshold changes execution;
- interruption/satisfaction/resumption follow shared rules;
- no per-frame abstract need loop is required.

### Persistence
- separate producer/consumer OS processes;
- consumer advances beyond checkpoint;
- uninterrupted and resumed abstract runs reach the same canonical target semantic state;
- malformed continuation rejects transactionally;
- requested run horizon is honored.

### Executor separation
- tests mechanically distinguish detailed and abstract implementations;
- neither executor calls the other as its implementation;
- both use the same semantic commands at equivalent semantic boundaries.

### Evidence
- pass predicates derive from executed observations;
- stale/missing/incompatible receipts fail verification;
- no M041/M042 claim is synthesized.

## Validation

Execution mode: `resumable-sharded`.

Receipt root:

```text
artifacts/validation/m040-smoke/
```

Evidence root:

```text
artifacts/simulation/M040/
```

Run:

```powershell
pwsh ./eng/suite.ps1 m040-smoke --plan-json

pwsh ./eng/suite.ps1 m040-smoke --shard shared-semantics
pwsh ./eng/suite.ps1 m040-smoke --shard abstract-scheduler-guards
pwsh ./eng/suite.ps1 m040-smoke --shard abstract-work-logistics
pwsh ./eng/suite.ps1 m040-smoke --shard abstract-needs
pwsh ./eng/suite.ps1 m040-smoke --shard abstract-travel-duration
pwsh ./eng/suite.ps1 m040-smoke --shard abstract-persistence-continuation
pwsh ./eng/suite.ps1 m040-smoke --shard executor-separation
pwsh ./eng/suite.ps1 m040-smoke --shard detailed-regression

pwsh ./eng/suite.ps1 m040-smoke --verify
```

Only `--verify` over current fingerprinted receipts establishes aggregate M040 success.

Then:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

No graphics validation is required.

## Human Review

Applicability: `none`.

All M040 completion predicates are mechanically decidable.

No `.review/` request is created.

## Direct Documentation Impact

This planning package adds:

- M040 milestone authority;
- ADR-0052;
- shared executor-neutral semantic contract;
- updated abstract execution contract.

Implementation must update directly contradicted active indexes, architecture/command/artifact docs as required by the completed implementation.

No broad unrelated documentation synchronization.

## Constrained Execution

Use resumable shards. Each shard runs foreground and writes a passing receipt only after its complete evidence succeeds.

Fresh-process child execution is allowed inside the bounded persistence shard. Detached/background work is not proof.

Partial child output never establishes success.

## Completion Audit

Continue implementation while any agent-resolvable gap remains, especially:

- duplicate detailed/abstract rules;
- old synthetic daily-cycle abstract execution;
- abstract calls detailed executor/pathfinding;
- duplicate abstract gameplay model;
- duration evidence not driving queue;
- needs not affecting execution;
- stale trigger mutation;
- fresh-process load without continuation;
- ignored requested horizon;
- M032 regression;
- fabricated evidence.

Terminate `Milestone status: COMPLETE` only after every acceptance criterion, all current receipts, verifier, repository-standard validation and direct documentation obligations pass.

Use `Milestone status: BLOCKED` only for external capability or a new material project decision.

`AWAITING HUMAN REVIEW` does not apply.

## Escalation Boundary

Return to planning only if completion requires changing a material decision above, including:

- changing the M039 runtime authority;
- preserving a separate abstract gameplay model;
- allowing shared rules to branch on executor;
- replacing the scheduler architecture;
- materially changing M032 gameplay semantics;
- implementing fidelity transitions in M040;
- defining cross-mode equivalence policy now;
- changing compatibility or human-review policy.

Local API/type/package/test/artifact mechanics remain implementation-owned.

## Baseline-Executability Audit

Ready for GPT-5.6 Luna.

Architecture, semantics, compatibility, scope, acceptance, validation and human-review policy are settled. Remaining decisions are implementation mechanics. No stronger model is required to resolve project-level uncertainty.
