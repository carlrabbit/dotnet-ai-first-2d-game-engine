# ADR-0051 — Close M031 with Typed Components and Atomic Semantic Transactions

## Status

Accepted for M039.

## Context

M031 introduced the optional partitioned SimulationWorld used by later detailed and abstract simulation work.

The durable project contracts already state that:

- runtime owns one entity identity space and typed component instances;
- game-defined components are typed and explicitly registered;
- semantic commands validate, stage and atomically commit mutation;
- factual events are emitted after commit;
- activities and reservations are authoritative semantic state;
- persistence classifications control save authority;
- fresh-process persistence is demonstrated by a real fresh-process continuation.

The current implementation diverged from those contracts in several material ways:

- SimulationWorld stores one `SimulationEntity` runtime component that itself owns a `string -> JsonElement` component dictionary;
- simulation registration metadata does not bind authoritative game state to the typed runtime stores;
- multi-component domain changes can be applied as separate live component writes followed by a separate `RecordFact`;
- activity stage transitions are largely caller-convention strings rather than shared validated policy;
- reservation capacity is trusted from caller input instead of derived from authoritative subject state;
- persistence classifications are not enforced by canonical capture;
- current v1 persistence serializes the nested JSON component bag;
- M031 evidence can label an in-process object round trip as a fresh-process proof.

These are architectural/semantic contract gaps, not merely missing tests.

## Decision

### One component authority

`EntityComponentWorld` remains the sole authoritative runtime owner of entity identities and component instances.

SimulationWorld is a semantic composition layer over that authority.

Game-defined simulation components are real typed CLR/runtime components with stable durable keys and explicit registration metadata.

A nested string-keyed JSON component bag is not authoritative simulation state after M039.

JSON may remain a boundary representation for serialization, artifacts, authored data and similar interfaces.

### Entity identity

Simulation code uses the repository's existing stable `EntityId` semantics for authoritative entity references.

Simulation-specific region membership and active/inactive semantics may be represented by typed simulation state and derived indexes without creating a second entity universe.

### Atomic semantic command boundary

One semantic command is one atomic authoritative transition.

A command can stage component, activity, reservation, lifecycle/region, sequence and event changes.

It commits all required authoritative changes and factual events together, or none.

The exact staging/transaction mechanism remains an implementation choice.

A factual event cannot be asserted independently of the semantic mutation that made it true.

### Command/event causality

Command IDs, event IDs, correlation and causation are real deterministic runtime semantics.

A successful command result identifies the events it emitted.

Factual events inherit real command causal context rather than milestone-proof constants.

### Activity and reservation authority

Activity kinds enforce valid transition policy through shared simulation/game authority.

Reservation capacity, availability and subject guards are derived from authoritative typed state through registered policy.

Terminal activities cannot retain active reservations.

Entity destruction cannot leave invalid active semantic references or a save that the same runtime rejects on load.

### Persistence classifications

Persistence classifications are executable save policy.

Authoritative state persists. Derived/transient/presentation/external state is omitted from canonical SimulationWorld authority according to the M031 contract.

Canonical fingerprints cover authoritative semantics rather than omitted execution/presentation state.

### Compatibility break

The corrected SimulationWorld persistence schema is:

```text
agentic2d.simulation-world-save.v2
```

and the minimum supported schema is v2.

The v1 SimulationWorld save is rejected rather than migrated.

The current M033 envelope that embeds SimulationWorld persistence becomes:

```text
agentic2d.multi-fidelity-save.v2
```

and validates nested SimulationWorld v2 state.

This is allowed because the current simulation project is not a published package/release compatibility promise, and v1 directly encodes the architecture being removed.

Generated validation artifacts are regenerated. Historical Git and completed review records are not rewritten.

### Current consumer migration

M039 updates all current in-repository consumers that directly rely on the flawed M031 component/mutation model, including M031, M032, M033 and direct M035 fixtures.

M034's independent settlement-state model is not migrated into SimulationWorld by this decision.

### Evidence integrity

A capability claim in generated machine evidence must be derived from executed observations.

"Fresh process" requires separate process invocations observed by the runner. A scenario writer cannot establish it by emitting a constant boolean.

## Consequences

Positive:

- detailed and abstract execution converge on one real semantic mutation boundary;
- game-defined state uses the same typed runtime ownership model as the rest of the engine;
- storage can later be optimized without changing simulation semantics;
- reservation and activity invariants become enforceable centrally;
- persistence classifications become meaningful;
- save/load equivalence evidence becomes trustworthy;
- later ECS/performance work is no longer coupled to a nested JSON object model.

Costs:

- M031/M032/M033 and direct M035 code must be adapted;
- unreleased internal simulation APIs may break;
- current v1 generated saves are incompatible and must be regenerated;
- transaction/registration support may require bounded changes in the core runtime.

## Rejected alternatives

### Keep the JSON bag but add typed wrappers

Rejected because the bag would remain the real authority and the existing typed ECS would still be bypassed.

### Preserve v1 with a compatibility adapter

Rejected because v1 serializes the architecture being removed and no current release promise requires preserving it.

### Treat the gaps as test-only problems

Rejected because the current source semantics themselves violate the typed-component, atomic-command, reservation and persistence contracts.

### Replace the ECS while closing M031

Rejected because storage architecture is not the problem being solved and would multiply risk without semantic benefit.

### Migrate M034 at the same time

Rejected because M034 needs its own integration audit. Folding it into foundation closure would make M039 unbounded.

## Relationship to Existing Authority

ADR-0016 remains authoritative: runtime owns entity identity and typed components.

The M031 simulation-world specification and architecture remain desired semantic authority.

The M035 save-compatibility contract remains the general policy for future compatibility boundaries.

M039 is a corrective closure milestone, not a repudiation of the one-world, semantic-time, activity/reservation, detailed/abstract convergence model.
