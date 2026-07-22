# Multi-Fidelity Simulation Architecture

## Shape

```text
Game work/logistics/needs semantics
               │
               ▼
M031 authoritative foundation
               │
       ┌───────┴────────┐
       ▼                ▼
M032 detailed       M033 abstract
executor            discrete-event executor
       └───────┬────────┘
               ▼
M033 region fidelity and reconciliation
               ▼
persistent world orchestration
```

## Placement

Conceptual families: discrete events, abstract execution, multi-fidelity reconciliation, and equivalence. Use existing projects where appropriate.

Discrete-event simulation is standalone-capable. Multi-fidelity integration depends on both executors and remains separate.

## Global orchestration

One world clock and command/event order remain authoritative. Region executors produce inputs to the same command pipeline.

## Transition ownership

Only the transition coordinator changes executor ownership. It serializes phases and validates a complete reconciled state before commit.

## Trigger lifecycle

Abstract triggers are linked to semantic activity revisions. Activation invalidates/transfers region triggers before detailed ownership begins.

## Materialization

Use abstract position/progress, stable mapping metadata, detailed map/occupancy, deterministic valid-cell projection, and detailed route rebuild.

## Abstraction

Use exact grid position, route goal, semantic progress, stable grid-to-area mapping, remaining duration, and next-trigger planning.

## Persistence

Save stable fidelity and executor continuation state. Rebuild derived queue indexes, pathfinder internals, and presentation.

## Equivalence harness

Runs complete worlds under controlled fidelity schedules and compares semantic ledgers. It is not gameplay authority.

## Invariants

1. Shared rules never branch on executor implementation.
2. One world clock.
3. One owner per region/activity.
4. Trigger handlers use commands.
5. Transitions are transactional.
6. Position reconciliation is explicit.
7. Stale triggers cannot mutate.
8. Detailed pathfinding is not abstract travel.
9. Standalone host has no graphics dependency.
10. Divergence is measured, not concealed.
