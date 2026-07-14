# ADR-0018 — Rendering Is Read-Only and raylib Is an Isolated Adapter

## Status

Proposed for Milestone 015. Accept when implementation is accepted.

## Context

The engine has authored maps/assets, runtime entities/components, continuous positions, definitions, interactions, and snapshots. A graphical client is needed without creating a second gameplay runtime or coupling engine contracts to one native binding.

## Decision

Use:

```text
authored visual definitions
→ backend-neutral semantic projection
→ backend-neutral command compilation
→ isolated raylib-cs debug client
```

Rendering is read-only. Entity definitions and static map objects may reference visual definitions without changing ownership. Static projection is cached; dynamic projection is rebuilt from snapshots. Only the graphical project references a pinned raylib-cs package. Live and recorded-snapshot modes share one path. Screenshots are explicit review evidence; structural JSON is semantic authority.

## Consequences

Runtime remains headless and deterministic; raylib is replaceable; rendering is inspectable; live stepping and artifact replay share a world model. Costs include new schemas, native resource lifecycle, graphics-capable validation, and non-portable pixel baselines.

## Rejected alternatives

Direct renderer store access, raylib types in core contracts, graphical code in `Agentic2D.Tools`, mutable `Renderable` component in M015, automatic screenshots, PNG pixels as semantic truth, and separate live/artifact projectors.
