# Region Fidelity and Reconciliation Contract

## Authority

Authoritative for M033 region fidelity, executor ownership, materialization, abstraction, and rollback.

## Fidelity

```text
detailed
abstract
```

Exactly one M033 region is detailed. Fidelity is authoritative persistent state.

## Ownership invariants

Each region and active activity stage has one executor owner. Detailed systems update only detailed regions; abstract triggers deliver only for abstract regions. Transitions are serialized. Mixed ownership is forbidden.

## Transition state

```text
stable
preparing
reconciling
validating
committing
failed
```

Half-committed transition state is never persisted as valid authority.

## Abstract to detailed

Transfer entities, identities, activities, progress, revisions, reservations, inventory, resources, storage, needs, abstract location/progress, and causality. Invalidate/transfer triggers. Materialize deterministic valid grid positions and rebuild detailed routes.

## Detailed to abstract

Discard routes/interpolation/presentation. Preserve and convert authoritative position, destination, progress, remaining duration, carried inventory, reservations, needs, and revisions. Schedule the next guarded trigger only after successful reconciliation.

## Plausible position rule

Materialized positions must be deterministic, in bounds, reachable or explicitly repaired, consistent with abstract progress, economically neutral, and artifacted. Exact hypothetical route reproduction is not required.

## Rollback

Failure returns to prior stable fidelity with prior ownership and valid queue/route state. No duplication.

## Persistence

Persist stable fidelity and completed transition revision. Never persist a mixed transitional state as complete.

## Diagnostics

```text
FIDELITY-STATE
FIDELITY-OWNER
RECONCILE-MATERIALIZE
RECONCILE-ABSTRACT
RECONCILE-POSITION
RECONCILE-TRIGGER
RECONCILE-ROLLBACK
```

## Exclusions

No multiple detailed regions, seamless rendered streaming, physical inter-region movement, or exact trace equality.
