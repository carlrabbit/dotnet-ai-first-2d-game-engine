# Shared Work, Logistics, and Needs Semantics Contract

## Authority

Authoritative for executor-neutral autonomous work, logistics, fixed needs, assignment, interruption and semantic completion rules.

SimulationWorld/M039 remain authoritative for identity, typed component ownership, semantic time, activities, reservations, atomic transactions, persistence and factual events.

## Model

```text
semantic state
→ derive opportunities
→ evaluate/select
→ atomically assign/reserve
→ executor continuation
→ shared semantic command
→ committed state/event
→ derive again
```

The shared layer never branches on executor identity.

## Opportunities and selection

Equivalent semantic state produces equivalent opportunities and selection decisions regardless of executor.

Selection uses explicit mechanical state, priorities, reservation/capacity validity, typed reachability/cost input and stable tie-breaks.

It does not use wall clock, render state, personality, mood, hidden randomness or executor identity.

## Assignment

Assignment atomically revalidates the opportunity/worker, acquires required reservations, creates/starts the activity and emits factual assignment/start events after commit.

Failure commits nothing.

## Logistics

Harvest, pickup, inventory, carry and deposit are shared semantic operations.

Storage capacity is authoritative:

```text
available = capacity - stored - active capacity reservations
```

Caller constants do not override subject capacity.

Resource conservation is calculated from authoritative state.

## Fixed needs

Current kinds are food, water and comfort.

Need authority includes level, last integrated semantic instant, thresholds, satisfaction target and revision.

Equivalent initial state and target semantic instant produce equivalent integrated need state.

Mandatory need outranks ordinary work according to shared priority.

Interruption preserves/releases reservations according to semantic rule, preserves carried inventory, satisfies the need through shared commands, then re-derives work.

Detailed may integrate at fixed semantic checkpoints. Abstract may integrate lazily to scheduled thresholds.

## Reachability boundary

An executor supplies typed reachability/cost estimates to shared selection.

Shared semantics do not call detailed grid or abstract graph planners directly.

## Persistence

Authoritative designations, worker state, resources, inventory, storage, needs, activities and reservations persist under SimulationWorld v2.

Opportunities, candidate lists and cost estimates are derived/rebuildable.

Executor-specific route/search state is outside this contract.

## Invariant

A rule change in work/logistics/needs must not require separate detailed and abstract implementations.
