# Multi-Fidelity Equivalence Contract

## Authority

Authoritative for M042 control composition, equivalence classes, tolerance policy, determinism, observer neutrality, continuation comparison and long-horizon multi-fidelity evidence.

## Required controls

```text
abstract-control
periodically-switched
mostly-detailed
detailed-reference
```

Controls must be mechanically proven distinct before equivalence results are valid.

## Rule-equivalent: zero tolerance

Zero tolerance applies to identity/lifecycle, equivalent-state work eligibility/priority, activity legality, reservation/capacity legality, command semantics, fixed need policy, conservation, duplicate completion, terminal reservation cleanup, stale old-epoch mutation, executor ownership, persistence referential integrity and half-transition validity.

## Same-schedule determinism

Identical initial state and identical execution schedule require exact fingerprints and ordered semantic evidence. Uninterrupted versus fresh-process resumed continuation under the same schedule also requires exact target equality.

## Bounded temporal approximation

Cross-schedule timing uses:

```text
T_base =
  detailed fixed-step quantum
+ abstract scheduling quantum
+ reconciliation mapping time-error bound
```

Blocked/retry comparison may add one retry quantum. Mandatory-need comparison may add one need-integration threshold quantum. Concrete values are derived before comparison from current authored/runtime policy. No tolerance scales with switch count.

## Completion timing

Matching semantic work by stable region/family/target/work key:

```text
abs(A.completion - B.completion) <= applicable T
```

where both runs complete the work inside the comparison window.

## Arrival ordering

For two corresponding contenders separated by more than `2 * T_base` in both runs, relative order must agree. Inside the near-simultaneous window, order may differ if zero-tolerance reservation/rule semantics remain valid.

## Fixed-horizon boundary allowance

At a target instant:

```text
abs(metricA - metricB)
<= currentInFlightAllowance(A) + currentInFlightAllowance(B)
```

Allowance is the maximum remaining contribution of currently nonterminal authoritative activities that can affect the metric. Completed historical activities do not contribute. Conservation and capacity are never approximate.

## Observer neutrality

Observer-neutrality schedules vary switch segmentation while holding total detailed exposure per region constant.

Compare low/medium/high switching. Required metrics include work/resource completion, source/carried/stored quantities, need warnings/mandatory/satisfaction, idle duration, blocked/retry duration, reservation conflicts, failures, and stale/cancelled triggers.

All schedules use identical zero-tolerance and bounded policies. No `switchCount * epsilon`. Divergence growing with switching beyond the fixed envelope is failure.

## Control schedules

Canonical 30-day controls:

- abstract control: all regions static abstract;
- periodic mixed: round robin every 24h;
- mostly detailed: repeating alpha 4d / beta 1d / gamma 1d;
- detailed reference: independent detailed run per non-interacting region, aggregated.

Observer-neutrality:

- low: alpha 10d / beta 10d / gamma 10d;
- medium: 24h round robin;
- high: 6h round robin.

Each observer schedule gives every region exactly ten days detailed exposure.

## Mixed orchestration ordering

At the same semantic instant:

1. fidelity transition boundary;
2. abstract triggers;
3. detailed fixed-step boundary;
4. derived inspection.

This ensures due-but-undelivered old-owner triggers are reconciled by the transition fence rather than double-delivered.

## Persistence continuation

Required fresh-process checkpoint classes:

```text
abstract-travel
abstract-carrying
immediately-after-materialization
detailed-carrying
immediately-after-abstraction
equal-time-trigger-and-switch-boundary
mandatory-need-interruption
```

Same schedule/fingerprint must be supplied after load. Consumer advances beyond checkpoint to common target. Resumed versus uninterrupted target equality is exact.

## Long horizon

Required stability campaign:

```text
>= 365 semantic days
>= 5 regions
>= 1000 successful paired switches
```

Prove stable ownership, conservation, capacity, reservation cleanup, no stale success mutation, no half transition, bounded obsolete continuation, valid periodic persistence and same-schedule deterministic rerun.

## Evidence independence

Raw run observations are input to an independent comparer. The comparer calculates schedule distinctness, invariants, conservation, timing deltas, boundary allowances, neutrality deltas and continuation equality. Scenario-produced booleans are not acceptance authority.

## Failure

Fail on aliased controls, invariant difference, exceeded timing/boundary envelope, switch-count-scaled tolerance, observer exposure mismatch, early/incomplete run, omitted control/checkpoint, failed same-schedule determinism or stale/incompatible evidence.
