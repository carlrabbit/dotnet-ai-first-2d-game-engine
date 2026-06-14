# Guide Sync Hint — Milestone 010 Review Pack Index Sync

## Status

pending

## Origin

Milestone 010 planning package.

## Purpose

After Milestone 010 is implemented, update active project indexes and command references so the review pack and generated asset curation workbench capabilities are discoverable from normal repository documentation.

## Suggested documentation-sync scope

Review and update, as needed:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/ARTIFACTS.md
docs/DECISIONS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/CONTENT.md
README.md
AGENTS.md
```

## Completion criteria

This hint can be deleted when:

- Milestone 010 is indexed with the correct status;
- `docs/specs/review-pack-contract.md` is indexed;
- `docs/specs/asset-curation-workbench-contract.md` is indexed;
- `docs/artifacts/review-pack-artifact-contract.md` is indexed;
- `docs/artifacts/asset-curation-workbench-artifact-contract.md` is indexed;
- ADR-0013 is indexed after acceptance;
- engineering docs list `eng/review-pack-smoke.sh` and `eng/asset-curation-smoke.sh` only if implemented;
- product CLI docs list `agentic2d review pack` and `agentic2d asset curate` only if implemented;
- ordinary implementation routing still does not require `.guide-profile.json`, `.guide-sync/`, copied guide docs, external guide internals, or prompt templates.

## Notes

This file is deferred documentation synchronization metadata. Ordinary implementation agents must ignore `.guide-sync/` unless explicitly assigned documentation synchronization, planning, guide migration, or release-readiness work.
