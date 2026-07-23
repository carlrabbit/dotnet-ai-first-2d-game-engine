# Milestone 035 — Scale, Stability, and Heavy Internal Testing Readiness

## Goal

Establish that the M031–M034 simulation game and its reusable engine capabilities are fit for sustained, failure-oriented internal testing rather than only bounded milestone demonstrations.

```text
implemented simulation and game loop
→ measured scale envelopes
→ deterministic stress and soak campaigns
→ fault injection and recovery
→ save compatibility and corruption handling
→ reproduction bundles and tester workflows
→ readiness evidence and blocking review
```

Primary acceptance question:

> Can internal testers run, save, resume, stress, break, diagnose, and reliably reproduce a multi-region settlement over long sessions and long simulated horizons without hidden corruption, unbounded degradation, or dependence on implementation knowledge?

M035 is a hardening and readiness milestone. It must not add a major gameplay subsystem. New code is justified only where required to measure, diagnose, recover, or keep the existing M031–M034 game loop within declared readiness budgets.

## Repository role and maturity assumptions

```text
repository role: capability-provider
bounded dogfood: hardened M034 settlement used as the reference internal-test product
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation, UI/component
maturity before milestone: implementation-ready, artifact-first, feature-complete for bounded internal play
maturity after acceptance: heavy-internal-testing-ready within the declared support envelope
execution mode: ai-executed-broad
```

M031 through M034 are complete and authoritative. The repository provides persistent multi-region simulation, detailed and abstract execution, autonomous work/logistics, environmental infrastructure, operations projections, persistence, graphical dogfood, and milestone-scoped review evidence.

M035 is mixed provider/dogfood work:

- provider responsibility: reusable performance measurement, invariant monitoring, fault injection, recovery, compatibility, reproduction, soak, and readiness capabilities;
- bounded dogfood responsibility: prove those capabilities against the M034 three-region settlement and declared larger stress fixtures.

M030 remains deferred. M035 does not reopen asset integration or presentation-content scope.

## Execution mode

`ai-executed-broad`

Implement as six complete hardening transformations:

1. define supported scale envelopes, semantic performance budgets, and reproducible benchmark fixtures;
2. add continuous invariant monitoring, deadlock/livelock detection, queue/reservation health, and bounded self-diagnostics;
3. add deterministic soak, stress, fidelity-switch, persistence-cycle, and fault-injection campaigns;
4. harden save schema compatibility, atomic recovery, corruption diagnostics, migration boundaries, and retained reference saves;
5. add reproduction bundles, tester session manifests, crash/invariant evidence, triage commands, and an internal-test runbook;
6. optimize only measured bottlenecks, execute the full readiness campaign, and complete the blocking readiness review.

## Scope

### Supported readiness envelope

M035 must declare and validate a minimum supported internal-testing envelope.

At minimum:

```text
persistent regions: 5
simultaneously detailed regions: 1
workers: 50 total, at least 10 in the detailed region
runtime entities: at least 1,000 authoritative entities
construction/infrastructure entities and plans: at least 150 combined
work opportunities: at least 500 generated/evaluated per representative cycle
active activities/reservations: at least 100 combined
scheduled triggers: at least 10,000 peak queue entries in a stress fixture
accelerated horizon: at least 365 simulated days
interactive session: at least 4 continuous real hours
fidelity transitions: at least 1,000 in a campaign
save/load cycles: at least 250 in a campaign
```

These are minimum proof sizes, not universal engine maxima. The implementation may exceed them.

The final support envelope must state:

- fixture topology;
- population and entity counts;
- host/runtime classification;
- graphical and headless modes;
- target budgets;
- known exclusions;
- evidence fingerprints.

### Performance and capacity budgets

Define budgets for semantic operations rather than vague “fast enough” statements.

Required measurements:

