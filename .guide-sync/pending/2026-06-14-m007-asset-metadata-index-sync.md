# Guide Sync Hint — M007 Asset Metadata Index Sync

## Status

Pending.

## Created by

Milestone 007 planning package.

## Purpose

After Milestone 007 is applied and implemented, synchronize repository indexes and command references for the new asset metadata and asset inspection capability.

## Scope

Review and update only as needed:

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
```

## Expected updates

- Index `docs/milestones/MILESTONE-007-asset-metadata-and-tile-atlas-curation-slice.md`.
- Index `docs/specs/asset-metadata-contract.md`.
- Index `docs/artifacts/asset-inspection-artifact-contract.md`.
- Index `docs/decisions/ADR-0012-asset-metadata-before-visual-workbench.md` if accepted.
- Document `agentic2d asset inspect <asset-id-or-path> --output <directory>` after implementation.
- Document `./eng/asset-inspect-smoke.sh` and `./eng/content-validate.sh assets` after implementation.
- Record `Agentic2D.AssetPipeline` as current only if the implementation actually creates that project.

## Completion criteria

Delete this hint only when all relevant index and cross-link updates are complete or when remaining work is narrowed into a more specific pending hint.
