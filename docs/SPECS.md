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
| `docs/specs/shared-asset-home-and-source-registry-contract.md` | Local authoring asset home and source identity. |
| `docs/specs/reusable-asset-discovery-profile-contract.md` | Reusable PNG/WAV observations and conservative proposals. |
| `docs/specs/reusable-asset-annotation-and-cleanup-contract.md` | Retained reusable corrections and disposable generated metadata. |
| `docs/specs/asset-campaign-and-batch-contract.md` | Game-local campaign proposals and bounded batches. |
| `docs/specs/asset-workbench-session-and-alias-contract.md` | Persistent provider-side workbench sessions and ephemeral aliases. |
| `docs/specs/asset-workbench-input-contract.md` | Explicit text-stream and mouse/touch workbench input. |
| `docs/specs/asset-workbench-decision-and-consequence-contract.md` | Review decisions, consequence confirmation, history, and staleness. |
| `docs/specs/asset-preview-host-ipc-contract.md` | Restartable local preview-host protocol. |
| `docs/specs/approved-asset-and-deterministic-promotion-contract.md` | Project-local approved assets and atomic deterministic promotion. |
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
| `docs/specs/autonomous-work-and-detailed-logistics-contract.md` | M032 derived work, selection, logistics, needs, and explanation. |
| `docs/specs/detailed-grid-navigation-and-activity-execution-contract.md` | M032 detailed navigation, movement, and command-backed execution. |
| `docs/specs/input-action-map-contract.md` | Backend-neutral input maps and mapping. |
| `docs/specs/tick-bound-input-frame-contract.md` | Tick input frames and accumulator. |
| `docs/specs/game-project-manifest-contract.md` | Game/product manifest independent of acquisition. |
| `docs/specs/game-workspace-manifest-contract.md` | Workspace composition and engine acquisition. |
| `docs/specs/workspace-scaffolding-contract.md` | Transactional consumer workspace creation. |
| `docs/specs/unified-agent-execution-workflow-contract.md` | Workspace/project/run consumer workflow. |
| `docs/specs/semantic-input-recording-and-replay-contract.md` | Semantic recording and replay. |
| `docs/specs/animation-definition-contract.md` | Authored deterministic presentation animation. |
| `docs/specs/animation-selection-and-sampling-contract.md` | Explicit base/overlay sampling and composition. |
| `docs/specs/animation-marker-contract.md` | Presentation-only marker occurrences. |
| `docs/specs/animated-render-projection-contract.md` | Animated values before semantic rendering. |
| `docs/specs/sound-definition-and-command-contract.md` | Deterministic sound definitions and backend-neutral commands. |
| `docs/specs/sound-cue-projection-contract.md` | Presentation-only cue projection. |
| `docs/specs/resource-damage-and-lifecycle-contract.md` | Health, damage, defeat, and lifecycle. |
| `docs/specs/item-inventory-and-collection-contract.md` | Items, inventory, and atomic collection. |
| `docs/specs/simulation-world-and-semantic-foundation-contract.md` | Optional partitioned simulation world, semantic time, commands/events, activities, reservations, persistence, and inspection. |
| `docs/specs/gameplay-presentation-event-contract.md` | Post-commit gameplay presentation events. |
| `docs/specs/geometry-authoring-diagnostics-contract.md` | Geometry inspection, preview, diagnostics, and structural/graphical comparison. |
| `docs/specs/generated-sound-linkage-contract.md` | Explicit synthesis-to-derived-asset-to-runtime-sound linkage. |
| `docs/specs/scaled-performance-workload-contract.md` | Bounded scaled performance workload and timing-authority policy. |
| `docs/specs/autonomous-tic-tac-toe-consumer-contract.md` | Second consumer state, rules, presentation, and validation scope. |

## Current invariants

- Runtime and headless product CLI remain usable without graphical dependencies.
- Stable IDs identify important project objects.
- Authored source is structured, validatable, diff-friendly, and reviewable.
- Runtime mutation occurs through validated runtime boundaries.
- Behavior, spatial, interaction, and rendering code do not directly mutate stores outside their authority.
- Static map content and runtime entities remain distinct.
- Rendering is read-only and backend-neutral before adapter translation.
- Structural artifacts are semantic evidence; screenshots are review evidence.
- Game/product truth remains independent of engine acquisition and checkout composition.
- Failures produce stable diagnostics and useful artifacts.
- The optional simulation foundation uses one authoritative world with explicit regions; rendering and spatial modules do not own or mutate it.
