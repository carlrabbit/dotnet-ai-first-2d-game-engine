# Milestone 042 — Multi-Fidelity Equivalence, Continuation, and Long-Horizon Proof

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
| Execution prerequisites | M040 COMPLETE and M041 COMPLETE with current aggregate verifiers |

M042 is the third and final corrective milestone closing the incomplete historical M033 capability:

```text
M040 real independent detailed + abstract executors
  ↓
M041 transactional fidelity ownership + reconciliation
  ↓
M042 real mixed-fidelity orchestration + equivalence + continuation + long-horizon proof
```

M042 may be planned and marked `ready` before M040/M041 implementation completes. Implementation MUST NOT begin until both predecessor completion contracts are satisfied. Historical M033 remains historical and is not reopened.

## Goal

Demonstrate that the repaired multi-fidelity architecture behaves as one game simulation rather than several loosely similar implementations.

M042 combines the real M040 executors and M041 transition boundary into deterministic mixed-region runs and proves:

- genuinely different execution schedules are actually different;
- rule-equivalent semantics never diverge;
- approximation differences remain inside authored, switch-count-independent bounds;
- switching frequency does not create systematic gameplay advantage or penalty;
- mixed-fidelity saves resume in a separate process without skipping or duplicating semantics;
- long-horizon repeated switching does not accumulate stale ownership, queue, reservation, conservation, or continuation defects.

At M042 completion the repository may legitimately treat the old M033 multi-fidelity capability as functionally closed by M040–M042.

## Primary Acceptance Question

> Do different detailed/abstract execution schedules over the same authored semantic scenario remain semantically valid, boundedly equivalent, observer-neutral, deterministic within each schedule, and exactly resumable under the same schedule after fresh-process persistence?

## Preconditions

Before M042 implementation validation:

```powershell
pwsh ./eng/suite.ps1 m040-smoke --verify
pwsh ./eng/suite.ps1 m041-smoke --verify
```

must both pass against current repository fingerprints, and both predecessor completion audits must be `COMPLETE`.

M042 MUST use the real M040 executors and M041 reconciliation implementation.

## Target State

### One deterministic mixed-fidelity orchestrator

M042 completes one bounded headless orchestrator over:

- one `SimulationWorld`;
- multiple persistent independent regions;
- one M040 detailed executor for the currently detailed region;
- M040 abstract executors/scheduler continuation for abstract regions;
- the M041 paired fidelity transition coordinator;
- one semantic clock;
- one authored execution schedule;
- one target horizon.

The orchestrator is provider/test infrastructure, not a second gameplay authority.

### Deterministic next-boundary loop

Mixed execution advances to the earliest of:

```text
next authored fidelity-switch instant
next due abstract trigger
next detailed fixed-step boundary
target horizon
```

At the same semantic instant the required orchestration phase order is:

1. authored fidelity transition boundary;
2. abstract trigger delivery in deterministic scheduler order;
3. detailed fixed-step boundary for the resulting current detailed owner;
4. derived inspection/evidence.

This preserves M041's due-at-transition rule: an abstract trigger due at the switch instant but not already committed is fenced/reconciled by the transition rather than also delivered under the old ownership epoch.

Implementation may map this to existing phase/priority machinery, but the observable ordering is fixed.

### Execution schedule is authored control input

An M042 schedule is deterministic scenario/control input, not gameplay state. It has stable identity and fingerprint and is a pure function of semantic time/horizon.

For persistence continuation, the same schedule ID/fingerprint is re-supplied and validated. Switch instants are derivable from semantic time, so M042 does not require a mutable schedule cursor in gameplay persistence.

A mismatched schedule fingerprint rejects the continuation comparison.

### Canonical control compositions

M042 requires four genuinely distinct 30-day controls over the same authored three-region semantic scenario.

#### `abstract-control`

All three regions run through M040 abstract execution for the entire horizon. No fidelity coordinator or detailed fixed-step execution is used.

