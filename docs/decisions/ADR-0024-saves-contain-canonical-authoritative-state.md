# ADR-0024 — Saves Contain Canonical Authoritative State

## Status

Proposed for M020; accept with implementation.

## Decision

Saves contain explicit, versioned, canonical authoritative records produced by registered persistence contributors. Contributors validate and reconstruct a fresh runtime transactionally. Adapter resources, presentation commands, artifacts, diagnostics, caches, and wall-clock data are excluded. Compatibility is strict and M020 provides no automatic migration.

## Consequences

Saves are deterministic and inspectable, but every persistent module must define an explicit contributor contract.
