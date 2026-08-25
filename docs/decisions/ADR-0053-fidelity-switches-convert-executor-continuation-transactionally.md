# ADR-0053 — Fidelity Switches Convert Executor Continuation Transactionally

## Status

Accepted for M041.

## Context

M040 establishes two real execution strategies over one shared semantic world.

Historical M033 switching mostly flipped fidelity metadata and cancelled triggers. Real executor continuation includes detailed routes/positions/progress or abstract graph/duration/trigger state, so ownership transfer must reconcile continuation without creating a second gameplay mutation system.

## Decision

### Gameplay semantics remain unchanged

A fidelity switch does not itself harvest resources, move inventory, satisfy needs, complete activities, release/reacquire reservations, or otherwise change gameplay meaning.

Shared semantic state remains in SimulationWorld/runtime authority. Only executor continuation and fidelity orchestration change.

### Paired atomic switch

Stable bounded M041 state has exactly one detailed region.

Switching the detailed region is one atomic paired transaction:

```text
old detailed -> abstract
target abstract -> detailed
```

No stable zero-detailed or dual-detailed state is valid.

### Immutable handoff staging

Each source executor can produce immutable transition handoff data describing current continuation. Handoff data is staging only, not persistent gameplay authority.

### Execution epoch fencing

Each region/owner has a revision or equivalent epoch guard.

Transition preparation fences both regions. Old-epoch detailed steps and abstract triggers cannot commit after ownership changes. Due-but-undelivered triggers are reconciled rather than delivered twice.

### Prepare then commit

Source extraction, target continuation, scheduler changes, route changes and ownership changes are staged and fully validated before live commit.

Sequential live changes plus compensating rollback are rejected.

### Spatial reconciliation

Explicit revisioned mapping metadata connects abstract node/edge space with detailed area/cell space.

Detailed→abstract preserves coherent travel progress.

Abstract→detailed deterministically materializes and rebuilds a detailed route.

Bounded repair cannot create semantic completion or economic advantage.

### Interaction progress

In-progress timed interactions preserve remaining semantic duration across the switch. Conversion does not execute the pending completion command.

### Stable persistence only

Canonical saves contain stable pre- or post-transition state. Half-transition preparation is not normal persisted authority.

M041 validates fresh-process continuation immediately before and after successful switching.

### Corrective staging

M041 proves local switch correctness only. M042 separately proves long-horizon equivalence, observer neutrality and scale.

## Consequences

Positive:

- semantic gameplay remains single-authority;
- stale source work is mechanically fenced;
- rollback is testable as equality to the prior stable boundary;
- M042 can treat each switch as a meaningful already-proven operation.

Costs:

- scheduler and detailed continuation storage may need bounded staging support;
- explicit spatial mapping metadata is required;
- progress conversion and fault injection add validation infrastructure.

## Rejected Alternatives

### Flip ownership metadata and repair afterward
Rejected because queue/route work can commit under mixed ownership.

### Recreate semantic activities/reservations on switch
Rejected because executor choice must not change gameplay authority.

### Persist transition-in-progress state
Rejected for M041 because it expands recovery semantics without benefit to the current bounded capability.

### Run an old due trigger and also reconstruct target continuation
Rejected because it permits duplicate semantic completion.

### Prove only final resource totals
Rejected because local ownership/rollback defects can remain hidden until long-horizon runs.
