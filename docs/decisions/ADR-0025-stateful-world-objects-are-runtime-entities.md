# ADR-0025 — Stateful World Objects Are Runtime Entities

## Status

Proposed for M020; accept with implementation.

## Decision

Doors, switches, and similar mutable objects are runtime entities with explicit state and transactions. Authored maps remain structurally static. Door state projects into collision, spatial indexing, interaction, animation, sound, and rendering; invalidation is explicit and persistence restores state before resume.

## Consequences

Runtime ownership remains clear. Arbitrary map mutation, destructible terrain, and tile replacement are deferred.
