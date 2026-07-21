# ADR-0042 — Simulation Foundation Is an Optional First-Class Engine Capability

## Status

Accepted for M031 implementation.

## Context

The third game requires:

- persistent regions;
- autonomous work and logistics in later milestones;
- a future standalone discrete-event simulation subsystem;
- a future detailed active-region executor;
- shared commands, events, activities, reservations, persistence, and inspection.

Three placements were considered:

1. game-local implementation;
2. conventional external/runtime-loaded plugin;
3. optional first-class engine capability with explicit composition.

A game-local implementation would duplicate or bypass existing entity, command, event, persistence, and inspection authority.

A conventional plugin would either require excessively broad internals or be a plugin in name only because simulation time and authoritative state are foundational.

Placing all simulation concepts in the mandatory engine kernel would burden games that do not require persistent multi-region simulation.

## Decision

Implement M031 as an optional first-class engine capability.

The engine and existing runtime define or host shared contracts for:

- stable identity;
- authoritative mutation;
- simulation time;
- commands and factual domain events;
- activities;
- reservations;
- persistence;
- inspection.

Games install the capability through explicit compile-time composition and contribute typed components and domain rules.

The future discrete-event engine will be a separate optional subsystem built on this foundation and capable of a standalone headless host.

The future multi-fidelity region capability will integrate detailed and abstract executors through the shared semantic contracts.

## Consequences

### Positive

- shared rules remain execution-mode independent;
- headless simulation remains possible;
- games without the capability remain simpler;
- future abstract simulation can live independently without forking world semantics;
- persistence and inspection remain engine-consistent;
- implementation remains strongly typed and analyzable;
- no dynamic plugin infrastructure is required.

### Negative

- some shared runtime contracts must expand;
- capability boundaries require deliberate public API design;
- persistence must support additional authoritative state;
- later detailed and abstract executors must conform to the same activity semantics.

## Rejected alternatives

### Game-local simulation foundation

Rejected because it would make stable identity, region partitioning, commands/events, persistence, and inspection diverge from engine authority.

### Runtime-loaded plugin

Rejected for M031 because dynamic discovery, version negotiation, deployment, reflection, and broad internal access are not required by a concrete use case.

### Mandatory engine-kernel subsystem

Rejected because not every engine consumer needs persistent simulation regions, activities, reservations, or simulation-time orchestration.

### One ECS world per region

Rejected because it complicates identity, transfers, reservations, cross-region references, save transactions, and future fidelity switching before measured scale requires it.

### Immediate archetype ECS rewrite

Rejected because current performance evidence does not establish that storage layout is the constraint. M031 measures baselines and preserves later optimization options.

### Event-sourced authoritative world

Rejected because the repository already uses state-oriented runtime authority and M031 needs canonical save/load rather than replaying an unbounded event history.

## Constraints

- explicit compile-time composition;
- no generalized runtime plugin loader;
- one authoritative world with explicit regions;
- no dependency from shared game rules to a future abstract executor;
- public contracts expose semantics, not internal stores;
- rendering remains read-only;
- scheduled-trigger queue implementation is deferred.
