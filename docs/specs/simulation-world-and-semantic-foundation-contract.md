# Simulation World and Semantic Foundation Contract

## Authority

This document is authoritative for the reusable simulation-world substrate introduced by M031.

It extends existing runtime, entity/component, behavior, spatial, inspection, and persistence authority. Where this contract is more specific about simulation-world concepts, this contract governs. It does not authorize the later abstract discrete-event executor or multi-fidelity region transitions.

## Purpose

Provide one authoritative, persistent, deterministic world that can be executed by current and future simulation strategies without duplicating game rules.

```text
authoritative world state
→ validated commands
→ atomic mutation
→ factual domain events
→ deterministic inspection and persistence
```

## Ownership boundaries

### Runtime owns

- world, region, entity, activity, reservation, command, and event identity;
- authoritative simulation clock;
- component stores and validated mutation;
- activity and reservation state;
- deterministic ordering;
- persistence and restoration;
- canonical inspection and fingerprints.

### Game modules own

- game-defined component types;
- domain command and event types;
- activity kinds and valid stage graphs;
- domain validation and state-transition rules;
- bounded proof fixtures.

### Behavior modules own

- immutable snapshot evaluation;
- emitted intents or commands;
- no direct mutation of authoritative stores.

### Spatial modules own

- interpretation of compatible spatial components and static world data;
- no entity identity or lifecycle authority.

### Rendering and presentation own

- read-only projections and transient presentation state;
- no simulation mutation or time advancement.

### Future optional execution subsystems may own

- scheduled-trigger queues;
- abstract duration models;
- detailed or abstract activity execution progress;
- region fidelity orchestration.

They must converge through this contract's shared commands, activities, reservations, and factual events.

## Identity

Use stable typed IDs. Required families:

```text
WorldId
RegionId
EntityId
ActivityId
ReservationId
SimulationCommandId
SimulationEventId
CorrelationId
CausationId
```

IDs must:

- be serializable canonically;
- never depend on filename, list position, component-store index, or presentation identity;
- remain stable across save/load;
- produce explicit duplicate diagnostics;
- not be silently reused after destruction.

## One authoritative world

M031 uses one authoritative simulation world with explicit region partitions.

A region is a durable logical partition, not an independent engine instance.

An entity is one of:

```text
region-owned
world-scoped
```

Region-owned entities have exactly one `RegionId`. World-scoped entities require an explicit registered classification.

Forbidden states:

- one entity owned by multiple regions;
- region-owned entity without a region;
- silent destruction when a region becomes inactive;
- cross-region component query returning duplicate entity identity;
- region transfer implemented as destroy-and-recreate with a new identity.

## Component registration

Game-defined component registration must be explicit or generated and deterministic.

Every persistent component family has:

- stable component key;
- current schema version;
- CLR/runtime type binding;
- persistence classification;
- serializer/deserializer or canonical codec;
- optional migration handlers when supported;
- inspection projection.

Persisted component identity must not use assembly-qualified type names as durable authority.

Registration order must not alter:

- component keys;
- query order;
- serialized output;
- canonical fingerprints;
- command or event outcomes.

Duplicate keys and incompatible registrations fail before world mutation.

## Persistence classification

Every runtime component or domain state family is classified as one of:

| Classification | Meaning | Save behavior |
|---|---|---|
| `authoritative-persistent` | Required to preserve gameplay semantics | Stored canonically |
| `derived-rebuildable` | Deterministically reconstructed from authoritative state | Omitted or stored only as validated cache |
| `active-mode-transient` | High-frequency detailed execution state | Omitted unless an explicit continuation contract exists |
| `presentation-only` | Rendering/audio/UI state | Never gameplay authority |
| `external-handle` | Native/process/file-system handle | Never persisted |

Classification must be inspectable.

## Lifecycle

Required lifecycle states or equivalent semantics:

```text
created
active
inactive
destroyed
```

Required operations:

```text
create entity
activate entity
deactivate entity
transfer region
destroy entity
```

Each operation:

