# M042 Multi-Fidelity Equivalence and Continuation Scenario

## Identity

```text
scenario.m042.multi-fidelity-equivalence-and-continuation
```

## Purpose

Canonical deterministic scenario for M042 mixed-fidelity equivalence, observer-neutrality and fresh-process continuation.

## Regions

```text
region.alpha
region.beta
region.gamma
```

Regions are gameplay-independent:

- no cross-region worker transfer;
- no cross-region hauling;
- no shared storage/resource authority.

This permits independent per-region detailed reference controls.

## Per-region semantic capability

Each region contains deterministic authored state sufficient for:

- generic workers;
- extraction designations;
- harvestable work;
- finite inventory;
- storage designation/capacity;
- food/water/comfort need sources;
- mandatory need interruption;
- detailed grid/path execution;
- abstract graph execution;
- detailed↔abstract mapping;
- carrying;
- blocked/retry proof cases.

The workload is finite but sized so the fastest observer-neutrality control does not exhaust ordinary work before 90% of the 30-day horizon.

## Canonical horizon

```text
30 semantic days
```

## Canonical controls

### Abstract
All three regions static abstract for 30d.

### Periodically switched
Exactly one detailed region, rotating alpha/beta/gamma every 24h.

### Mostly detailed
Repeating six-day schedule:

```text
alpha 4d
beta 1d
gamma 1d
```

repeated five times.

### Detailed reference
Three independent single-region detailed runs, one for each canonical region, aggregated by the comparison harness.

## Observer-neutrality controls

All have exactly ten days detailed exposure per region.

### Low

```text
alpha 10d
beta 10d
gamma 10d
```

### Medium
24h round-robin.

### High
6h round-robin.

## Fresh-process checkpoint classes

```text
abstract-travel
abstract-carrying
immediately-after-materialization
detailed-carrying
immediately-after-abstraction
equal-time-trigger-and-switch-boundary
mandatory-need-interruption
```

Each checkpoint is a stable save boundary. Consumer continues under the identical schedule to the declared common target.

## Long-horizon variant

Separate deterministic stress composition:

```text
>= 5 independent persistent regions
>= 365 semantic days
>= 1000 successful paired switches
```

No new gameplay semantics are introduced for the stress variant.

## Comparison

Use the authoritative multi-fidelity equivalence contract. Raw scenario observations do not decide pass/fail themselves. An independent comparer calculates acceptance metrics.

## Human review

None.