- detailed fixed-step duration percentiles;
- work-opportunity derivation and worker-selection duration;
- grid pathfinding requests, expanded nodes, and percentiles;
- abstract events processed per real second;
- queue insertion/delivery/stale-event ratios;
- region materialization and abstraction latency;
- operations-projection generation latency;
- save and load duration and artifact size;
- memory working set and managed allocations;
- entity/component/activity/reservation counts;
- frame-time percentiles in the supported graphical fixture;
- sustained throughput trend over time;
- post-load and post-transition recovery latency.

Budgets must distinguish:

```text
blocking semantic budget
advisory same-machine performance budget
trend/regression budget
```

Semantic correctness always overrides timing.

### Baseline and regression comparison

Provide canonical benchmark fixtures and comparison output.

A performance comparison must include:

- baseline identifier and provenance;
- current identifier;
- comparable host/runtime classification;
- metric definition;
- sample count;
- median and tail values where meaningful;
- absolute and percentage difference;
- allowed regression threshold;
- pass, fail, or not-comparable status;
- diagnostic reason for non-comparable results.

Do not silently replace a baseline to make a regression pass.

Baseline promotion must be an explicit reviewed operation or an M035-approved initial baseline creation.

### Continuous invariant monitoring

Add a bounded invariant monitor usable during normal scenarios, stress runs, and graphical play.

Required monitored invariants:

- unique stable identity;
- valid entity lifecycle and region ownership;
- one executor owner per region/activity;
- no stale-trigger authoritative mutation;
- reservation quantity/capacity bounds;
- no reservation owned by completed/cancelled activity;
- no duplicate semantic completion;
- resource and environmental-flow conservation;
- storage and infrastructure capacity bounds;
- valid construction/crop/condition state;
- deterministic queue order;
- no detailed update outside the detailed region;
- persistence referential integrity;
- alert cause integrity;
- no half-committed fidelity transition.

Monitor modes:

```text
off
checkpoint
continuous-bounded
failure-only artifacting
```

The monitor must not introduce nondeterministic mutation or become a hidden repair mechanism.

### Deadlock, livelock, and starvation detection

Detect simulation states that remain technically running but cannot make meaningful progress.

Required classifications:

- no eligible worker;
- unreachable target or destination;
- reservation cycle or leak;
- repeated identical failed selection;
- repeated same-state route replan;
- repeated same-instant trigger loop;
- activity with no owning executor;
- activity with no progress beyond a declared duration;
- blocked construction or maintenance with satisfiable prerequisites not being scheduled;
- critical need starvation despite available reachable supply;
- alert that remains active without changing causal state.

Detection must provide:

- stable diagnostic code;
- affected IDs;
- first and current instant;
- recent causal history;
- relevant candidate/reservation/route/queue state;
- whether the state is valid shortage, recoverable block, or engine defect;
- suggested triage command.

Automatic gameplay repair is out of scope unless an existing semantic rule already defines recovery.

### Deterministic stress campaigns

Provide authored campaign families:

```text
population-and-entity scale
work-selection contention
pathfinding and occupancy stress
abstract queue and stale-trigger stress
fidelity transition churn
construction/infrastructure load
resource-shortage and recovery
maintenance failure cascades within M034 rules
save/load cycling
operations-projection stress
graphical long-session soak
```

Campaigns use stable seeds, authored inputs, bounded safety limits, and structured receipts.

The campaign harness must support:

- plan generation;
- individual case/shard execution;
- resume after process termination;
- aggregate verification;
- deterministic rerun comparison;
- minimization or bounded reduction metadata for failures.

### Fault injection

Add deterministic fault-injection points at approved infrastructure boundaries.

Required fault classes:

- command fails before commit;
- persistence write interrupted before atomic replace;
- save payload truncated or checksum-invalid;
- unknown schema/component/trigger key;
- region transition fails during preparation or validation;
- target/entity destroyed while reserved;
- storage/destination disabled during delivery;
- abstract graph edge disabled during travel;
- detailed route invalidated repeatedly;
- event/trigger duplicated at delivery boundary;
- operations projection fails after authoritative commit;
- graphical adapter terminates without mutating world authority.

