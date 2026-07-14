# Specs

## Authority

This document indexes permanent behavioral authority. Milestones sequence work; specs define current truth.

## Current spec documents

| Document | Authority area |
|---|---|
| `docs/specs/project-thesis.md` | Engine purpose and project thesis. |
| `docs/specs/runtime-principles.md` | Deterministic commands, events, queries, and state. |
| `docs/specs/agentic-workflow.md` | Human-agent workflow. |
| `docs/specs/minimal-deterministic-runtime.md` | Minimal runtime semantics. |
| `docs/specs/product-cli-contract.md` | `agentic2d` product CLI. |
| `docs/specs/scenario-runner-contract.md` | Scenario execution. |
| `docs/specs/content-validation-contract.md` | Structured content validation. |
| `docs/specs/asset-metadata-contract.md` | Asset metadata and PNG-backed atlas source. |
| `docs/specs/asset-pipeline.md` | Asset pipeline principles. |
| `docs/specs/review-pack-contract.md` | Review-pack aggregation. |
| `docs/specs/asset-curation-workbench-contract.md` | Static curation workbench. |
| `docs/specs/asset-review-decision-contract.md` | Review decisions and safe asset metadata changes. |
| `docs/specs/asset-perception-contract.md` | Deterministic local perception evidence. |
| `docs/specs/map-content-contract.md` | Authored map content. |
| `docs/specs/runtime-inspection-contract.md` | Structured runtime inspection. |
| `docs/specs/behavior-modules.md` | Behavior-module principles. |
| `docs/specs/deterministic-behavior-runtime-contract.md` | Behavior phases, snapshots, intents, and deterministic resolution. |
| `docs/specs/pluggable-spatial-runtime-contract.md` | Spatial module boundary. |
| `docs/specs/grid-spatial-module-contract.md` | `spatial.grid`. |
| `docs/specs/entity-component-runtime-contract.md` | Runtime entities, components, snapshots, and mutations. |
| `docs/specs/mixed-world-projection-contract.md` | Separation of map static content and runtime entity state. |
| `docs/specs/continuous-kinematic-spatial-module-contract.md` | Continuous AABB movement and collision. |
| `docs/specs/entity-definition-and-instantiation-contract.md` | Entity definitions, spawns, overrides, transactional instantiation, and provenance. |
| `docs/specs/spatial-query-and-trigger-contract.md` | Spatial lookup, overlap/radius queries, filters, and trigger transitions. |
| `docs/specs/interaction-runtime-contract.md` | Explicit interaction intent and deterministic target selection. |
| `docs/specs/visual-definition-contract.md` | Presentation-only visual definitions. |
| `docs/specs/render-projection-contract.md` | Read-only backend-neutral render projection. |
| `docs/specs/raylib-debug-client-contract.md` | Isolated raylib-cs graphical client. |
| `docs/specs/input-action-map-contract.md` | Backend-neutral input maps and mapping. |
| `docs/specs/tick-bound-input-frame-contract.md` | Tick input frames and accumulator. |
| `docs/specs/semantic-input-recording-and-replay-contract.md` | Semantic recording and replay. |

## Current invariants

- Runtime and headless product CLI remain usable without graphical dependencies.
- Stable IDs identify important project objects.
- Authored source is structured, validatable, diff-friendly, and reviewable.
- Runtime mutation occurs through validated runtime boundaries.
- Behavior, spatial, interaction, and rendering code do not directly mutate stores outside their authority.
- Static map content and runtime entities remain distinct.
- Rendering is read-only and backend-neutral before adapter translation.
- Structural artifacts are semantic evidence; screenshots are review evidence.
- Failures produce stable diagnostics and useful artifacts.
