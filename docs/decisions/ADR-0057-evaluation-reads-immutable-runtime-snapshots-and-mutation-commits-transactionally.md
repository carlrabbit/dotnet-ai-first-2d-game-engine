# ADR-0057 — Evaluation Reads Immutable Runtime Snapshots and Mutation Commits Transactionally

## Status

Accepted for M045.

## Context

M039 established `EntityComponentWorld` as the single authoritative component owner, but behavior snapshots remained entity-ID-only, spatial resolvers retained live world references, lifecycle mutations could require compensating rollback, and CLR type bindings could silently select the first stable component ID.

## Decision

All behavior/domain/spatial evaluation reads an immutable typed runtime snapshot captured at a phase boundary.

Evaluators return mutation proposals; they do not mutate the live world.

A bounded runtime transaction validates and stages entity lifecycle, provenance and heterogeneous component set/remove operations before commit.

Generic CLR-type access is permitted only for an unambiguous stable component binding. Ambiguous runtime types require explicit stable-key access.

Snapshot fingerprints use canonical semantic encodings.

Rejected commands preserve their actual tick and identity and emit no factual success events.

## Consequences

The ECS remains simple and replaceable; behaviors/spatial modules gain one coherent read boundary; M014 and SimulationWorld atomic construction no longer need compensating destruction; descriptor aliases remain possible without unsafe generic resolution; and M046 can repair continuous movement semantics without reopening mutation ownership.