#### `periodically-switched`

Exactly one region is detailed at a time. Detailed ownership rotates:

```text
alpha → beta → gamma → alpha ...
```

every 24 semantic hours for 30 days.

#### `mostly-detailed`

One focus region receives most detailed exposure using a repeating six-day cycle:

```text
alpha detailed: 4 days
beta detailed: 1 day
gamma detailed: 1 day
```

The cycle repeats exactly five times over 30 days.

#### `detailed-reference`

Because M041 permits exactly one detailed region in a mixed world, detailed reference consists of three independent single-region controls. Each region starts from the exact corresponding canonical region state and runs for 30 days entirely through the M040 detailed executor.

The harness aggregates their semantic results. The canonical M042 regions have no cross-region hauling/transfer, so this is a valid bounded reference. It is validation composition, not a normal game mode.

### Control schedule distinctness gate

Before equivalence metrics are accepted, validation proves the required controls are different through at least:

- schedule fingerprint;
- detailed semantic-time exposure by region;
- abstract semantic-time exposure by region;
- fidelity-transition count;
- detailed-step count;
- abstract-trigger delivery count.

If required controls alias the same execution schedule/path, M042 fails before equivalence analysis.

### Canonical scenario

Canonical scenario ID:

```text
scenario.m042.multi-fidelity-equivalence-and-continuation
```

Regions:

```text
region.alpha
region.beta
region.gamma
```

They use the same shared work/logistics/needs semantics with region-specific stable IDs and deterministic detailed/abstract mapping. There is no cross-region resource or worker transfer.

Each region contains enough authored finite work, storage, and need-source authority for detailed/abstract work, carrying, storage, fixed needs, repeated interruptions, and transition-state coverage.

For observer-neutrality runs, ordinary work must remain non-exhausted through at least 90% of the 30-day horizon under the fastest completing control. This prevents workload exhaustion from making productivity neutrality vacuous. Do not add synthetic periodic replenishment solely for this proof; scale the finite authored workload instead unless existing explicit production semantics already apply.

## Equivalence Classes

### Rule-equivalent: zero tolerance

These remain exact/valid independent of schedule:

- identity/lifecycle semantics;
- equivalent-state work eligibility;
- equivalent-state priority/tie-break semantics;
- activity-stage legality;
- reservation/capacity legality;
- semantic command acceptance/rejection for equivalent state;
- fixed need thresholds/policy;
- resource conservation;
- no duplicate semantic completion;
- no terminal reservation leak;
- no stale old-epoch mutation;
- exactly one executor owner per active continuation;
- valid persistence references;
- no half-committed transition;
- no storage/inventory capacity violation.

No tolerance may be added to these predicates.

### Same-schedule determinism: exact

Same initial scenario + same schedule + same horizon must produce exact canonical fingerprints and equivalent ordered semantic evidence.

Same-schedule uninterrupted versus save/fresh-process-resume continuation is also exact.

### Bounded approximate outcomes

Approximation is permitted only for executor-dependent timing/continuation effects:

- travel/arrival instant;
- activity completion instant;
- interruption instant;
- blocked/retry duration;
- near-simultaneous arrival order;
- fixed-horizon completed-work/productivity totals;
- fixed-horizon need-satisfaction counts/timing.

## Authored Tolerance Policy

### Timing envelope

For a corresponding semantic transition:

```text
T_base =
    detailed fixed-step quantum
  + abstract scheduler timing quantum
  + M041 reconciliation mapping time-error bound
```

All terms are read from current runtime/scenario policy metadata before compared runs execute.

For blocked/retry boundaries:

```text
T_blocked = T_base + one declared retry quantum
```

For mandatory need interruption/satisfaction:

```text
T_need = T_base + one declared need-integration threshold quantum
```

If a term is exact in the current implementation, it contributes zero.

No term may be multiplied by fidelity-switch count. Concrete computed microsecond values are recorded before comparison.

### Corresponding completion timing