- validates the current lifecycle state;
- applies atomically;
- emits factual events only after commit;
- produces stable diagnostics on failure;
- invalidates or resolves affected activities/reservations according to registered domain rules;
- preserves entity identity during region transfer.

Destroyed identity is tombstoned or otherwise protected from silent reuse within the authoritative world lineage.

## Region queries

Region-filtered queries must:

- use stable deterministic entity order;
- respect lifecycle visibility;
- return each entity at most once;
- distinguish world-scoped inclusion explicitly;
- not depend on dictionary order;
- remain read-only;
- support canonical inspection.

Named domain query services may compose component and region queries with policy, but raw query access must not spread uncontrolled mutation authority.

## Component change observations

The runtime may emit internal component-change observations for:

- index invalidation;
- derived projection updates;
- diagnostics;
- future scheduler integration.

These observations are not domain events unless game semantics explicitly promote them.

They must not:

- mutate authoritative state recursively outside approved phases;
- become the public gameplay history by accident;
- leak transient implementation addresses or handles.

## Simulation time

### Types

`SimulationInstant` represents an absolute point on the simulation timeline.

`SimulationDuration` represents a non-negative or explicitly signed duration according to the final implementation contract. Arithmetic must be checked and deterministic.

Canonical representation must:

- avoid culture-sensitive serialization;
- avoid floating-point ambiguity for authoritative ordering;
- state resolution explicitly;
- reject overflow and unsupported negative values.

### Clock

One authoritative simulation clock owns `Now`.

Wall-clock time and render frame time are not authoritative simulation time.

The existing fixed-tick runtime may advance the clock through current phases. Later optional systems may advance it differently, but all authoritative mutation observes one coherent timeline.

### Ordering

When multiple operations share the same simulation instant, deterministic ordering uses a stable explicit key. At minimum, ordering must account for:

```text
simulation instant
phase
sequence or stable command/event ID
```

Do not rely on task scheduling, thread timing, hash iteration, or wall-clock timestamp.

## Commands

A command requests one validated state transition.

A command envelope contains or resolves:

- command ID;
- command type key;
- issued simulation instant;
- correlation ID;
- causation ID where applicable;
- actor/source;
- target references;
- expected revisions or guards;
- typed payload.

Command handling:

1. reads the current authoritative state;
2. validates identity, lifecycle, region, revision, reservations, and domain preconditions;
3. stages all mutation;
4. commits atomically;
5. emits factual domain events after commit;
6. produces a structured command result.

Failure:

- commits no partial mutation;
- emits no factual success event;
- preserves stable diagnostics;
- may emit a diagnostic/failure result that is not represented as a completed gameplay fact.

## Domain events

A domain event records an authoritative fact that has completed.

Event envelope contains or resolves:

- event ID;
- event type key;
- simulation instant;
- correlation and causation IDs;
- affected identities;
- typed payload;
- deterministic sequence.

Examples:

```text
EntityCreated
EntityTransferredRegion
ActivityStarted
ActivityStageChanged
ReservationAcquired
ReservationReleased
ResourceHarvested
ResourceDeposited
```

Events are not mutation requests.

Event consumers may update derived projections or issue later commands through approved boundaries. They must not mutate stores directly.

M031 does not make event sourcing the authoritative world model.

## Scheduled-trigger extension boundary

A scheduled trigger is a future input that a later optional subsystem may deliver.

M031 defines only the semantic boundary:

```text
due simulation instant
trigger ID
owner activity/entity
expected revision
typed trigger kind
causal reference
```

When delivered later, a trigger must revalidate current authoritative state and normally issue a command.

M031 does not define:

- priority-queue implementation;
- catch-up algorithm;
- event coalescing;
- cancellation data structure;
- lazy continuous integration;
- abstract travel model.

## Activities

An activity represents mode-independent semantic work performed by an actor.

Required state:

```text
activity ID
actor entity ID
activity kind key
current stage key
target references
start instant
last transition instant
stage progress
revision
status
correlation/causation
interruption or cancellation reason
completion result
```

Statuses include equivalent semantics for:

```text
planned
active
interrupted
cancelled
completed
failed
```

Activity stages form a registered valid transition graph per activity kind.

