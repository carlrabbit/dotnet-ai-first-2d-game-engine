# Specs

## Authority

This document indexes internal behavioral authority for the engine.

Specs define what must be true. Milestones sequence work but do not define permanent behavior.

## Current spec documents

| Document | Authority area |
|---|---|
| `docs/specs/project-thesis.md` | Core project thesis and engine purpose. |
| `docs/specs/runtime-principles.md` | Initial runtime principles for deterministic command/event/query behavior. |
| `docs/specs/agentic-workflow.md` | Intended human-agent workflow. |
| `docs/specs/minimal-deterministic-runtime.md` | Minimal runtime semantics introduced by Milestone 002. |
| `docs/specs/product-cli-contract.md` | Initial `agentic2d` product CLI behavior introduced by Milestone 003. |
| `docs/specs/scenario-runner-contract.md` | Authored scenario runner behavior introduced by Milestone 005. |
| `docs/specs/content-validation-contract.md` | Content validation behavior introduced by Milestone 006 and extended by Milestone 007. |
| `docs/specs/asset-metadata-contract.md` | Authored asset metadata behavior introduced by Milestone 007. |
| `docs/specs/asset-pipeline.md` | Initial asset pipeline principles. |
| `docs/specs/review-pack-contract.md` | Review pack evidence aggregation introduced by Milestone 010. |
| `docs/specs/asset-curation-workbench-contract.md` | Generated static asset curation workbench behavior introduced by Milestone 010. |
| `docs/specs/asset-review-decision-contract.md` | Authored asset review decisions and safe metadata mutation introduced by Milestone 011. |
| `docs/specs/asset-perception-contract.md` | Deterministic local asset perception and proposal evidence introduced by Milestone 011. |
| `docs/specs/map-content-contract.md` | First authored map domain and inspection behavior introduced by Milestone 011. |
| `docs/specs/runtime-inspection-contract.md` | Deterministic structured runtime inspection introduced by Milestone 011. |
| `docs/specs/behavior-modules.md` | Initial behavior module principles. |

## Initial invariants

- The engine is headless-first and CLI/API-first.
- Important project objects require stable IDs.
- Source-of-truth project data must be structured, diff-friendly, merge-friendly, schema-validatable, and reviewable.
- Runtime state must be observable through structured inspection.
- Scenario validation is a first-class engine concept.
- Failures must produce diagnostics and artifacts sufficient for agent or human diagnosis.
- Debug and packaged runtimes must preserve the same semantic behavior where both modes apply.
- `agentic2d` is the product/runtime CLI; `eng/` scripts are repository engineering wrappers.