For work identified by the same stable semantic work key (region/family/target or equivalent stable identity):

```text
abs(completionInstantA - completionInstantB) <= applicable T
```

where both runs complete the work within the comparison window.

### Arrival ordering

If two corresponding contenders are separated by more than `2 * T_base` in both runs, their relative semantic arrival/completion order must agree.

Within the near-simultaneous window order may differ if all zero-tolerance rule/reservation semantics remain valid.

### Fixed-horizon boundary allowance

At an arbitrary target instant, one run may have semantic work in flight that another has just completed.

For metric `M`:

```text
abs(M_A - M_B)
<= boundaryAllowance(A, M) + boundaryAllowance(B, M)
```

`boundaryAllowance(run, M)` is the maximum remaining contribution of currently nonterminal authoritative activities at the target that can still affect `M`.

Examples:

- carrying may contribute carried quantity to future stored quantity;
- in-progress harvest may contribute at most its reserved/declared harvest quantity;
- active need activity may contribute one pending satisfaction occurrence.

Completed historical activities do not enlarge the allowance. There is no `switchCount * epsilon` term.

Conservation, capacity, reservation validity and duplicate completion remain exact.

## Observer Neutrality

Observer neutrality varies switching segmentation while holding total detailed exposure per region constant.

Run a 30-day three-region family where each region receives exactly ten days detailed exposure.

### Low frequency

```text
alpha 10d
beta 10d
gamma 10d
```

### Medium frequency

Round-robin detailed ownership every 24 hours for 30 days.

### High frequency

Round-robin detailed ownership every 6 hours for 30 days.

All use identical semantic initial state/policy.

Compare:

- resource/work completions;
- source/carried/stored quantities;
- need warnings/mandatory thresholds/satisfactions;
- idle duration;
- blocked/retry duration;
- reservation conflicts;
- failures/diagnostics;
- stale/cancelled triggers;
- transition count.

Required:

- zero-tolerance invariants pass;
- pairwise timing/productivity/need differences stay inside the same fixed envelopes;
- high-frequency switching receives no larger error budget;
- no systematic economic or need-safety advantage/penalty emerges solely from segmentation.

Every reported metric includes low/medium/high values and pairwise deltas. A conclusion such as `systematicEffect = none` is valid only if independently derived from these predicates.

## Mixed-Fidelity Fresh-Process Continuation

Required checkpoint classes:

```text
abstract-travel
abstract-carrying
immediately-after-materialization
detailed-carrying
immediately-after-abstraction
equal-time-trigger-and-switch-boundary
mandatory-need-interruption
```

For each checkpoint:

1. run canonical schedule uninterrupted to a declared target;
2. independently run to the checkpoint;
3. save only stable canonical authority;
4. terminate producer process;
5. start separate consumer process;
6. load save;
7. validate same schedule ID/fingerprint;
8. continue beyond checkpoint to the same target;
9. compare uninterrupted and resumed outcomes.

Within the same schedule the target comparison is exact, including:

- semantic world fingerprint;
- fidelity/ownership state;
- execution epochs;
- canonical scheduler continuation;
- detailed/abstract active continuation;
- activity/reservation state;
- resource/inventory/storage/need state;
- transition revision;
- required ordered semantic facts.

A child process that only deserializes/inspects is not proof.

## Thirty-Day Canonical Horizon

Canonical equivalence and observer-neutrality horizon is exactly 30 semantic days.

Fail on early termination, safety/event limit before horizon, observer workload exhausted before 90% horizon, missing control, missing checkpoint, or omitted required comparison.

## Long-Horizon Stress

After canonical equivalence passes, execute a separate deterministic stress campaign:

```text
horizon: at least 365 semantic days
regions: at least 5 independent persistent regions
successful paired fidelity switches: at least 1000
stable detailed-region count: exactly 1
```

This is stability/invariant proof, not tolerance discovery.

Required predicates:

- no conservation/capacity failure;
- no duplicate completion;
- no terminal reservation leak;
- no stale old-epoch success mutation;
- no half transition;
- no ownerless/dual-owned continuation;
- no unbounded accumulation of executable cancelled/stale triggers;
- no unbounded accumulation of obsolete detailed routes/continuations;
- queue/continuation state remains bounded by current workload plus documented retained diagnostics;
- declared periodic save/load checkpoints remain valid;
- full horizon completes without fabricated early success.

At least one same-seed/same-schedule rerun must produce the same canonical final fingerprint/evidence summary.

No new performance budget is introduced by M042 beyond completing the bounded campaign.

## Comparison Harness Independence

Run producers emit raw structured observations.

The comparer independently computes:

- schedule distinctness;
- invariant outcomes;
- conservation;
- timing deltas;
- boundary allowances;
- observer-neutrality deltas;
- continuation equality.

A scenario-produced field such as `zeroDivergence = true` or `observerNeutral = true` is not acceptance authority.

## Scope

- deterministic mixed-fidelity orchestration;
- distinct schedule/control harness;
- canonical three-region 30-day scenario;
- authored tolerance policy;
- rule/invariant equivalence comparer;
- observer-neutrality family;
- fresh-process checkpoint continuation;
- 365-day / 5-region / 1000-switch stability campaign;
- independent evidence/comparison;
- replacement/demotion of obsolete historical M033 equivalence artifacts as current authority.

## Non-goals

Do not:

- add new gameplay systems;
- change work/logistics/needs semantics merely to improve equivalence;
- weaken M040 executor separation;
- weaken M041 transition atomicity;
- add cross-region hauling;
- add multiple simultaneous detailed regions;
- add traits/skills/mood;
- add M034 infrastructure fidelity semantics;
- add rendering/animation/audio;
- add graphical human review;
- tune tolerances after observing failures;
- scale tolerance by switch count;
- use randomized schedules as completion authority;
- introduce multithreaded/distributed orchestration;
- optimize ECS/scheduler storage speculatively.

## Compatibility

M042 does not intentionally change:

```text
SimulationWorld v2
current M040 abstract continuation persistence
current M041 stable fidelity/transition persistence
```

Schedule identity/fingerprint is validation/control input, not gameplay save authority.

If correct stable continuation requires a persisted schema break, return to planning. Do not silently change compatibility in M042.

## Resolved Decisions

1. M042 begins only after M040 and M041 are COMPLETE.
2. Mixed orchestration uses one SimulationWorld and one semantic clock.
3. Same-instant orchestration order is switch, abstract trigger, detailed step, inspection.
4. Schedules are stable deterministic control input and are validated on resume.
5. Four canonical controls are genuinely distinct; detailed reference uses independent per-region detailed controls because canonical regions do not interact across regions.
6. Canonical horizon is 30 semantic days.
7. Rule-equivalent invariants have zero tolerance.
8. Same-schedule reruns and save/resume continuation require exact equality.
9. Timing tolerance derives from fixed execution/mapping/retry policy and never scales with switch count.
10. Fixed-horizon work/need differences use current in-flight boundary allowance only.
11. Observer-neutrality compares equal-exposure low/medium/high switching schedules.
12. Long-horizon stress is at least 365 days, 5 regions, and 1000 successful paired switches.
13. Comparison is independently calculated from raw observations.
14. Human review is none.

## Required Authority

Read after `AGENTS.md` and this milestone:

