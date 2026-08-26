# ADR-0054 — Multi-Fidelity Equivalence Uses Distinct Controls and Switch-Count-Independent Bounds

## Status

Accepted for M042.

## Context

The historical M033 equivalence proof could compare duplicated runs under different labels and emit observer-neutrality conclusions without calculating them. M040 and M041 create real executor and transition boundaries. M042 requires a comparison policy that cannot hide accumulated transition bias behind increasingly generous tolerances.

## Decision

### Controls must be genuinely distinct

M042 uses four separate controls:

```text
abstract-control
periodically-switched
mostly-detailed
detailed-reference
```

The harness proves they differ through schedule fingerprints and execution counters before comparing outcomes.

The detailed reference uses independent per-region detailed controls because the canonical M042 regions have no cross-region gameplay and the mixed-world contract permits one detailed region at a time.

### Rule semantics use zero tolerance

Identity, lifecycle, eligibility rules, priorities, activity legality, reservation/capacity legality, command semantics, need policy, conservation, duplicate completion, stale mutation and ownership validity are not approximate.

### Same-schedule behavior is exact

Identical seed + scenario + execution schedule requires exact deterministic fingerprint/evidence. Save/resume continuation under the same schedule also requires exact target equality.

### Timing bounds derive from execution granularity

Cross-mode timing uses a declared envelope derived before comparison from:

```text
detailed fixed-step quantum
+ abstract scheduling quantum
+ reconciliation mapping time-error bound
```

Blocked and need cases may add one declared retry or need-integration quantum. No term is multiplied by switch count.

### Fixed-horizon work differences use in-flight boundary allowance

At an arbitrary horizon, completed totals may differ only by the maximum contribution of currently nonterminal activities that can still affect the metric. Historical completed work does not enlarge the allowance. Conservation and capacity remain exact.

### Observer neutrality controls exposure

Low/medium/high switch-frequency schedules give each region equal total detailed exposure. They use the same tolerance policy. High switch frequency gets no additional error budget. Systematic divergence that grows with switch count beyond the fixed envelope is failure.

### Comparison is independent

The comparer calculates invariants, deltas, allowances and neutrality from raw observations. Producer-emitted success booleans are not acceptance authority.

## Consequences

Positive:

- equivalence cannot pass because control labels alias the same run;
- tolerance cannot be tuned to accumulated switching defects;
- observer neutrality isolates switch segmentation rather than detailed exposure;
- persistence continuation is stricter than cross-mode comparison;
- long-horizon transition tax becomes visible.

Costs:

- comparison harness is substantial;
- raw observations must retain enough semantic identity to match work/timing;
- runtime policy must expose timing quantum/mapping-bound metadata;
- canonical scenario must retain non-exhausted work long enough to measure neutrality.

## Rejected Alternatives

### Exact fingerprints across different fidelity schedules
Rejected because detailed and abstract continuation intentionally approximate time/position differently.

### One numeric global tolerance
Rejected because it mixes rule errors, timing approximation and fixed-horizon boundary effects.

### Tolerance proportional to number of switches
Rejected because it explicitly permits cumulative switching bias.

### Compare only final resource totals
Rejected because reservations, needs, timing, stale work and observer bias can fail while final totals happen to match.

### Random switch schedules as completion authority
Rejected because deterministic authored controls provide reproducible diagnosis.
