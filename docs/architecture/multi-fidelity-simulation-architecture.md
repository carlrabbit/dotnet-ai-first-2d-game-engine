# Multi-Fidelity Simulation Architecture

## Corrective sequence

```text
M040 — two real executors over shared semantics
M041 — transactional executor-ownership reconciliation
M042 — mixed-fidelity equivalence and long-horizon proof
```

## Core shape

```text
                   shared semantic authority
              work / logistics / needs / facts
                          │
              ┌───────────┴───────────┐
              ▼                       ▼
      detailed executor        abstract executor
       grid + fixed-step         DES + durations
              │                       │
              └───────┬───────────────┘
                      ▼
             M041 transition coordinator
         handoff + mapping + staged commit
                      ▼
             stable target ownership
```

## M041 principle

Gameplay semantics do not move between executors. They already live in shared SimulationWorld/runtime authority.

The transition converts only source executor continuation into target executor continuation.

## Stable ownership

Exactly one region is detailed in the bounded M041 composition. Every stable active continuation has one current owner. Execution epochs fence old work.

## Transition transaction

A switch prepares both directions before commit:

```text
old detailed handoff
→ prepare abstract continuation

target abstract handoff
→ prepare detailed continuation

+ queue changes
+ route changes
+ owner/epoch changes
→ validate
→ atomic commit
```

Failure commits none.

## Spatial reconciliation

Explicit revisioned mapping connects abstract node/edge space with detailed area/cell space.

Detailed→abstract preserves coherent travel progress.

Abstract→detailed deterministically materializes and rebuilds route.

Mapping is orchestration/configuration authority, not gameplay state.

## Timed interactions

For harvest/pickup/deposit/eat/drink/rest/retry, remaining semantic duration crosses the executor boundary. Conversion does not execute completion.

## M042 boundary

M041 establishes local switch correctness.

M042 owns real mixed-fidelity long runs, distinct execution schedules, bounded timing tolerances, observer neutrality, repeated-switch scale and broad mixed-fidelity fresh-process equivalence.

Do not infer M042 readiness merely from a passing M041 switch.