1. `docs/milestones/MILESTONE-040-shared-simulation-semantics-and-real-abstract-executor.md`
2. `docs/milestones/MILESTONE-041-fidelity-ownership-and-reconciliation.md`
3. `docs/specs/shared-work-logistics-and-needs-semantics-contract.md`
4. `docs/specs/detailed-grid-navigation-and-activity-execution-contract.md`
5. `docs/specs/discrete-event-simulation-contract.md`
6. `docs/specs/abstract-activity-and-travel-contract.md`
7. `docs/specs/region-fidelity-and-reconciliation-contract.md`
8. `docs/specs/multi-fidelity-equivalence-contract.md`
9. `docs/architecture/multi-fidelity-simulation-architecture.md`
10. `docs/decisions/ADR-0052-shared-simulation-semantics-are-executor-neutral.md`
11. `docs/decisions/ADR-0053-fidelity-switches-convert-executor-continuation-transactionally.md`
12. `docs/decisions/ADR-0054-multi-fidelity-equivalence-uses-distinct-controls-and-switch-count-independent-bounds.md`
13. `docs/scenarios/m042-multi-fidelity-equivalence-and-continuation.md`
14. `docs/specs/save-compatibility-and-recovery-contract.md`
15. `docs/engineering/command-contract.md`
16. `docs/engineering/validation-tiers.md`
17. `eng/platform-verification.json`
18. `docs/engineering/platform-verification.md`

Inspect live M040/M041 source/tests and historical M033/M035 evidence code only as needed.

Do not read `.guide-profile.json`, `.guide-sync/`, external guides, planning conversation, prompt templates or `docs/research/` during ordinary implementation.

## Acceptance Criteria

### Predecessor integrity
- current M040 verifier passes;
- current M041 verifier passes;
- M042 uses real predecessor implementations.

### Orchestrator
- deterministic boundary loop uses required same-instant ordering;
- one detailed owner in mixed controls;
- abstract regions continue autonomously;
- target horizon is honored exactly;
- no old-epoch work commits after switch.

### Distinct controls
- all four canonical controls execute;
- fingerprints/counters prove required controls differ;
- no required controls alias one implementation/run.

### Zero-tolerance invariants
- every rule-equivalent predicate passes for every required control/checkpoint;
- conservation/capacity/reservation/ownership failures are unconditional failures.

### Bounded timing/outcomes
- concrete timing envelopes are derived before comparison;
- corresponding timing deltas remain in bounds;
- near-simultaneous arrival-order rule is applied exactly;
- no tolerance scales with switch count;
- fixed-horizon differences use independently calculated current in-flight allowances only.

### Observer neutrality
- low/medium/high schedules give each region exactly ten days detailed exposure;
- ordinary work remains non-exhausted through at least 90% horizon;
- pairwise values/deltas are reported;
- zero-tolerance predicates pass;
- bounded metrics remain inside the same switch-count-independent envelopes.

### Determinism
- same-schedule reruns produce exact fingerprints for all canonical controls;
- long-horizon same-schedule rerun is exact.

### Persistence continuation
- every checkpoint uses separate producer/consumer OS processes;
- consumer advances beyond checkpoint;
- same schedule fingerprint is validated;
- resumed versus uninterrupted target state is exact;
- no duplicate/missed transition or semantic completion appears after resume.

### Long horizon
- at least 365 days;
- at least 5 persistent regions;
- at least 1000 successful paired switches;
- all stability predicates pass;
- obsolete queue/route/continuation authority does not accumulate without bound.

### Evidence integrity
- comparer independently computes acceptance from raw observations;
- self-asserted equivalence booleans cannot pass;
- missing/stale/incompatible control/checkpoint/receipt fails.

### Historical closure
- obsolete historical M033 equivalence evidence is no longer current completion authority;
- historical milestone/review records remain immutable;
- docs may state M040–M042 close the previously incomplete M033 capability only after M042 completes.

## Validation

Execution mode: `resumable-sharded`.

Receipt root:

```text
artifacts/validation/m042-smoke/
```

Evidence root:

```text
artifacts/simulation/M042/
```

Preconditions:

```powershell
pwsh ./eng/suite.ps1 m040-smoke --verify
pwsh ./eng/suite.ps1 m041-smoke --verify
```

Plan:

```powershell
pwsh ./eng/suite.ps1 m042-smoke --plan-json
```

Required shards:

