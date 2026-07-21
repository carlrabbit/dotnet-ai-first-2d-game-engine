# ADR-0040 — Shared Local Asset Home Is Authoring Infrastructure, Not Game Authority

## Status

Proposed for M028. Accept when M028 completes.

## Context

Multiple Agentic2D games may reuse raw asset libraries and discovery metadata. Copying every library into every game wastes storage and discovery work, but a shared store must not make games dependent on one machine or let generated metadata become game semantics.

## Decision

Use one configurable machine-local asset home as the normal reuse mechanism. It contains raw sources, registry, generated profiles, reusable annotations, previews, sessions, and cache.

Generated metadata is disposable and may be cleaned/rebuilt. Reusable human annotations remain until explicit removal. Game-specific campaign decisions stay in the game project or bounded fixtures. Future approved assets are materialized into game authority; the shared home is never runtime/export authority.

Portable profile-bundle export is deferred until cross-machine reuse is demonstrated.

## Consequences

Multiple games can reuse one profile; changed sources create new fingerprints; stale metadata can be removed simply; human corrections are retained; no database/server/sync system is required; M029 builds the interactive workbench over these contracts; M030 validates promotion and game integration.