Stage transitions:

- occur through validated commands;
- update revision monotonically;
- reject stale expected revisions;
- preserve causal evidence;
- never complete twice;
- release or transfer reservations according to domain rules.

Stage progress is semantic, not presentation animation progress.

When a coordinator accepts work that requires initial reservations, it must atomically revalidate the candidate, create the activity, and acquire every initial reservation. A conflict or stale candidate leaves no created activity or partial reservation behind.

## Reservations

A reservation protects a contested entity, resource quantity, or capacity while an activity is active.

Required state:

```text
reservation ID
owner activity ID
reserving entity ID
subject reference
reservation kind
quantity/capacity
acquired instant
revision or subject version guard
status
release reason
```

Required semantics:

- deterministic conflict resolution;
- no negative or excess reservation;
- atomic acquisition;
- idempotent release;
- stale expected revision fails safely;
- destruction or invalidation of the subject produces explicit resolution and diagnostics;
- save/load preserves active reservations;
- completed or cancelled activities cannot retain leaked reservations.

Reservations are authoritative simulation state. A cache of available quantity is derived unless explicitly defined otherwise.

## Persistence envelope

Provide a versioned canonical save envelope containing:

- schema family and version;
- world identity;
- simulation instant;
- deterministic sequence state;
- regions;
- entities and persistent components;
- activities;
- reservations;
- required random-stream state;
- compatibility metadata;
- canonical fingerprint inputs.

Serialization order must be explicit.

Load:

- validates the complete envelope before commit;
- resolves registered type keys;
- validates referential integrity;
- validates lifecycle, region, activity, and reservation invariants;
- reconstructs derived state;
- commits transactionally into an empty or explicitly replaceable world;
- emits a structured load report.

Unknown incompatible schema versions and unknown required component keys fail clearly.

Atomic save replacement must preserve the previous valid save on failure where supported by repository policy.

## Canonical fingerprint

The canonical world fingerprint covers authoritative semantic state, not:

- generated artifact paths;
- process IDs;
- wall-clock timestamps;
- native handles;
- dictionary iteration order;
- presentation-only state;
- advisory timing measurements.

Equivalent direct and save/load continuations must produce the same final fingerprint in the M031 proof.

## Inspection

Inspection must provide bounded machine-readable projections for:

- world and region summaries;
- entity identities, lifecycle, region, and component keys;
- component persistence classifications;
- activities, stages, revisions, and causal references;
- reservations, quantities, status, and release reason;
- simulation time and ordering state;
- command results and domain events;
- invariant violations;
- canonical fingerprint.

Inspection is read-only and cannot advance the clock or mutate state.

## Determinism and conservation

The same authored input, seed, registration set, and command sequence must produce:

- the same command outcomes;
- the same event sequence;
- the same authoritative final state;
- the same canonical fingerprint.

The wood proof must establish:

```text
initial wood + harvested wood
= carried wood + stored wood + otherwise explicitly accounted wood
```

No resource may duplicate or disappear through command failure, reservation conflict, save/load, or repeated completion.

## Diagnostics

Use stable diagnostic codes and structured context.

Required categories:

```text
SIM-WORLD
SIM-REGION
SIM-COMPONENT
SIM-TIME
SIM-COMMAND
SIM-EVENT
SIM-ACTIVITY
SIM-RESERVATION
SIM-PERSISTENCE
SIM-INVARIANT
```

Diagnostics must identify relevant stable IDs and expected/actual revisions without requiring raw logs.

## Compatibility and extension

M031 must preserve current runtime and consumer behavior.

Future milestones may add:

- autonomous work coordination;
- detailed activity executors;
- discrete-event scheduling;
- multi-fidelity regions;
- environmental infrastructure.

They must extend this contract rather than bypass it.

## Explicit exclusions

This contract does not authorize:

- archetype/sparse-set storage rewrite;
- multithreaded simulation;
- dynamic runtime plugin discovery;
- one world per region;
- discrete-event queue implementation;
- pathfinding;
- rendering mutation;
- event-sourced authoritative state;
- game UI or M030 asset consumption.
