# Entity Component Runtime Contract

## Authority

This document is authoritative for runtime entity identity, entity lifecycle, typed component ownership, component queries, immutable snapshots, and command-buffered component mutation.

It does not define a final ECS storage architecture.

## Entity model

An entity is:

```text
stable EntityId
+ zero or more typed components
```

An entity is not required to be a mutable object graph or derive from a universal `GameObject` base class.

The runtime owns:

- entity existence;
- entity lifecycle;
- component instances;
- component mutation;
- deterministic entity/component enumeration;
- snapshot creation;
- lifecycle and mutation events.

## Entity lifecycle

Required operations:

```text
CreateEntity
EntityExists
DestroyEntity
EnumerateEntities
```

Rules:

- IDs are stable and unique within one runtime world;
- duplicate creation is rejected;
- unknown-entity mutation is rejected;
- destruction removes all components;
- destruction is idempotent only if explicitly documented; default behavior should reject destroying an unknown entity;
- lifecycle operations produce structured command results and factual events.

Recommended diagnostics:

| ID | Meaning |
|---|---|
| `ENTITY0001` | Duplicate entity ID. |
| `ENTITY0002` | Entity not found. |
| `ENTITY0003` | Invalid entity ID. |
| `ENTITY0004` | Entity lifecycle command rejected. |

## Component registration and identity

Each component type has a stable type ID.

Initial IDs:

```text
component.grid-position
component.continuous-transform-2d
component.kinematic-motion-2d
component.collision-aabb-2d
component.spatial-membership
```

Registration is explicit. Runtime reflection scanning is not required.

The runtime may map stable type IDs to typed stores through hand-written registration.

## Component operations

Required semantics:

```text
SetComponent<T>
TryGetComponent<T>
RemoveComponent<T>
QueryEntitiesWith<T>
QueryEntitiesWith<T1, T2>
```

Rules:

- set requires an existing entity;
- first set emits component-added;
- replacement emits component-updated;
- remove emits component-removed;
- removing a missing component returns a stable rejected/no-op result according to one documented policy;
- component queries are deterministic by entity ID;
- component values are copied or exposed read-only;
- behavior code cannot receive mutable stores.

Recommended diagnostics:

| ID | Meaning |
|---|---|
| `COMPONENT0001` | Component type not registered. |
| `COMPONENT0002` | Invalid component value. |
| `COMPONENT0003` | Component incompatible with entity/module context. |
| `COMPONENT0004` | Component removal target missing. |
| `COMPONENT0005` | Component mutation rejected. |

## Storage policy

The initial implementation may use:

```text
typed dictionaries
typed arrays
small explicit store registry
```

It must not require:

```text
archetype ECS
sparse-set optimization
reflection dispatch
third-party ECS framework
```

Semantic contracts must not depend on the chosen storage representation.

## Snapshots

A snapshot is immutable and tick-scoped.

It exposes:

- entity existence;
- stable entity enumeration;
- typed component lookup;
- one- and two-component entity queries;
- current tick;
- deterministic fingerprint.

All behaviors in one phase receive the same snapshot.

A later mutation must not alter an existing snapshot.

## Snapshot fingerprint

Fingerprint input must include:

- tick;
- stable entity IDs;
- stable component type IDs;
- semantic component values;
- deterministic ordering.

Volatile process, timestamp, path, and allocation data are excluded.

SHA-256 lowercase hexadecimal is preferred.

## Mutation boundary

Behavior modules emit intents.

Domain modules resolve intents into accepted typed mutation commands or rejected domain results.

The runtime validates and applies accepted commands.

No behavior or spatial resolver may directly mutate component stores during intent evaluation.

## Determinism

Equivalent initial entity/component state, inputs, tick count, and source revision must produce equivalent:

- entity enumeration;
- component queries;
- snapshots;
- fingerprints;
- command ordering;
- lifecycle/component events;
- final entity/component state.
