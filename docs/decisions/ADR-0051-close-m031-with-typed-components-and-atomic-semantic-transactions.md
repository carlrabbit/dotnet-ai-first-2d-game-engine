# ADR-0051 — Close M031 with One Typed Component Authority and Atomic Semantic Transactions

## Status

Accepted for M039.

This revision incorporates corrective planning from PR 22. It supersedes any interpretation where `SimulationWorld` may own a private heterogeneous component-value store merely because those values are CLR objects rather than `JsonElement`.

## Context

M031 introduced the optional partitioned `SimulationWorld` used by later detailed and abstract simulation work.

Existing project authority already requires one runtime-owned entity/component universe, typed components, validated mutation, semantic command/event separation, activities and reservations as authoritative semantic state, executable persistence classification, and deterministic save/load continuation.

The pre-M039 implementation violated those contracts by storing simulation components in a nested JSON bag.

The first PR-22 correction replaced the JSON bag with:

```text
SimulationWorld.componentValues
    component-key
      -> entity-id
          -> object
```

while `EntityComponentWorld` continued to own only the `SimulationEntity` wrapper. That is still a second component universe.

PR 22 also exposed two related risks:

- mutable CLR component classes allow uncontrolled state mutation when returned by reference;
- assembly-qualified CLR names were beginning to participate in registration/persistence fingerprints even though CLR deployment identity is not durable gameplay identity.

M032's failed typed migration is evidence that the runtime needs a better heterogeneous infrastructure boundary, not evidence that simulation state should remain JSON-shaped or `SimulationWorld`-owned.

## Decision

### EntityComponentWorld is the sole component authority

`EntityComponentWorld` owns every authoritative runtime component instance.

`SimulationWorld` owns simulation semantics but no parallel component-value store.

Forbidden:

```text
SimulationWorld -> Dictionary<string, JsonElement> authoritative values
SimulationWorld -> Dictionary<string, object> authoritative values
any equivalent shadow component store
```

### Typed domain surface plus type-erased infrastructure surface

The runtime supports two views over the same stores:

1. generic typed domain operations such as registration/get/set/query;
2. bounded type-erased infrastructure operations needed for persistence, inspection and heterogeneous transaction staging.

Registration remains explicit. No dynamic plugin or assembly scanning is introduced.

A runtime component descriptor resolves stable type ID, CLR type, owner, validator and canonical codec. The concrete descriptor/API is implementation-owned.

### Durable identity is not CLR deployment identity

Stable component key and schema/codec metadata define durable identity.

CLR type is runtime binding metadata.

Assembly-qualified names, assembly versions, paths and similar deployment details are excluded from canonical persistence identity and registration fingerprints.

Load resolves a persisted stable key through the current explicit descriptor registry.

### Authoritative components are immutable/read-only

Runtime component reads cannot provide an uncontrolled mutation path.

Authoritative components are immutable values or are defensively copied so changing a returned object cannot modify stored state.

Mutation occurs only through approved runtime/semantic command boundaries.

### Heterogeneous batch ownership

`EntityComponentWorld` supplies bounded heterogeneous component staging/commit semantics so a semantic command can update multiple component types atomically.

All mutations validate before any become visible.

Sequential live mutation plus best-effort rollback is rejected as the atomicity model.

### Semantic transaction ownership

`SimulationWorld` coordinates domain transactions spanning typed ECS component batches, activities, reservations, lifecycle/region state, sequence/causal state and factual events.

A factual event is published only after the complete semantic transition commits.

### JSON is a boundary projection

JSON is permitted for canonical encoding, inspection/artifacts, authored data and CLI boundaries.

Gameplay logic does not use JSON property lookup as its authoritative component API.

A JSON inspection projection is derived from typed ECS state.

### Consumer migration

M032 and M033 migrate coherently to typed creation, reads, mutation and persistence.

The migration does not redesign component granularity without a separate project-level reason.

Current M032 deterministic behavior remains the semantic target. Regressions from the failed PR-22 typed experiment are fixed, not bypassed by reverting to JSON.

Direct M035 `SimulationWorld` fixtures adapt as needed. M034 remains outside this decision.

### Persistence

The existing M039 compatibility decision remains:

```text
SimulationWorld v2 is current/minimum.
SimulationWorld v1 is rejected.
M033 multi-fidelity v2 embeds SimulationWorld v2.
```

Persisted component identity is stable component key plus schema/codec metadata and canonical payload. CLR assembly identity is not persisted compatibility authority.

### Evidence integrity

M039 evidence must establish claims from observed predicates.

The pre-correction PR-22 M039 receipts do not establish milestone completion because `typed-component-authority` still used a shadow store and some evidence fields were constant summaries rather than tested predicates.

All M039 validation receipts are regenerated after the corrected architecture is implemented.

## Consequences

Positive:

- one real component universe;
- heterogeneous persistence, inspection and transactions no longer require a `SimulationWorld` shadow store;
- domain code receives typed component APIs;
- ECS storage may evolve later without changing semantic contracts;
- save compatibility is stable across CLR assembly renames/version changes;
- M032/M033 become typed-runtime consumers rather than JSON adapters;
- evidence can mechanically detect a second component authority.

Costs:

- `EntityComponentWorld` needs bounded descriptor and heterogeneous-batch infrastructure;
- M032/M033 migration is broader than replacing registration metadata;
- mutable proof DTOs must be replaced or safely copied;
- current PR-22 component implementation must be refactored rather than patched incrementally.

## Rejected Alternatives

### Keep `SimulationWorld.componentValues`

Rejected because it is a second component universe and violates ADR-0016/M013 authority regardless of whether values are `JsonElement` or CLR objects.

### Keep JSON gameplay reads while only storage becomes typed

Rejected because domain logic remains coupled to serialization shape and the migration is not complete.

### Put assembly-qualified type names in persistence/fingerprints

Rejected because deployment identity is not stable gameplay/save identity.

### Make every component tiny/archetype-oriented during M039

Rejected because M039 is semantic/authority closure, not an ECS performance redesign.

### Sequentially mutate and compensate on failure

Rejected because observers can see partial mutation and rollback itself can fail or emit invalid evidence.

### Replace `EntityComponentWorld` entirely

Rejected because the current storage architecture is sufficient once its heterogeneous infrastructure boundary is completed.

## Relationship to Existing Authority

ADR-0016 remains authoritative: runtime owns entity identity and typed components.

The entity-component runtime contract defines the underlying typed runtime.

The simulation-world contract defines higher-level semantic authority.

M039 closes the gap between them rather than creating a second runtime.