Fault injection must be explicit, disabled by default, deterministic, inspectable, and unavailable to ordinary game content unless deliberately composed for testing.

### Long-horizon soak

Required headless soak:

- at least 365 simulated days;
- supported five-region fixture;
- repeated infrastructure shortages and recovery;
- scheduled fidelity switching;
- periodic saves and fresh-process loads;
- invariant monitor enabled;
- deterministic final-state and ledger evidence;
- memory and throughput trend evidence;
- no unbounded queue, artifact, journal, or projection growth.

Required interactive soak:

- at least four continuous real hours in a graphics-capable environment;
- normal operations interaction, region switching, pause/speed changes, save/load, and inspection;
- frame-time/memory trend capture;
- no requirement for pixel identity;
- explicit operator session manifest.

The interactive soak may be split into resumable sessions only where the runbook defines continuity evidence; the total four-hour requirement must still be demonstrated.

### Persistence compatibility and recovery

Declare one M035-supported save compatibility policy.

Required policy topics:

- current save schema version;
- minimum supported prior schema or explicit “current-only before M035” boundary;
- forward-incompatible save behavior;
- unknown optional versus required fields/types;
- migration registration and ordering;
- atomic save replacement;
- backup/previous-good-save policy;
- checksum/fingerprint validation;
- corruption diagnostics;
- recovery command and output;
- retained golden/reference saves;
- save anonymization/sanitization if needed for sharing internally.

M035 must retain representative reference saves for:

- stable settlement;
- active construction;
- active carrying;
- pending abstract triggers;
- immediately after fidelity transition;
- active shortage;
- failed infrastructure;
- pre-M035 supported schema where applicable.

### Crash and failure evidence

Any unhandled process failure, invariant violation, aggregate campaign failure, or persistence recovery failure must produce a bounded evidence bundle where process state permits.

Required evidence:

- run/session ID;
- repository/build fingerprint;
- authored input/seed;
- scenario/campaign and shard;
- simulation instant;
- active region/fidelity state;
- recent commands/events/triggers;
- activities/reservations;
- invariants and diagnostics;
- save/checkpoint reference;
- reproduction command;
- environment/runtime classification;
- sanitized exception/crash information.

Do not include secrets, absolute personal paths as semantic authority, native memory dumps by default, or unbounded logs.

### Reproduction bundles

Provide a portable repository-relative reproduction bundle.

A bundle contains or references:

- schema/version;
- scenario/campaign identity;
- seed and authored inputs;
- required save/checkpoint;
- command/event/input replay where required;
- expected failure signature;
- deterministic runtime configuration;
- exact reproduction command;
- artifact index;
- minimization status;
- redaction/sanitization report;
- observed versus expected result.

Required commands conceptually:

```text
repro capture
repro inspect
repro run
repro verify
repro minimize or reduce, where bounded support exists
```

A reproduction bundle must not require the external guide repository or the planning conversation.

### Internal tester workflow

Provide a concise repository-local internal-testing runbook.

It must cover:

- supported host/runtime and graphics setup;
- starting a reference world;
- loading reference saves;
- normal save locations;
- enabling diagnostics and invariant monitoring;
- running a session with a tester/session ID;
- reporting a defect;
- capturing a reproduction bundle;
- locating alerts, event journal, activities, reservations, queue, and transition evidence;
- known limitations and unsupported actions;
- recovery from corrupt or interrupted saves;
- exact commands for readiness campaigns;
- what constitutes a blocking defect.

The runbook is project truth, not a copied guide.

### Measured optimization

Optimization is authorized only for a measured blocking or materially regressed metric.

Potential implementation families, when evidence requires them:

- query/index improvements;
- pathfinding cache or bounded reuse;
- queue compaction;
- reduced projection rebuilding;
- allocation reduction;
- serialization streaming/buffering;
- event/journal retention bounds;
- targeted component-store optimization;
- batched derived-state invalidation.

Not automatically authorized:

- wholesale archetype ECS rewrite;
- multithreaded simulation;
- distributed execution;
- unsafe memory;
- generalized job system;
- broad public API redesign.

Any substantial optimization must include semantic equivalence and before/after evidence.

### Heavy internal testing readiness decision

M035 produces a readiness decision with:

```text
ready
ready-with-declared-limitations
not-ready
```

Only `ready` or `ready-with-declared-limitations` completes the milestone.

Declared limitations must:

- be outside the supported envelope or non-blocking within it;
- have concrete user impact;
- include workaround where available;
- not conceal data corruption, resource duplication, deadlock, unreproducible crashes, or failed recovery.

## Non-goals

Do not implement:

- a new gameplay subsystem;
- cross-region hauling/trade;
- multiple detailed regions;
- character traits, skills, health, combat, social systems, or research;
- complex weather, temperature, power, pipe, or waste networks;
- commercial release packaging;
- public early-access launch readiness;
- final audiovisual asset integration or M030;
- generalized modding/plugin infrastructure;
- multiplayer/networking;
- distributed simulation;
- speculative multithreading;
- wholesale ECS rewrite without measured blocking evidence;
- broad guide migration;
- TBPs, issue templates, or workflow YAML.

## Focus Area 1 — Scale envelope and performance budgets

Authority:

- `docs/specs/internal-testing-scale-and-performance-contract.md`;
- `docs/architecture/internal-testing-readiness-architecture.md`;
- `docs/decisions/ADR-0046-readiness-is-a-versioned-evidence-gate-and-optimization-is-measurement-driven.md`.

Required outcomes:

- canonical scale fixtures;
- metric definitions;
- budgets and regression thresholds;
- baseline provenance and promotion rules;
- headless and graphical measurement;
- machine-readable comparison.

Blocking cases:

- non-comparable environment presented as regression pass;
- baseline silently replaced;
- semantic failure masked by timing success;
- tail latency omitted where it exposes stalls;
- unbounded growth across soak.

## Focus Area 2 — Invariants, deadlock detection, and diagnostics

Authority:

- `docs/specs/runtime-health-and-diagnostics-contract.md`;
- existing M031–M034 invariant and artifact authority.

Required outcomes:

- continuous bounded invariant monitor;
- progress/deadlock/livelock/starvation detection;
- queue/reservation/activity health;
- stable diagnostic taxonomy;
- bounded evidence on failure;
- no hidden automatic repair.

Blocking cases:

- leaked reservation;
- ownerless activity;
- same-instant event loop;
- repeated route replan;
- critical supply available but never selected;
- monitor changes authoritative outcome.

## Focus Area 3 — Stress, soak, and fault campaigns

Authority:

- `docs/specs/stress-soak-and-fault-campaign-contract.md`;
- `docs/scenarios/m035-heavy-internal-testing-readiness-campaign.md`.

Required outcomes:

- deterministic campaign plans and shards;
- 365-day headless soak;
- four-hour graphical soak;
- transition/save/load churn;
- deterministic fault injection;
- resume/verify behavior;
- trend and failure evidence.

Blocking cases:

- early termination counted as success;
- partial shard evidence aggregated;
- fault injection active outside test composition;
- nondeterministic rerun without diagnostics;
- memory, queue, journal, or artifact growth unbounded.

## Focus Area 4 — Persistence compatibility and recovery

Authority:

- `docs/specs/save-compatibility-and-recovery-contract.md`;
- existing M031–M034 persistence contracts.

Required outcomes:

- explicit compatibility policy;
- retained reference saves;
- schema migration/diagnostics;
- atomic save and previous-good recovery;
- corruption detection;
- recovery commands;
- fresh-process compatibility matrix.

Blocking cases:

