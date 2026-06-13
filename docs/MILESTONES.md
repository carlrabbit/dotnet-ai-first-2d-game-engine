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

## Deferred or superseded milestone documents

| Document | Status | Notes |
|---|---|---|
| `docs/milestones/MILESTONE-003-asset-curation-workbench-spike.md` | Deferred candidate | This was an early candidate Milestone 003. It is not the current Milestone 003. Renumber or replace it before implementation. |

## Next milestone direction

The next implementation milestone should be selected explicitly. Likely candidates include:

```text
scenario runner foundation
asset curation workbench spike
content/schema validation foundation
runtime inspection/reporting expansion
external guide-system migration if its package is applied later
```

Do not treat candidate directions as approved implementation scope without a milestone document.
