# Region Fidelity and Reconciliation Contract

## Authority

Authoritative for M041 region fidelity, executor ownership, transition handoff, materialization, abstraction, atomicity, rollback and stable transition persistence.

M040 is authoritative for the real detailed and abstract executors being connected. Shared gameplay semantics remain authoritative in SimulationWorld and the shared work/logistics/needs contract.

## Stable fidelity

```text
Detailed
Abstract
```

Exactly one participating region is detailed at every stable M041 boundary. All other participating regions are abstract.

## Ownership

Each region and active executor continuation has exactly one current executor owner.

Detailed execution commits only under current detailed ownership epoch. Abstract trigger delivery commits only under current abstract ownership epoch.

Mixed or ownerless stable continuation is invalid.

## Paired switch

The canonical switch atomically changes:

```text
A Detailed + B Abstract
→
A Abstract + B Detailed
```

Both conversions are one transaction.

## Semantic invariance

Fidelity change alone does not change identities/lifecycle, resources, inventory, storage, needs, designations, activities, reservations, semantic destinations, or gameplay factual events.

Executor continuation changes are orchestration state.

## Transition state

Internal lifecycle is equivalent to:

```text
stable
preparing
reconciling
validating
committing
stable
```

Failure before commit returns to the full prior stable state. Only one transition is active at once.

## Handoff

The source executor supplies immutable transition-only continuation information including applicable activity/stage/revision, actor, destination, phase, source revision, location/progress, remaining duration, mapping/spatial/graph guards and route/trigger references.

Handoff data is staging, not a second authority.

## Epoch fence

Transition preparation fences new commits for both switching regions.

Late old-epoch routes, detailed steps and triggers become stale.

A due trigger not committed before the fence is invalidated and reconciled into target continuation; it does not also deliver.

## Detailed to abstract

Preserve shared semantics.

Convert detailed-only continuation:

- exact position;
- route/destination progress;
- current interaction remaining duration;
- spatial/mapping revision;

into abstract node/edge location, coherent monotonic progress, abstract route/duration continuation and one guarded next trigger.

Carried inventory remains shared state. Switching does not complete work or needs.

## Abstract to detailed

Preserve shared semantics.

Invalidate old abstract triggers.

Map abstract node/edge/progress through explicit mapping metadata to a deterministic valid detailed position. Rebuild detailed route to the existing semantic destination.

Translate remaining interaction duration into detailed continuation without completing the interaction.

If no valid materialization exists, reject and retain prior abstract ownership.

## Mapping

Each region has stable revisioned mapping metadata between abstract graph nodes/edges and detailed areas/cells.

Materialization candidate selection is deterministic.

Bounded repair may choose another valid cell only within declared mapping scope and cannot produce semantic arrival/completion or economic gain.

Mapping revision mismatch rejects or stales preparation.

## Atomic commit

Prepare before mutating live authority:

- both source handoffs;
- both target continuations;
- queue cancellations/additions;
- detailed route/position changes;
- fidelity and execution epochs.

Validate the complete prepared result, then commit together.

Live sequential mutation plus compensating rollback does not satisfy this contract.

## Rollback

Injected or natural failure before commit leaves unchanged:

- semantic fingerprint;
- fidelity/owner/epoch;
- queue;
- detailed continuation;
- abstract continuation;
- activity/reservation state.

## Reservations

Reservations survive successful switching unchanged unless ordinary gameplay semantics changed them before transition began.

Switching does not release/reacquire reservations solely to change executor.

## Persistence

Canonical save contains stable fidelity/continuation only.

Never serialize a half transition as a valid stable world.

Save during active transition must deterministically finish/rollback before capture or reject/defer with a stable diagnostic.

M041 validates fresh-process continuation from stable checkpoints immediately before and immediately after a switch.

## Diagnostics

```text
FIDELITY-STATE
FIDELITY-OWNER
FIDELITY-EPOCH
RECONCILE-HANDOFF
RECONCILE-MATERIALIZE
RECONCILE-ABSTRACT
RECONCILE-POSITION
RECONCILE-PROGRESS
RECONCILE-TRIGGER
RECONCILE-ROUTE
RECONCILE-REFERENCE
RECONCILE-ROLLBACK
RECONCILE-PERSISTENCE
```

## Exclusions

No multiple detailed regions, physical cross-region movement, cross-region hauling, seamless visual streaming, long-horizon equivalence tolerances, observer neutrality, or persistence of intentionally half-completed transition state.
