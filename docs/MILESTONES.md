# Milestones

## M028

M028 supplies the machine-local shared asset home, reusable discovery profiles and annotations, game-local campaigns/batches, and copyable headless evidence. Its blocking review is `review.m028.shared-asset-library-discovery-and-campaign-foundation`; M029 workbench interaction and asset promotion remain deferred.

## M029

M029 adds persistent provider-side workbench sessions, explicit text-stream and mouse/touch command routing, a restartable preview-host protocol, history-preserving consequence-aware review, and atomic deterministic promotion. Its blocking review is `review.m029.choice-driven-workbench-preview-and-promotion`; consumer integration remains M030 scope.

## Authority

This document indexes planned and completed implementation milestones.

## Current milestone documents

| Milestone | Document | Status | Notes |
|---|---|---|---|
| 000 | `docs/milestones/MILESTONE-000-initialization-skeleton.md` | Complete | Repository initialization and project-truth skeleton. |
| 001 | `docs/milestones/MILESTONE-001-base-engineering-substrate.md` | Implemented | Base .NET engineering substrate and canonical commands. |
| 002 | `docs/milestones/MILESTONE-002-minimal-deterministic-runtime.md` | Implemented | Minimal deterministic runtime and artifacts. |
| 003 | `docs/milestones/MILESTONE-003-introduce-product-cli-around-runtime.md` | Implemented | Product CLI surface. |
| 004 | `docs/milestones/MILESTONE-004-migrate-to-external-guide-system-v0.2.0.md` | Implemented | External guide-system metadata and routing. |
| 005 | `docs/milestones/MILESTONE-005-scenario-runner-and-runtime-evidence-foundation.md` | Implemented | Scenario runner and runtime evidence. |
| 006 | `docs/milestones/MILESTONE-006-content-schema-validation-foundation.md` | Implemented | Content validation. |
| 007 | `docs/milestones/MILESTONE-007-asset-metadata-and-tile-atlas-curation-slice.md` | Implemented | Asset metadata and atlas inspection. |
| 008 | `docs/milestones/MILESTONE-008-documentation-synchronization-after-m007.md` | Implemented | First documentation synchronization. |
| 009 | `docs/milestones/MILESTONE-009-update-to-latest-guide-system-v0.3.0.md` | Implemented | Guide-system v0.3.0 metadata and disconnected execution handoff. |
| 010 | `docs/milestones/MILESTONE-010-asset-review-pack-and-curation-workbench-foundation.md` | Implemented | Review packs and static curation workbench. |
| 011 | `docs/milestones/MILESTONE-011-asset-authoring-map-validation-and-runtime-evidence-expansion.md` | Implemented | Asset review/perception, maps, runtime inspection, bounded journey. |
| 012 | `docs/milestones/MILESTONE-012-deterministic-behavior-modules-and-pluggable-grid-spatial-runtime.md` | Implemented | Behavior intents and `spatial.grid`. |
| 018 | `docs/milestones/MILESTONE-018-game-workspace-manifest-deterministic-scaffolding-and-unified-agent-execution-workflow.md` | Implemented | Consumer workspace manifests, source acquisition, and unified run evidence. |
| 013 | `docs/milestones/MILESTONE-013-entity-component-runtime-and-continuous-kinematic-spatial-module.md` | Implemented | Runtime entities/components and continuous kinematic spatial module. |
| 014 | `docs/milestones/MILESTONE-014-authored-entity-definitions-runtime-instantiation-spatial-queries-triggers-and-interactions.md` | Implemented | Entity definitions, transactional instantiation, queries, triggers, and interactions. |
| 015 | `docs/milestones/MILESTONE-015-read-only-rendering-projection-and-raylib-cs-debug-client.md` | Implemented | Read-only render projection and isolated raylib-cs debug client. |

## Deferred or superseded milestones

| Document | Status | Notes |
|---|---|---|
| `docs/milestones/MILESTONE-003-asset-curation-workbench-spike.md` | Deferred candidate | Superseded by the implemented M003 and later asset milestones. |

## Proposed engineering-validation roadmap — decouple regressions from milestone receipts

**Priority:** High; schedule before the new UI/workbench regression surface expands substantially, ideally before or alongside M053. **State:** Unassigned candidate, not a ready milestone; no new number is reserved.

**Observed problem:** M050's `m048-review-regression` requires a successful `m048-smoke --verify`, and M048's twelve receipts embed a repository-wide fingerprint. Unrelated source, documentation, or ledger edits can therefore force re-execution of predecessor acceptance shards, including native Raylib capture, without indicating an actual regression.

**Target:** Preserve worthwhile M048/M050 behavior checks as maintained unit/integration tests with focused entry points. Retain milestone-completion evidence and approved human reviews as historical proof; later changes check affected current contracts, not the whole predecessor milestone. Make reusable test-receipt freshness depend on relevant inputs with a conservative fallback for unknown dependencies.

**Proposed implementation sequence:**

1. Inventory M048/M050 validation by role: permanent behavior, bounded/native integration, historical completion proof, and unverified success assertions.
2. Establish ordinary fast regression coverage and explicit native integration journeys for shared preview, review and input contracts.
3. Replace whole-prior-milestone dependencies with narrow, exercised behavioral prerequisites while preserving outstanding M048/M050 acceptance and review obligations.
4. Introduce scoped, fail-closed validation-input fingerprints and separate historical archival records from current test outcomes.
5. Test changed/unrelated-file selection, negative regressions, native evidence freshness and real execution cost; update project engineering authority and completion documentation once a ready milestone adopts the design.

**Non-goals:** No reopening approved historical reviews, retroactive weakening of ready milestones, silent relaxation of current graphic prerequisites, or M053 workbench migration.

**Research:** `docs/research/milestone-receipts-vs-regression-validation-2026-10-12.md` (non-authoritative). This roadmap records priority and desired outcome, not an executable contract.

## Next direction

Select the next milestone explicitly from current project truth. Likely candidates include deterministic input frames, animation projection, audio projection, save/load and replay, or packaging. None is current capability until specified and implemented.
| 022 | `docs/milestones/MILESTONE-022-adopt-guide-system-v0.5.0-constrained-validation-and-human-review.md` | Implemented | Guide-system v0.5.0 constrained validation and repository-local review. |
| 027 | `docs/milestones/MILESTONE-027-geometry-and-generated-sound-contract-stabilization-review-evidence-packs-and-guide-system-v060-migration.md` | In progress | Stable authoring contracts, durable review packs, and milestone-scoped review migration. |
