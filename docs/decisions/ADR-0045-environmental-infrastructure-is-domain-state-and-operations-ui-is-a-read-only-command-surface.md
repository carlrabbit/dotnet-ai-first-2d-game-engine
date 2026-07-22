# ADR-0045 — Environmental Infrastructure Is Domain State and Operations UI Is a Read-Only Command Surface

## Status

Accepted for M034.

## Decision

Infrastructure, construction, production, storage, condition, maintenance, and policy are authoritative simulation-domain state.

The operations surface is a read-only projection plus explicit command input.

Detailed and abstract execution invoke the same semantic commands.

## Consequences

- headless and graphical behavior remain equivalent;
- saves contain game truth without UI dependence;
- alerts/explanations can be structurally validated;
- later clients can reuse projections;
- UI cannot shortcut state changes.

## Rejected

UI-owned construction state, detailed-only infrastructure rules, renderer-driven production, opaque aggregate counters without provenance, and a full fluid/network solver.

## Constraints

Explicit commands, fixed-point/integer authority, reusable contracts, bounded dogfood, and no M030 dependency.
