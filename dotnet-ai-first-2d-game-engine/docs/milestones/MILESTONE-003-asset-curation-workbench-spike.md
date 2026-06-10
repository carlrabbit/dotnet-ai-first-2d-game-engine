# Milestone 003 — Asset Curation Workbench Spike

## Goal

Create the first vertical slice for agent-operable asset interpretation and preview generation.

## Required authority

- `docs/specs/asset-pipeline.md`
- `docs/CONTENT.md`
- `docs/ARTIFACTS.md`
- `docs/HUMAN-REVIEW.md`
- `docs/architecture/runtime-evaluation.md`

## Scope

- Inspect a raw PNG.
- Detect or accept a grid.
- Produce structural metadata.
- Create a tileset draft.
- Generate a contact sheet or preview artifact.
- Record provenance.
- Mark semantic labels as proposed, not approved.

## Runtime spike note

raylib-cs may be used here if a fast rendering/preview path is useful. Do not let the spike decide the final runtime architecture by accident.

## Validation tier

Tier 5 artifact/human review for semantic and visual review outputs.
