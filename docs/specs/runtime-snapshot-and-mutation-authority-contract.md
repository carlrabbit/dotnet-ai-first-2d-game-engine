# Runtime Snapshot and Mutation Authority Contract

## Authority

Authoritative for immutable typed runtime snapshots, generic component binding, canonical snapshot fingerprints, evaluator read boundaries, and atomic entity/component/provenance mutation transactions.

## Snapshot

A runtime snapshot is immutable, detached and phase-scoped. It exposes tick, fingerprint, entity existence/enumeration, typed lookup/query, stable-key lookup/query, and read-only component inspection over the same captured semantic state.

Later live mutation cannot alter a previous snapshot. Mutable values are defensively copied. Ordering is ordinal by stable IDs.

## Behavior boundary

All behaviors in one behavior phase receive the same snapshot. Behavior code receives no mutable runtime store. Domain/spatial evaluation consumes snapshot state plus authored/static inputs.

## Generic binding

Stable component ID is semantic identity. Generic CLR-type-only access is valid only when exactly one registered stable component family binds that CLR type. If several IDs share one CLR type, generic access rejects as ambiguous and stable-key access is required. Registration order never chooses semantic identity.

## Canonical fingerprint

Fingerprint derives from tick, stable entity IDs, stable component IDs, and canonical semantic encodings in ordinal order. It excludes assembly/process/path/time/allocation data and unordered-container insertion order.

## Runtime transaction

A runtime transaction may stage `CreateEntity`, `DestroyEntity`, `SetProvenance`, `SetComponent`, and `RemoveComponent`. All operations/preconditions validate before live mutation. One invalid operation rejects the whole transaction. Commit is one visible authority boundary. Rejection emits no factual success lifecycle/component events and leaves authoritative state unchanged.

## Evaluator command boundary

Resolvers/evaluators return domain results plus proposed runtime mutations. They do not call live mutation APIs while resolving. The execution coordinator deterministically submits accepted proposals for commit.

## Rejected evidence

Rejected command/transaction evidence preserves requested tick/instant and caller identity and includes the failing diagnostic. It never reports tick zero merely because the operation rejected.

## Higher-level semantics

`SimulationWorld` retains activities, reservations, tombstones, regions, domain events and higher-level transaction authority and may compose runtime transactions.