```powershell
pwsh ./eng/suite.ps1 m042-smoke --shard mixed-orchestrator-and-control-distinctness
pwsh ./eng/suite.ps1 m042-smoke --shard zero-tolerance-invariants
pwsh ./eng/suite.ps1 m042-smoke --shard bounded-temporal-equivalence
pwsh ./eng/suite.ps1 m042-smoke --shard observer-neutrality
pwsh ./eng/suite.ps1 m042-smoke --shard mixed-fresh-process-continuation
pwsh ./eng/suite.ps1 m042-smoke --shard deterministic-reruns
pwsh ./eng/suite.ps1 m042-smoke --shard long-horizon-transition-stability
pwsh ./eng/suite.ps1 m042-smoke --shard evidence-integrity
pwsh ./eng/suite.ps1 m042-smoke --shard predecessor-regression

pwsh ./eng/suite.ps1 m042-smoke --verify
```

Only `--verify` over current fingerprinted receipts establishes aggregate M042 success.

### Standard validation

After M042 verifier:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

No graphics validation required.

## Human Review

Applicability: `none`.

Equivalence, determinism, observer neutrality, continuation and long-horizon invariants are mechanically decidable.

No `.review/` request is created.

## Direct Documentation Impact

This planning package establishes:

- M042 milestone;
- ADR-0054;
- corrected multi-fidelity equivalence contract;
- completed staged multi-fidelity architecture target;
- canonical M042 scenario.

Implementation updates directly contradicted command/artifact/scenario indexes and docs required by the completed implementation. No broad documentation synchronization.

## Deferred Documentation Synchronization

No new `.guide-sync/pending/` hint is required. Implementation must not read `.guide-sync/`.

## Constrained Execution

M042 is graphics-free but intentionally long-running. Use resumable shards.

The 365-day stress and its rerun belong entirely inside the dedicated shard. Partial output is not aggregate success.

Fresh-process child processes run as bounded foreground validation work. Detached/background execution, timeout inflation, and partial logs are not proof.

## Completion Audit

Continue implementation while any agent-resolvable gap remains, especially:

- control runs alias each other;
- orchestrator ordering is nondeterministic;
- abstract regions stop progressing;
- comparison relies on scenario-produced pass booleans;
- zero-tolerance predicates are softened;
- tolerance is tuned post hoc or grows with switch count;
- observer schedules have unequal detailed exposure;
- workload exhausts too early;
- save/load only deserializes rather than continues;
- any checkpoint is omitted;
- 365-day/5-region/1000-switch campaign is incomplete;
- stale queue/route/continuation authority grows without bound;
- same-schedule determinism fails;
- M040/M041 regress.

Terminate:

```text
Milestone status: COMPLETE
```

only when all acceptance criteria, current shard receipts, verifier, standard validation and documentation obligations pass.

Use:

```text
Milestone status: BLOCKED
```

only for unavailable external capability, unmet predecessor completion, or a newly discovered material planning decision.

`AWAITING HUMAN REVIEW` does not apply.

## Escalation Boundary

Return to planning if implementation requires:

- weakening M040 shared-semantics/executor separation;
- weakening M041 transition atomicity;
- changing the one-detailed-region invariant;
- changing SimulationWorld/fidelity persistence compatibility;
- adding cross-region gameplay to create the proof;
- new tolerance categories or switch-count-scaled tolerance;
- changing canonical control schedule semantics;
- introducing multithreaded/distributed orchestration;
- changing human-review policy.

Implementation owns concrete orchestrator types, schedule structures, raw evidence schemas, comparer implementation, metric storage, process fixtures, local performance refactoring and test organization.

## Baseline-Executability Audit

Ready for GPT-5.6 Luna, conditional only on explicit M040/M041 execution prerequisites.

Architecture, control schedules, orchestration ordering, equivalence classes, tolerance policy, observer-neutrality policy, persistence checkpoint set, long-horizon scale, acceptance, validation and human-review policy are settled.

Remaining choices are implementation mechanics. No material project decision is delegated to the executor.
