# ADR-0043 — Work Opportunities Are Derived and Detailed Execution Is an Optional Simulation Strategy

## Status

Accepted for M032 implementation.

## Context

M031 provides authoritative activities/reservations but deferred autonomous selection and detailed execution. M032 must decide whether every possible job is a persistent ECS entity and whether detailed path execution belongs inside shared activity rules.

## Decision

Potential work is derived inspectable state keyed by authoritative inputs/revisions. Persistent targets remain entities; accepted work becomes an M031 activity with reservations. Durable work-order entities require a later concrete need and spec amendment.

Detailed grid execution is an optional first-class strategy built on M031 activities. It owns transient route/progress and issues shared commands at semantic boundaries. Shared domain rules do not depend on pathfinding, rendering, or executor internals.

## Consequences

Benefits: less transient entity churn, deterministic pure selection, atomic assignment, durable accepted activity identity, future abstract reuse, route reconstruction after load, optional installation.

Costs: careful invalidation, bounded historical explanation evidence, assignment revalidation, and possible future need for durable work orders.

## Rejected alternatives

- every job as ECS entity;
- marker-component activity chains;
- detailed execution inside command handlers;
- speculative unified detailed/abstract executor;
- dynamic plugin loading.

## Constraints

Derivation/evaluation are read-only; assignment is atomic; executor owns no domain authority; paths are transient/rebuildable; shared rules contain no detailed branch; one active detailed region; deterministic ties/explanation are mandatory.
