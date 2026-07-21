# Simulation Foundation Architecture

## Purpose

Define the implementation boundary for M031 without prescribing internal file layout.

## Context

The engine already owns runtime entity identity, typed components, validated mutation, commands/events, snapshots, behavior phases, spatial interpretation, scenarios, inspection, and persistence diagnostics.

The third game requires these capabilities to become one persistent partition-aware simulation substrate. Later milestones will add two execution strategies:

```text
detailed active-region execution
abstract discrete-event execution
```

M031 creates the shared semantic foundation only.

## Architectural position

```text
┌──────────────────────────────────────────────────────────────┐
│ Game modules                                                 │
│ component types, domain commands/events, activity stage rules│
└──────────────────────────────┬───────────────────────────────┘
                               │
┌──────────────────────────────▼───────────────────────────────┐
│ Optional first-class simulation foundation                   │
│ world/region model, semantic time, activities, reservations, │
│ deterministic ordering, persistence, inspection              │
└──────────────────────────────┬───────────────────────────────┘
                               │
┌──────────────────────────────▼───────────────────────────────┐
│ Existing engine runtime                                      │
│ entities/components, mutation boundaries, snapshots,         │
│ commands/events, scenarios, diagnostics                      │
└──────────────────────────────────────────────────────────────┘
```

The capability must run headlessly and may be installed explicitly by games that need it. Games that do not install it retain the existing runtime surface.

## Dependency rule

```text
existing engine contracts
← simulation foundation
← game simulation modules
← later detailed/abstract executors
```

Forbidden dependency:

```text
shared game rule
→ future discrete-event queue
```

A shared harvest completion rule cannot branch on whether detailed or abstract execution produced the completion command.

## One world, explicit partitions

Use one authoritative world.

```text
World
├── world-scoped entities
├── Region A
│   └── region-owned entities
├── Region B
│   └── region-owned entities
└── semantic simulation clock
```

Region partitions are indexes and authority constraints over one identity space.

This avoids premature problems with:

- cross-world identity;
- resource transfer;
- activity ownership;
- reservation references;
- persistence transactions;
- future region fidelity transitions.

## Recommended internal modules

Names are conceptual.

```text
Simulation.Foundation.Contracts
  identities
  time
  command/event envelopes
  activity and reservation contracts
  persistence/inspection schemas

Simulation.Foundation.Runtime
  world composition
  registration
  lifecycle
  region indexes
  deterministic phases
  activity/reservation stores
  persistence and fingerprints

Simulation.Foundation.Validation
  invariants
  artifact validators
  scenario assertions

Game.M031.Proof
  worker/tree/storage components
  harvest/deposit commands
  bounded stage driver
```

The implementation may place these within existing projects when that better preserves repository architecture. Do not create assemblies merely to mirror this diagram.

## Runtime phase model

M031 must use explicit deterministic phases. A compatible conceptual sequence:

```text
1. ingest authored/external commands
2. snapshot or query current state
3. validate and resolve commands
4. stage mutation
5. atomically commit mutation
6. publish factual domain events
7. update derived indexes/projections
8. validate invariants and emit artifacts
```

Behavior evaluation remains consistent with existing behavior runtime phases.

Do not infer a generalized scheduler from component read/write sets.

## Activity model

Activities are explicit domain state, not implicit chains of marker components.

```text
Activity
├── semantic intent
├── actor and targets
├── current stage
├── progress
├── revision
├── status
└── reservations
```

M031 stage advancement is command-driven by a deterministic proof driver.

Later:

```text
detailed executor
→ stage/progress commands

abstract executor
→ scheduled triggers
→ stage/progress commands
```

Both converge on the same command handlers.

## Reservation model

Reservations are cross-system concurrency control for simulation semantics, even before multithreading exists.

They guard:

- exclusive target ownership;
- resource quantity;
- storage or processing capacity.

The reservation subsystem should expose domain-oriented operations rather than raw mutable collections.

Subject availability is calculated from authoritative quantity/capacity minus active reservations.

## Change observations versus domain events

```text
component changed
  internal invalidation/inspection signal

ResourceDeposited
  factual gameplay event
```

Do not publish every component write as a domain event.

Do not let internal observations become an unrestricted reactive mutation pipeline.

## Persistence model

Use canonical state persistence, not event sourcing.

```text
versioned save envelope
├── world and regions
├── entities and persistent components
├── clock and deterministic sequencing
├── activities
├── reservations
└── compatibility metadata
```

Derived indexes and presentation state rebuild after load.

Load validates fully before replacing authoritative state.

## Inspection and artifacts

The runtime should produce a normalized snapshot suitable for:

- agent reasoning;
- deterministic tests;
- human architecture review;
- later multi-fidelity equivalence comparisons.

Avoid exposing internal store layout in artifacts. Artifacts describe semantic state.

## Composition model

Explicit static composition:

```text
builder
  .AddExistingRuntime()
  .AddSimulationFoundation()
  .AddGameModule(...)
```

No runtime assembly scanning, plugin marketplace, remote service, or generalized extension host is introduced.

## Performance model

M031 prioritizes semantic correctness and baseline measurement.

Acceptable initial implementation may use existing stores and indexes where correctness is clear.

Optimization triggers require measured evidence in later milestones. Candidate future changes include dense storage, specialized indexes, event coalescing, and parallelism, but none is an M031 goal.

## Failure behavior

Every cross-cutting operation must fail atomically:

- component registration;
- entity creation;
- region transfer;
- reservation acquisition;
- activity transition;
- save;
- load.

Failure evidence includes stable IDs, expected/current revision, diagnostic code, and no partial state.

## Future extension points

M031 must leave bounded typed extension points for:

- game-defined components;
- activity kinds and stage graphs;
- domain commands/events;
- scheduled-trigger delivery;
- detailed executor state;
- abstract executor state;
- region fidelity metadata;
- persistence contributors;
- inspection contributors.

It must not implement these future consumers prematurely.

## Architecture invariants

1. One authoritative world.
2. One authoritative simulation clock.
3. Stable identity across save/load and region transfer.
4. Mutations through validated runtime boundaries.
5. Commands request; events record facts.
6. Activities are explicit semantic state.
7. Reservations are authoritative and deterministic.
8. Rendering remains read-only.
9. Spatial modules do not own entities.
10. The simulation foundation can run without graphics.
11. The future abstract executor is optional and not a dependency of shared rules.
12. Deterministic artifacts describe semantics, not internal storage.
