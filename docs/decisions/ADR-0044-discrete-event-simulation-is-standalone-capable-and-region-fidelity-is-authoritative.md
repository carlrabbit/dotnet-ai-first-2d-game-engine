# ADR-0044 — Discrete-Event Simulation Is Standalone-Capable and Region Fidelity Is Authoritative

## Status

Accepted for M033 implementation.

## Context

Inactive regions must advance faster than real time while one loaded region remains detailed. The abstract engine could be game-local, inseparable from graphics, or an optional standalone subsystem. Fidelity could be inferred or explicit.

## Decision

The discrete-event engine is an optional first-class subsystem with a standalone headless host.

Multi-fidelity reconciliation is a separate optional capability integrating M032 detailed execution and M033 abstract execution.

Region fidelity and executor ownership are authoritative persistent state. Shared rules remain execution-mode independent.

## Consequences

Positive: accelerated CI/balancing, deterministic long-horizon tests, no renderer dependency, explicit ownership, mixed-fidelity persistence, future server hosting, shared rules.

Negative: complex transitions/persistence, approximate position mapping, mandatory equivalence evidence, shared global time/order orchestration.

## Rejected alternatives

- Game-local abstract loop: duplicates engine authority.
- Mandatory kernel: unnecessary for all consumers.
- Dynamic plugin: explicit composition is sufficient.
- Inferred fidelity: cannot be reliably persisted/validated.
- Detailed pathfinding in inactive regions: defeats scale objective.
- Exact transition equivalence: unnecessary and infeasible.

## Constraints

Standalone-capable, explicit composition, authoritative fidelity, one detailed region, transactional transitions, shared rules, no dynamic plugin framework, and no multithreaded delivery in M033.
