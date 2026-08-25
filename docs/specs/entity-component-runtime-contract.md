# Entity Component Runtime Contract

## Authority

This document is authoritative for runtime entity identity, entity lifecycle, typed component ownership, component registration, component queries, immutable/read-only component state, snapshots, and command-buffered/batched component mutation.

It does not define a final ECS storage architecture.

## Entity model

An entity is:

```text
stable EntityId
+ zero or more typed components
```

The runtime owns entity existence and lifecycle, every authoritative runtime component instance, component registration descriptors, component validation and mutation, deterministic enumeration, immutable snapshots, and lifecycle/component evidence.

No higher-level subsystem may create a second authoritative component-value store.

## Component identity and registration

Each component family has a stable type ID.

Registration is explicit.

The runtime maintains one descriptor per registered component family. A descriptor resolves at least:

```text
stable type ID
CLR runtime type
owner
validator
canonical serialization/deserialization authority
```

Stable type ID is durable semantic identity.

CLR type identity is runtime binding information. Assembly-qualified type names, assembly versions, file paths and equivalent deployment metadata are not canonical persisted component identity and do not perturb semantic registration fingerprints.

Dynamic runtime assembly scanning/plugin discovery is not required.

## Typed domain operations

Required typed semantics include equivalents of:

```text
Register<T>
Set<T>
TryGet<T>
Remove<T>
Query<T>
Query<T1,T2>
```

Rules:

- set requires an existing entity;
- the component family must be registered;
- validation occurs before mutation;
- first set emits component-added evidence;
- replacement emits component-updated evidence;
- remove follows one documented missing-value policy;
- queries are deterministic by entity ID;
- behavior/domain code does not receive mutable stores.

## Type-erased infrastructure operations

The runtime may expose a bounded non-generic/type-erased infrastructure API over the same registration descriptors and stores.

Its permitted purposes are persistence encoding/decoding, inspection/evidence, explicit heterogeneous mutation staging, and descriptor lookup by stable type ID.

It is not a second storage model and is not the preferred game/domain programming surface.

Generic and type-erased operations MUST resolve the same authoritative store and values.

## Immutable/read-only component values

Authoritative component reads cannot provide an uncontrolled mutation path.

Preferred stored forms are immutable records, readonly record structs, or equivalent immutable value objects.

If a mutable CLR object is accepted at a boundary, the runtime must defensively copy it such that mutating caller-held or returned references cannot mutate authoritative stored state without a runtime mutation operation.

A focused test must prove this property for current simulation components.

## Heterogeneous mutation batch

The runtime supports a bounded atomic batch containing mutations of multiple registered component families.

Required behavior:

1. resolve entity and descriptor for every staged mutation;
2. validate all values and preconditions;
3. stage without mutating live stores;
4. reject the full batch if any staged mutation is invalid;
5. commit all staged component changes as one visible runtime boundary;
6. produce deterministic mutation/event evidence.

The implementation may use temporary dictionaries, immutable staged state, copy-on-write, or another bounded mechanism.

Sequential live writes followed by compensating rollback do not satisfy atomic batch semantics.

The component batch does not by itself own game/domain events, activities or reservations; higher-level semantic coordinators may compose the component batch into a larger transaction.

## Storage policy

Permitted initial storage includes typed dictionaries, typed arrays, and a small explicit descriptor/store registry.

The runtime does not require archetype ECS, sparse-set optimization, reflection discovery, or a third-party ECS framework.

Semantic contracts do not depend on storage representation.

## Snapshots

A snapshot is immutable and tick-scoped.

It exposes entity existence, stable entity enumeration, typed component lookup, deterministic component queries, current tick, and deterministic fingerprint.

A later mutation must not alter an existing snapshot.

## Snapshot fingerprint

Fingerprint input includes tick, stable entity IDs, stable component type IDs, semantic component values, and deterministic ordering.

It excludes CLR assembly identity, paths, timestamps, process data, and storage allocation/index details.

## Mutation boundary

Behavior modules emit intents.

Domain modules resolve intents into accepted typed mutation commands or rejected domain results.

The runtime validates and applies accepted component mutation/batches.

No behavior, spatial resolver, simulation coordinator or consumer may directly mutate component stores during evaluation.

## Determinism

Equivalent registered component descriptors, initial state, inputs and command sequence produce equivalent entity enumeration, component queries, snapshots, fingerprints, accepted/rejected mutations, lifecycle/component events, and final component state.
