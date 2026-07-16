# ADR-0026 — Player-Facing Presentation Is Derived and Transient

## Status

Proposed for Milestone 021. Accept when implementation is accepted.

## Context

Effects, particles, shake, HUD, prompts, and notifications improve player comprehension but must not become gameplay authority or contaminate canonical saves.

## Decision

Player-facing presentation is a deterministic read-only projection from authoritative state, post-commit events, and authored definitions.

Effects, particles, shake, and notifications are transient and excluded from saves. Persistent gameplay presentation reconstructs after load from authoritative state.

## Consequences

The engine gains replayable presentation evidence while keeping persistence and gameplay boundaries clean. Pre-save transient feedback does not resume.