- partial world mutation on load failure;
- corrupt save accepted silently;
- old supported save cannot migrate;
- recovery overwrites last good evidence;
- save/load changes canonical authority unexpectedly.

## Focus Area 5 — Reproduction bundles and tester workflow

Authority:

- `docs/specs/reproduction-and-internal-testing-contract.md`;
- `docs/engineering/internal-testing-runbook.md`;
- artifact contract.

Required outcomes:

- session manifest;
- failure capture;
- portable reproduction bundle;
- exact reproduction/verify commands;
- bounded logs and sanitized evidence;
- tester runbook;
- reference worlds/saves and defect-report workflow.

Blocking cases:

- failure cannot be reproduced from bundle;
- bundle depends on local absolute paths or planning context;
- missing seed/build fingerprint;
- unbounded or secret-bearing logs;
- tester needs debugger/source knowledge for routine reporting.

## Focus Area 6 — Optimization, readiness campaign, and review

Authority:

- all M035 contracts;
- `docs/artifacts/heavy-internal-testing-readiness-artifact-contract.md`;
- review request.

Required outcomes:

- measured targeted optimization only;
- before/after semantic equivalence;
- complete readiness matrix;
- blocking-defect disposition;
- readiness decision;
- resumable aggregate verification;
- blocking human review.

## Implementation constraints

### No new game feature family

M035 may add test fixtures, diagnostics, recovery, bounded retention, and measured optimizations. It must not add a major player-facing simulation system.

### Semantic authority

M031–M034 rules remain authoritative. Health monitors, profilers, campaign harnesses, and reproduction tools observe or inject declared test faults; they do not become gameplay authority.

### Determinism

Stress and fault campaigns must remain reproducible. Performance timing may vary, but authored inputs, semantic outcomes, failure signatures, and invariant results must be deterministic under comparable configuration.

### Bounded evidence

Logs, journals, traces, queue snapshots, screenshots, and reproduction bundles require declared retention and size limits.

Evidence truncation must be explicit and preserve the most relevant causal window.

### Compatibility

Do not silently reinterpret saves. Schema/version handling and migration must be explicit, ordered, and artifacted.

### Optimization authority

A measured failing budget or significant regression is required before architectural optimization. Every optimization records:

- triggering metric;
- before result;
- implementation family;
- semantic validation;
- after result;
- remaining limitation.

### Provider versus dogfood

Reusable health, campaign, compatibility, reproduction, and measurement capabilities belong in provider modules. M034 settlement fixtures remain bounded dogfood.

### Public API posture

Prefer CLI/tooling and internal engineering contracts for readiness operations. Add public runtime APIs only where consumers require stable health, checkpoint, or reproduction integration.

## Required authority documents

The implementation agent must read only this list before implementation, plus directly referenced existing types where necessary:

1. `AGENTS.md`;
2. `README.md`;
3. `docs/ENGINEERING.md`;
4. `docs/engineering/command-contract.md`;
5. `docs/engineering/validation-tiers.md`;
6. `docs/engineering/human-review-workflow.md`;
7. `docs/TERMINOLOGY.md`;
8. `docs/SPECS.md`;
9. `docs/specs/runtime-principles.md`;
10. M031–M034 permanent specs, architecture records, ADRs, scenarios, and artifact contracts resolved through `docs/SPECS.md` and the active milestone indexes;
11. existing persistence, runtime-inspection, product-CLI, performance, diagnostics, rendering, and input authority relevant to the implementation;
12. `docs/specs/internal-testing-scale-and-performance-contract.md`;
13. `docs/specs/runtime-health-and-diagnostics-contract.md`;
14. `docs/specs/stress-soak-and-fault-campaign-contract.md`;
15. `docs/specs/save-compatibility-and-recovery-contract.md`;
16. `docs/specs/reproduction-and-internal-testing-contract.md`;
17. `docs/architecture/internal-testing-readiness-architecture.md`;
18. `docs/decisions/ADR-0046-readiness-is-a-versioned-evidence-gate-and-optimization-is-measurement-driven.md`;
19. `docs/engineering/internal-testing-runbook.md`;
20. `docs/scenarios/m035-heavy-internal-testing-readiness-campaign.md`;
21. `docs/artifacts/heavy-internal-testing-readiness-artifact-contract.md`;
22. this milestone document.

