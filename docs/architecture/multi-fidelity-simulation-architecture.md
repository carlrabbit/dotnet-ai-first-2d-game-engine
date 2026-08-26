# Multi-Fidelity Simulation Architecture

## Corrective closure sequence

```text
M040 — real independent executors over shared semantics
M041 — transactional fidelity ownership/reconciliation
M042 — mixed-fidelity orchestration/equivalence/continuation
```

## Core shape

```text
                  shared semantic authority
                work / logistics / needs
             activities / reservations / facts
                         │
             ┌───────────┴───────────┐
             ▼                       ▼
     detailed executor        abstract executor
      grid + fixed-step         DES + durations
             │                       │
             └──────────┬────────────┘
                        ▼
              M041 transition boundary
          handoff + mapping + staged commit
                        │
                        ▼
              M042 mixed orchestrator
          schedule + global boundary ordering
                        │
                        ▼
             independent equivalence comparer
```

## Mixed orchestrator

One `SimulationWorld` and one semantic clock.

At each iteration choose the earliest of authored switch instant, next abstract trigger, next detailed step or target horizon.

Same-instant phase order:

```text
switch
abstract trigger
detailed step
inspection
```

Execution schedule is deterministic control input, not gameplay authority.

## Controls

M042 compares real distinct controls, not labels around one implementation.

Abstract control uses only abstract execution. Mixed controls use M041 one-detailed-region switching. Detailed reference uses independent detailed controls for each non-interacting canonical region.

## Equivalence

Shared rule semantics remain exact. Timing/position approximation uses fixed predeclared execution-granularity bounds. Fixed-horizon completion differences are bounded only by current in-flight semantic work. No tolerance grows with switch count.

## Observer neutrality

Switch-frequency experiments hold total detailed exposure per region constant. This isolates segmentation/switching as the independent variable. High-frequency switching receives no larger semantic error budget.

## Persistence

Save authority remains stable gameplay/fidelity/continuation state. The authored execution schedule is re-supplied and fingerprint-validated on continuation. Same-schedule resumed versus uninterrupted results are exact.

## Long horizon

After the canonical 30-day proof, a separate deterministic 365-day, 5-region, 1000+ switch campaign validates that local M041 correctness does not accumulate global stale-state or semantic drift.

## Closure

M040–M042 together supersede the incomplete functional claims of historical M033 without rewriting its historical milestone/review record.
