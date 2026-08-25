# Simulation World and Semantic Foundation Contract

## Authority

This document is authoritative for the reusable simulation-world substrate introduced by M031 and corrected by M039.

It extends the entity/component runtime. Where simulation-specific semantics are described here, this contract governs.

## Purpose

Provide one authoritative persistent deterministic simulation world that can be executed by detailed and abstract strategies without duplicating game rules.

```text
typed runtime state
→ validated semantic command
→ staged runtime + semantic mutation
→ atomic commit
→ factual domain events
→ deterministic persistence/inspection
```

## Ownership boundaries

### EntityComponentWorld owns

- authoritative entity identity;
- every authoritative runtime component instance;
- explicit component descriptors;
- component validation;
- typed queries;
- heterogeneous component batch commit;
- immutable/read-only component snapshots.

### SimulationWorld owns

- simulation world identity and clock;
- region/lifecycle semantics layered over runtime identity;
- activities and reservations;
- semantic command coordination;
- command/event causal sequence;
- persistence classification policy;
- simulation persistence/restoration;
- semantic inspection/fingerprints.

### Game modules own

- typed game-defined component types;
- domain command/event types;
- activity transition policy;
- reservation subject/capacity/guard policy;
- bounded proof fixtures.

### Forbidden ownership

`SimulationWorld` and game modules MUST NOT own a second authoritative component-value dictionary or object graph parallel to `EntityComponentWorld`.

JSON/object component maps are permitted only as read-only derived/boundary projections.

## Component registration

Each simulation component family binds explicit runtime registration to simulation metadata:

```text
stable component key
schema version
CLR runtime type binding
persistence classification
canonical codec
inspection projection
optional rebuild authority
```

The stable key/schema/codec metadata is durable identity.

Assembly-qualified CLR type names are not canonical persistence identity and do not affect semantic fingerprints.

Generic typed domain access and type-erased persistence/inspection access resolve the same runtime descriptor/store.

## Typed component access

Simulation/game logic uses typed component APIs or typed semantic query services.

Authoritative reads do not normally traverse:

```text
entity.Components[string]
JsonElement.GetProperty(...)
```

Inspection may project typed values into keyed JSON, but that output is read-only evidence.

Authoritative component values are immutable/read-only to consumers.

## Identity and regions

Use stable typed IDs including `EntityId`, `WorldId`, `RegionId`, `ActivityId`, `ReservationId`, command/event/correlation/causation IDs.

One simulation world contains explicit regions.

An entity is region-owned or world-scoped according to explicit simulation semantics.

Region transfer preserves identity.

Destroyed identity is protected from silent reuse.

Active → inactive → active is supported where valid.

Destroy/transfer commands resolve or reject affected activity/reservation references atomically so the resulting world remains persistable/loadable.

## Semantic time

One authoritative simulation clock owns `Now`.

Wall-clock/render time is not simulation authority.

Deterministic ordering uses explicit simulation instant plus phase/sequence semantics rather than task/hash/wall-clock order.

## Semantic commands and transactions

A semantic command may affect heterogeneous typed runtime components, activities, reservations, lifecycle/region state, deterministic sequence/causal state, and factual domain events.

Handling:

1. read current authoritative state;
2. validate identity, lifecycle, region, revisions, activity/reservation and domain preconditions;
3. stage typed component batch through `EntityComponentWorld`;
4. stage `SimulationWorld` semantic state;
5. validate the complete transition;
6. commit atomically;
7. emit/publish factual domain events after commit;
8. return structured command result with actual emitted event IDs.

Failure commits no partial state and no factual success event.

Live `SetComponent` calls followed by an independent `RecordFact` do not represent one semantic atomic command.

## Domain events

A domain event records a committed fact.

It resolves stable event ID, type, simulation instant, sequence, affected IDs, correlation/causation and canonical payload.

Events are not mutation requests.

## Activities

Activities are explicit mode-independent semantic work state.

Activity kinds have registered/shared transition authority.

Stage/status transitions validate kind, current and next stage/status, expected revision, actor/target prerequisites, and terminal rules.

Invalid stage/status combinations reject even if revision is current.

Initial activity plus required initial reservations is atomic.

## Reservations

Reservations are authoritative semantic concurrency state.

Capacity/availability and subject guards are derived from authoritative typed component state through registered reservation policy.

Caller-supplied or hardcoded capacity values do not override authoritative subject capacity.

Required semantics include deterministic conflict resolution, positive quantity, atomic acquisition, idempotent release, stale-guard rejection, subject invalidation and terminal-activity cleanup.

Completed/cancelled/failed activities cannot retain active reservations.

## Persistence classification

| Classification | Save authority |
|---|---|
| authoritative-persistent | stored canonically |
| derived-rebuildable | omitted and rebuilt where registered |
| active-mode-transient | omitted unless separately declared continuation authority |
| presentation-only | never gameplay/save authority |
| external-handle | never persisted |

Classification is executable behavior, not descriptive metadata.

## Persistence envelope and compatibility

Current canonical SimulationWorld schema:

```text
agentic2d.simulation-world-save.v2
```

Minimum supported schema: v2.

v1 is explicitly incompatible and rejected; it is not migrated.

Persisted component entries resolve stable component key, component schema version and canonical payload. Persisted CLR assembly names are not required.

Load resolves keys through current explicit runtime descriptors, decodes typed values, validates the complete envelope/referential state, stages runtime plus simulation state, rebuilds required derived state, and commits only after complete validation.

M033 multi-fidelity persistence uses its v2 envelope and embeds/validates SimulationWorld v2.

## Canonical fingerprint

Canonical fingerprint covers authoritative semantics and stable durable metadata.

It excludes assembly-qualified CLR type names, process IDs, paths, timestamps, native handles, presentation/transient state, and storage allocation/index details.

Equivalent direct and save/load continuations produce equivalent authoritative fingerprints.

## Inspection

Inspection is read-only.

It may provide stable JSON projections for tools and artifacts, including entities/lifecycle/region, stable component keys, canonical component values, persistence classifications, activities/reservations, commands/events, invariants/fingerprint.

Inspection JSON is derived from typed runtime state and is not the normal gameplay component API.

## Current consumer rule

M032/M033/current direct consumers extend this contract through typed runtime components and semantic commands.

They do not bypass the runtime with parallel component stores or JSON-shaped authoritative state.

## Explicit exclusions

This contract does not authorize archetype/sparse-set/third-party ECS rewrite, dynamic plugin/assembly scanning, one world per region, multithreaded simulation, rendering mutation, event-sourced authoritative state, `SimulationWorld`-owned component dictionaries, or CLR assembly identity as persistence identity.