Do not read unrelated milestone bodies after resolving permanent authority.

Do not read the external guide repository, `.guide-profile.json`, `.guide-sync/`, copied guides, prompt templates, or `docs/research/`.

Read `.review/` only for M035 review and readiness disposition.

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
src/Agentic2D.Input/
src/Agentic2D.DebugClient.Raylib/
src/Agentic2D.Engineering/
tests/unit/Agentic2D.Tests.Unit/
authored stress, soak, fault, and reference-save fixtures
eng/
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/ARTIFACTS.md
docs/SCENARIOS.md
README.md
.review/
artifacts/
```

Do not include implementation source, tests, generated artifacts, or reference-save binaries in this planning package.

## Validation tiers and concrete commands

### Tier 0 — Repository and documentation

```bash
./eng/format.sh --verify
./eng/docs-check.sh
./eng/check.sh
```

### Tier 1 — Focused unit and contract validation

```bash
./eng/test-filter.sh PerformanceBudget
./eng/test-filter.sh RuntimeHealth
./eng/test-filter.sh DeadlockDetection
./eng/test-filter.sh FaultInjection
./eng/test-filter.sh SaveCompatibility
./eng/test-filter.sh SaveRecovery
./eng/test-filter.sh ReproductionBundle
./eng/test-filter.sh InternalTestSession
./eng/test-filter.sh ReadinessGate
```

### Tier 2 — Focused capability smoke

Create and run:

```bash
./eng/performance-budget-smoke.sh
./eng/runtime-health-smoke.sh
./eng/deadlock-detection-smoke.sh
./eng/fault-injection-smoke.sh
./eng/save-compatibility-smoke.sh
./eng/save-recovery-smoke.sh
./eng/reproduction-bundle-smoke.sh
./eng/internal-test-session-smoke.sh
./eng/m035-readiness-smoke.sh
```

Graphics-capable focused validation:

```bash
./eng/m035-graphical-soak-smoke.sh
```

The graphical command reports `passed`, `failed`, or `skipped-not-graphics-capable`. A skip cannot satisfy readiness review.

### Tier 3 — Regression

```bash
./eng/m031-smoke.sh --verify
./eng/m032-smoke.sh --verify
./eng/m033-smoke.sh --verify
./eng/m034-smoke.sh --verify
./eng/gameplay-integrated-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/persistence-diagnostics-smoke.sh
./eng/render-projection-smoke.sh
./eng/input-replay-smoke.sh
./eng/m028-smoke.sh --verify
./eng/m029-smoke.sh --verify
```

Use current authoritative commands where an older suite lacks `--verify`.

### Tier 4 — Resumable M035 suite

```bash
./eng/m035-smoke.sh
./eng/m035-smoke.sh --plan-json
./eng/m035-smoke.sh --shard <id>
./eng/m035-smoke.sh --verify
```

Required shards:

```text
documentation
scale-fixtures
performance-baselines
performance-regression
runtime-invariants
deadlock-livelock-starvation
queue-reservation-health
fault-command-persistence
fault-transition-execution
save-compatibility-matrix
save-corruption-recovery
reproduction-bundles
tester-session-workflow
population-entity-stress
pathfinding-work-stress
abstract-queue-stress
fidelity-transition-churn
persistence-cycle-campaign
infrastructure-shortage-campaign
headless-365-day-soak
graphical-4-hour-soak
memory-throughput-trends
m031-m034-regression
engine-regression
asset-train-regression
readiness-report
human-review
integrated
```

Only `./eng/m035-smoke.sh --verify` establishes aggregate success.

### Tier 5 — Human readiness review

```bash
./eng/review-list.sh --milestone M035
./eng/review-show.sh review.m035.heavy-internal-testing-readiness
./eng/review-check.sh --milestone M035
```

## Validation execution mode

```text
Tier 0: direct
Tier 1: direct/focused
Tier 2 structural: direct
Tier 2 campaign/soak: resumable-sharded
Tier 2 graphical: graphics-capable human-operated or supervised
Tier 3: direct where bounded; resumable where aggregate is long
Tier 4: resumable-sharded
Tier 5: human-review
```

## Resumable validation contract

Suite:

```bash
./eng/m035-smoke.sh
```

Plan:

```bash
./eng/m035-smoke.sh --plan-json
```

Shard:

```bash
./eng/m035-smoke.sh --shard <id>
```

Receipts:

```text
artifacts/validation/m035-smoke/plan.json
artifacts/validation/m035-smoke/receipts/<shard-id>.json
artifacts/validation/m035-smoke/verify.json
```

Long-running campaign shards may have nested case receipts under:

```text
artifacts/validation/m035-smoke/campaigns/<campaign-id>/plan.json
artifacts/validation/m035-smoke/campaigns/<campaign-id>/receipts/<case-id>.json
artifacts/validation/m035-smoke/campaigns/<campaign-id>/verify.json
```

The parent shard passes only when its nested campaign verifier passes.

Fingerprint scope includes:

- M035 implementation and tests;
- relevant M031–M034 dependencies;
- stress/soak/fault fixtures;
- reference-save manifests and migrations;
- M035 authority documents;
- engineering wrappers and tested host;
- solution/project files;
- performance budget/baseline configuration;
- direct documentation indexes;
- graphical and headless session manifests;
- readiness report;
- M035 review request/record state.

Each receipt records:

- suite, shard, and optional case ID;
- schema and fingerprint;
- command and environment classification;
- start/end and result;
- semantic/invariant status;
- metric and trend summary where relevant;
- safety-limit/early-termination status;
- evidence paths.

`--verify` fails when:

- a required receipt is missing, stale, failed, or incomplete;
- a nested campaign is not verified;
- a soak ended early;
- a blocking semantic or compatibility budget failed;
- performance comparison is non-comparable but presented as passed;
- an unbounded trend is detected;
- a failure lacks required reproduction evidence;
- graphical soak evidence is absent or failed;
- readiness report is not `ready` or `ready-with-declared-limitations`;
- human review is not approved;
- aggregate success is inferred from partial output.

## Acceptance criteria

M035 is accepted only when all are true:

1. A versioned supported internal-testing envelope is documented and artifacted.
2. The five-region, fifty-worker, one-thousand-entity minimum fixture completes required validation.
3. Performance metrics and budgets have stable definitions and baseline provenance.
4. Blocking semantic budgets pass.
5. Advisory performance regressions are either within threshold or explicitly dispositioned with evidence.
6. No unbounded memory, queue, journal, artifact, or projection growth occurs in required soaks.
7. Continuous invariant monitoring detects injected violations and does not change valid outcomes.
8. Deadlock, livelock, starvation, repeated-replan, and same-instant-loop detectors produce actionable evidence.
9. Reservation, activity, queue, region ownership, conservation, construction, crop, infrastructure, and alert invariants remain valid under stress.
10. Fault injection is deterministic, test-only, disabled by default, and covers all required fault classes.
11. The 365-simulated-day headless soak completes with deterministic rerun evidence.
12. The four-real-hour graphical soak completes in a documented graphics-capable environment.
13. At least 1,000 fidelity transitions complete without mixed ownership or unexplained divergence.
14. At least 250 save/load cycles complete with fresh-process continuation.
15. The save compatibility policy is explicit and enforced.
16. All retained supported reference saves load or migrate successfully.
17. Corrupt, truncated, unknown, and forward-incompatible saves fail safely with actionable diagnostics.
18. Atomic recovery preserves a previous good save and does not partially mutate the destination world.
19. Every injected blocking failure produces or references a bounded reproduction bundle.
20. Reproduction bundles run and verify without planning context, local absolute paths, or external guide access.
21. Internal testers can start, save, load, diagnose, and report defects using the runbook without debugger/source knowledge.
22. Crash/invariant evidence includes build, input, seed, time, region/fidelity, causal history, and reproduction command where process state permits.
23. Any optimization is tied to measured evidence and preserves semantic equivalence.
24. M031–M034 and earlier regressions pass.
25. The readiness artifact set validates completely.
26. `./eng/m035-smoke.sh --verify` passes from current receipts.
27. The readiness decision is `ready` or `ready-with-declared-limitations`.
28. No unresolved blocking defect remains within the supported envelope.
29. The blocking M035 human review is approved.
30. Direct documentation reflects current project truth and support limits.
31. No new gameplay subsystem, M030 integration, speculative rewrite, or guide dependency is introduced.

## Direct documentation impact

Update:

- `README.md` with heavy-internal-testing-ready status only after the gate passes;
- `docs/TERMINOLOGY.md` with support envelope, soak, campaign, fault injection, reproduction bundle, readiness decision, and blocking defect;
- `docs/SPECS.md` with M035 permanent contracts;
- `docs/ENGINEERING.md` with health, campaign, recovery, reproduction, and readiness commands;
- `docs/ARTIFACTS.md` with M035 artifacts;
- `docs/SCENARIOS.md` with scale/stress/soak fixtures;
- active persistence and runtime-inspection docs where compatibility or diagnostics truth changes;
- a concise supported-envelope and known-limitations project document if current index conventions require it.

Do not claim heavy-internal-testing readiness before aggregate verification and review approval.

## Deferred documentation synchronization hints

The package includes:

```text
.guide-sync/pending/2026-07-22-m035-heavy-internal-testing-readiness-sync.md
```

Implementation agents must not read or resolve it.

## Human-review requirements

```text
applicability: required
completion effect: blocking
review classes: release-readiness, engineering, gameplay-operations, UX, artifact-quality
canonical review ID: review.m035.heavy-internal-testing-readiness
owning milestone: M035
owning milestone path: docs/milestones/MILESTONE-035-scale-stability-and-heavy-internal-testing-readiness.md
reviewer role: repository user and designated engine reviewer; one person may fill both roles if explicitly recorded
acceptable completion decisions: ready, ready-with-declared-limitations
```

Review subject:

> Whether the declared simulation/game support envelope is sufficiently stable, measurable, recoverable, diagnosable, reproducible, and usable for heavy internal testing.

Required evidence is defined by the review request and artifact contract.

No implicit waiver. A readiness decision of `not-ready` blocks completion.

Completed approval becomes historical M035 evidence. Future release or public-EA milestones must declare their own readiness review and must not reopen M035 merely because the repository later changes.

## Constrained-runtime handling

1. Run `./eng/m035-smoke.sh --plan-json`.
2. Execute focused structural shards separately.
3. Generate nested plans for stress, save-cycle, headless-soak, and graphical-soak campaigns.
4. Run each case in a foreground invocation and preserve receipts.
5. Verify each nested campaign.
6. Generate the readiness report and review pack.
7. Complete the M035 review.
8. Rerun `readiness-report` and `human-review` shards if their evidence predates the final decision.
9. Run `./eng/m035-smoke.sh --verify`.
10. Report readiness only from successful aggregate verification.

Do not background long runs, inflate command timeouts as a substitute for resumability, or treat partial logs/case output as campaign success.

## Out-of-scope guide migration work

No guide migration is part of M035.

The external guide repository is planning input only. Target repository documentation contains project truth and must not reference guide documents as operational authority.
