# ADR-0016 — Runtime Owns Entities and Components; Spatial Modules Own Spatial Semantics

## Status

Proposed for Milestone 013. Accept when the milestone implementation is accepted.

## Context

The minimal runtime stores one integer position per entity, while the grid behavior slice currently uses specialized grid-position state. A continuous spatial module would otherwise introduce additional private dictionaries for transform, motion, and collision state.

That would create multiple entity universes, duplicate lifecycle logic, complicate snapshots and inspection, and make future health, inventory, animation, dialogue, and save-state work harder.

The project also needs a mixed world:

- grid-authored background and terrain;
- continuously moving player and NPC entities;
- large static authored objects such as trees;
- no mandatory general-purpose physics engine.

## Decision

The runtime owns:

- entity identity and lifecycle;
- typed component instances;
- component mutation;
- immutable snapshots;
- deterministic entity/component queries;
- lifecycle and component evidence.

Spatial modules own:

- spatial component types;
- intent interpretation;
- spatial queries;
- movement/collision semantics;
- domain resolution.

Maps own authored static spatial data. Static map content is projected into a static spatial world and is not automatically converted into runtime entities.

The grid module will migrate to runtime-owned component state.

The second spatial reference module is `spatial.continuous-kinematic-2d`, using finite double world coordinates, AABB collision, fixed-tick integration, and deterministic axis-separated response without rigid-body physics.

## Consequences

Positive:

- grid and continuous modules share one entity lifecycle and snapshot model;
- component storage can later be optimized without changing semantics;
- future non-spatial components use the same runtime foundation;
- static map objects remain cheap and distinct from mutable entities;
- continuous movement does not require a physics dependency;
- runtime inspection can expose one coherent world state.

Costs:

- entity/component mutation and snapshots become explicit;
- the grid implementation must be migrated;
- scenario initial-state validation becomes richer;
- component serialization and evidence require stable type IDs;
- continuous collision policy must be precisely documented.

## Rejected alternatives

### Spatial modules own private entity dictionaries

Rejected because it duplicates identity, lifecycle, snapshots, and inspection.

### Adopt a third-party ECS immediately

Rejected because storage performance and archetype design are not yet proven requirements.

### Convert every map object and tile into an entity

Rejected because most static authored content does not need runtime lifecycle or mutable state.

### Add general-purpose physics now

Rejected because fixed-tick kinematic AABB movement satisfies the current use case with less nondeterminism and complexity.

### Standardize one universal position component

Rejected because grid and continuous modules require different state and semantics.
