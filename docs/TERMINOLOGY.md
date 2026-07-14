# Terminology

## Authority

This document is authoritative for project vocabulary.

## Core terms

| Term | Meaning |
|---|---|
| Agentic engine | A game engine designed so AI agents can implement, validate, inspect, and iterate through structured interfaces and evidence. |
| Headless-first | Core engine capability operates through CLI/API workflows without requiring a graphical client. |
| Artifact-first | Commands produce machine-readable and reviewable evidence. |
| Stable ID | Durable identity independent of filename, display name, hierarchy position, or visual coordinates. |
| Scenario | Deterministic runtime validation with authored inputs, assertions, and artifacts. |
| Content | Authored non-code project data, including scenarios, assets, maps, entity definitions, visual definitions, and review decisions. |
| Generated artifact | Reproducible output derived from source content or code. |
| Entity definition | Authored reusable defaults for runtime entity composition. It is not a runtime entity. |
| Spawn | Authored request that combines a definition ID, spawn ID, runtime entity ID, source context, and bounded overrides. |
| Runtime entity | Stable mutable runtime identity plus typed components. |
| Static map object | Authored map content projected into static world or rendering data without runtime entity lifecycle. |
| Component | Typed runtime state owned and mutated by the runtime. |
| Behavior module | Code that reads immutable snapshots and emits intents rather than mutating stores directly. |
| Spatial module | Capability that interprets spatial components and static world data without owning entity identity. |
| Spatial query | Read-only deterministic query over compatible spatial entity state. |
| Trigger | Non-solid entity-owned spatial volume that emits entered/exited transitions. |
| Interaction intent | Explicit request by an interactor to select and begin an eligible interaction. |
| Visual definition | Authored presentation-only definition containing asset regions, anchors, sizes, layers, ordering, and tint. |
| Render projection | Deterministic read-only conversion from authored content and immutable runtime state into backend-neutral render items and commands. |
| Debug client | Optional graphical adapter for inspection. It is not runtime authority. |
| Structural render evidence | Backend-neutral JSON render frame, item, command, binding, and diagnostic artifacts. |
| Screenshot evidence | Explicitly captured PNG review evidence; not cross-platform semantic truth. |
| Human review gate | Explicit validation for outputs automation cannot fully judge. |
| Debug runtime | Development representation optimized for inspection and diagnostics. |
| Packaged runtime | Future release representation optimized for compactness and performance; not yet implemented. |
