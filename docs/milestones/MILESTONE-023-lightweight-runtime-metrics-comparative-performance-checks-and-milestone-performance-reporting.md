# Milestone 023 — Lightweight Runtime Metrics, Comparative Performance Checks, and Milestone Performance Reporting

## Goal

Add bounded, observational runtime metrics and repeatable same-machine performance evidence without changing deterministic simulation semantics. Complete the repository-specific guide-system v0.5.1 corrective assessment.

## Repository role and maturity assumptions

```text
role: capability-provider
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation
maturity: implementation-ready, artifact-first
execution: implementation milestone with optional human review
```

The provider owns reusable metrics, product artifacts, and engineering comparisons. Consumer projects opt into collection through `project run`; they do not define metric dimensions or performance policy.

## Authority routing

Read only these authorities for M023 implementation: `README.md`, `docs/ENGINEERING.md`, `docs/engineering/command-contract.md`, `docs/engineering/constrained-validation-execution.md`, `docs/engineering/validation-tiers.md`, `docs/specs/runtime-principles.md`, `docs/specs/product-cli-contract.md`, and `docs/specs/unified-agent-execution-workflow-contract.md`.

The v0.5.1 ZIP is unpacked at repository root for the requested corrective assessment, but it is not implementation authority. Ordinary implementation agents do not read `.guide-profile.json` or `.guide-sync/`; the `guide-v051` shard reads the profile only as machine-readable metadata.

## Scope and non-goals

In scope: bounded counter/gauge/duration collection; centralized tick/phase timing; metrics run artifacts; four reference workloads; advisory capture/compare/report commands; M023 resumable validation; v0.5.1 profile correction.

Out of scope: changing fixed-tick scheduling, game semantics, arbitrary tag dictionaries, entity labels, unbounded time series, cross-machine claims, a benchmark service, synthetic gameplay complexity, mandatory graphical overlay, and modifying M022 engineering infrastructure beyond the profile version correction.

## Focus areas

### Metrics model and instrumentation

Metric IDs are a stable finite vocabulary: `runtime.tick.duration`, five phase durations, active entities, submitted/accepted/rejected commands, emitted events, spatial query/collision counters, render/effect/particle/UI/text/sound gauges or counters, and persistence save/load durations. Types are only `counter`, `gauge`, and `duration`.

Modes are `off`, `summary`, and `per-tick`. Off allocates no collector or artifacts. Summary retains aggregates only. Per-tick retains a fixed 300-tick ring window; no hot-path tag maps or per-entity dimensions are allowed.

Central runtime tick boundaries observe monotonic host time. Behavior, spatial, mutation, presentation, and render-projection phase scopes are separate. Work counters remain deterministic observations; elapsed values and rates are observational. Metrics must not alter commands, events, random seeds, spatial results, presentation, saves, or replay equivalence.

Snapshots expose current values, total/min/max/average summaries, a recent fixed window, recent p95, effective ticks/sec, average active entities, work/tick, and work/observed-second. Simulation time remains fixed and separate from wall-clock projection.

### Product artifacts and inspection

`agentic2d runtime smoke [--metrics off|summary|per-tick] --output <directory>` and `agentic2d project run <project> --scenario <id> --metrics off|summary|per-tick --output <directory>` support collection. Enabled runs write `metrics-summary.json`; per-tick mode additionally writes bounded `metrics-ticks.jsonl`. Timing fields explicitly state that they are not deterministic authority. `run inspect` sees metrics as a normal artifact family.

An optional Raylib overlay remains human-review-only and is deferred; headless artifacts are acceptance authority.

### Comparative workloads and reports

The fixed reference workloads are `performance.runtime-reference`, `performance.entities-reference`, `performance.persistent-world-reference`, and `performance.presentation-reference`. Each declares its stable ID, existing source scenario, fixed seed, summary mode, relevant metrics, post-measurement artifact boundary, one warm-up iteration, five measured headless Release iterations, and median primary statistic.

`perf-capture` records elapsed medians, allocated bytes, and deterministic work counters separately. Comparisons assume one machine, identical configuration/workload definitions/seeds, and captures close in time. They are advisory only: less than 5% absolute elapsed change is `within-noise`; decreases of 5% or more are `improved`; increases of 5% or more are `possible-regression` pending explanation. Allowed report statuses also include `not-applicable`, `not-measured`, `expected-cost`, and `confirmed-regression`; not-applicable must include a reason.

Reports are written to `artifacts/performance/<milestone-id>/performance-report.json` and `.md`, including these limitations. From M024 onward, implementation milestones must declare affected workloads, comparison or not-applicable reason, expected work-count changes, report paths, and possible-regression treatment.

## Direct and deferred documentation impact

Direct documentation impact is this milestone plus engineering command indexing. Deferred: optional overlay contract and M024+ workload declaration sections. No broad documentation synchronization, public release documentation, copied guides, TBPs, or issue templates are introduced.

## Human review assessment

Review is recommended and non-blocking. Suggested evidence: summary and short tick stream, before/after comparison, JSON/Markdown report, optional overlay screenshot, M023 verifier, and profile-version check. Suggested subjects are vocabulary usefulness, collection overhead, workload representativeness, report clarity, overlay readability, and corrective-assessment completeness. No request is required unless automation uncovers an unresolved issue.

## Validation and execution handoff

Tier 1: `./eng/build.sh`, `./eng/test-filter.sh Metrics`, `./eng/test-filter.sh Performance`, `./eng/perf-smoke.sh`.

Tier 2 direct checks: `./eng/check.sh`, `./eng/cli-smoke.sh`, `./eng/product-validate.sh`.

M023 is resumable-sharded. Use `./eng/m023-smoke.sh --plan-json`, then separate invocations of `metrics-contracts`, `runtime-instrumentation`, `metrics-artifacts`, `comparative-workloads`, `performance-report`, `integrated`, and `guide-v051`; stop on real failure; finally run `./eng/m023-smoke.sh --verify`. Receipts are atomic under `artifacts/validation/m023-smoke/`; timing values never participate in receipt fingerprints. Only successful verification establishes aggregate completion.

Regression handoff also runs M019/M020/M021 through each suite's `--plan-json`, every required shard, and `--verify` interfaces. Do not use backgrounding, timeout inflation, or partial logs as proof.
