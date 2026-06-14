# Guide Sync Hint — Milestone 006 Content Validation Index Sync

## Status

pending

## Origin

Milestone 006 planning package.

## Purpose

After Milestone 006 is applied and implemented, update repository indexes and cross-links so active project documentation reflects the new content validation capability.

## Suggested documentation-sync scope

Review and update, as needed:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/DECISIONS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
README.md
AGENTS.md
```

## Completion criteria

This hint can be deleted when:

- Milestone 006 is indexed with the correct status.
- `docs/specs/content-validation-contract.md` is indexed.
- `docs/artifacts/content-validation-artifact-contract.md` is indexed.
- ADR-0011 is indexed after acceptance.
- Engineering docs list `eng/content-validate.sh` only if implemented.
- Product CLI docs list `agentic2d content validate` only if implemented.
- README and AGENTS are updated only if their current examples or routing become stale.

## Notes

This file is deferred documentation synchronization metadata. Ordinary implementation agents must ignore `.guide-sync/`.
