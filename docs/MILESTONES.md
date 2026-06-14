# Milestones

## Authority

This document indexes planned and completed implementation milestones.

Milestones define implementation focus areas. They should reference specs and engineering validation tiers instead of duplicating all authority.

## Current milestone documents

| Milestone | Document | Status | Notes |
|---|---|---|---|
| 000 | `docs/milestones/MILESTONE-000-initialization-skeleton.md` | Complete | Repository initialization skeleton and project-truth documents. |
| 001 | `docs/milestones/MILESTONE-001-base-engineering-substrate.md` | Implemented | Base .NET engineering substrate, canonical `eng/` commands, minimal solution. |
| 002 | `docs/milestones/MILESTONE-002-minimal-deterministic-runtime.md` | Implemented | Minimal deterministic runtime, built-in runtime smoke path, runtime result artifact contract. |
| 003 | `docs/milestones/MILESTONE-003-introduce-product-cli-around-runtime.md` | Implemented | First stable product CLI surface around the minimal runtime and product CLI validation wrappers. |
| 004 | `docs/milestones/MILESTONE-004-migrate-to-external-guide-system-v0.2.0.md` | Implemented | External guide-system metadata, ordinary-agent routing, and guide-sync queue adoption. |
| 005 | `docs/milestones/MILESTONE-005-scenario-runner-and-runtime-evidence-foundation.md` | Implemented | Authored `runtime.smoke` scenario runner and runtime evidence artifacts. |
| 006 | `docs/milestones/MILESTONE-006-content-schema-validation-foundation.md` | Implemented | Content validation foundation for authored scenario JSON. |
| 007 | `docs/milestones/MILESTONE-007-asset-metadata-and-tile-atlas-curation-slice.md` | Implemented | Asset metadata validation, tile atlas fixture, and asset inspection artifacts. |
| 008 | `docs/milestones/MILESTONE-008-documentation-synchronization-after-m007.md` | Implemented | Documentation synchronization after Milestones 004 through 007. |

## Deferred or superseded milestone documents

| Document | Status | Notes |
|---|---|---|
| `docs/milestones/MILESTONE-003-asset-curation-workbench-spike.md` | Deferred candidate | This was an early candidate Milestone 003. It is not the current Milestone 003. Renumber or replace it before implementation. |

## Next milestone direction

The next implementation milestone should be selected explicitly. Likely candidates include:

```text
asset curation workbench spike
runtime inspection/reporting expansion
map/content validation expansion
human review pack criteria
```

Do not treat candidate directions as approved implementation scope without a milestone document.
