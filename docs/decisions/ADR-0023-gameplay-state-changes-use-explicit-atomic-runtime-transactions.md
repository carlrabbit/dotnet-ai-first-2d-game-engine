# ADR-0023 — Gameplay State Changes Use Explicit Atomic Runtime Transactions

## Status

Proposed for Milestone 019. Accept when implementation is accepted.

## Context

Damage, defeat, inventory changes, and world-item removal affect authoritative state and must remain deterministic, inspectable, and free from partial mutation.

## Decision

Behaviors emit intents. Resolvers validate complete changes before commands/transactions mutate state. Damage records intent, resolution, transition, and post-commit events. Collection commits inventory update and world-item removal atomically. Events emit only after commit. Defeat remains distinct from removal.

## Consequences

No partial collection, duplicate ownership, or event-before-state ambiguity. The design provides concrete persistence boundaries for later save/load at the cost of explicit records and deliberately limited feature breadth.

## Rejected alternatives

Direct component mutation, event-before-commit, animation-driven damage, automatic defeat removal, and inventory-first best-effort world removal.
