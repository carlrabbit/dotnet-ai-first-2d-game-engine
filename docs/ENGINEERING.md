# Engineering

## Authority

This document indexes build, validation, commands, and project-layout policy.

## Current status

The repository includes deterministic runtime, product CLI, scenarios, content validation, asset workflows, maps, runtime inspection, behavior/spatial systems, entity/component runtime, definitions and interactions, backend-neutral rendering, and an isolated raylib-cs debug client.

## Indexed documents

| Document | Purpose |
|---|---|
| `docs/engineering/command-contract.md` | Canonical engineering commands. |
| `docs/engineering/validation-tiers.md` | Validation tiers and graphics-capable distinction. |
| `docs/engineering/future-dotnet-solution.md` | Current and candidate solution shape. |
| `docs/engineering/product-cli.md` | Product CLI invocation. |

## Canonical commands

```bash
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
./eng/check.sh
```

## Current capability wrappers

```text
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/content-validate.sh maps
./eng/content-validate.sh entities
./eng/content-validate.sh visuals
./eng/asset-inspect-smoke.sh
./eng/review-pack-smoke.sh
./eng/asset-curation-smoke.sh
./eng/asset-review-smoke.sh
./eng/asset-perception-smoke.sh
./eng/map-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/m011-smoke.sh
./eng/behavior-smoke.sh
./eng/grid-spatial-smoke.sh
./eng/m012-smoke.sh
./eng/entity-runtime-smoke.sh
./eng/continuous-spatial-smoke.sh
./eng/m013-smoke.sh
./eng/entity-definition-smoke.sh
./eng/workspace-directory-reference-smoke.sh
./eng/workspace-directory-copy-smoke.sh
./eng/workspace-local-git-smoke.sh
./eng/workspace-minimal-game-run-smoke.sh
./eng/m018-smoke.sh
./eng/m018-directory-reference-smoke.sh
./eng/m018-directory-copy-smoke.sh
./eng/m018-local-git-smoke.sh
./eng/m018-consumer-workflow-smoke.sh
./eng/m018-consumer-bootstrap-smoke.sh <temporary-root>
./eng/m018-consumer-run-smoke.sh <workspace>
./eng/m018-consumer-review-smoke.sh <workspace>
./eng/spatial-query-trigger-smoke.sh
./eng/interaction-smoke.sh
./eng/m014-smoke.sh
./eng/visual-content-smoke.sh
./eng/render-projection-smoke.sh
./eng/raylib-debug-client-smoke.sh
./eng/m015-smoke.sh
./eng/input-content-smoke.sh
./eng/input-mapping-smoke.sh
./eng/input-runtime-smoke.sh
./eng/input-replay-smoke.sh
./eng/m016-smoke.sh
./eng/animation-content-smoke.sh
./eng/animation-sampling-smoke.sh
./eng/animation-marker-smoke.sh
./eng/animated-render-smoke.sh
./eng/animation-replay-smoke.sh
./eng/m017-smoke.sh
./eng/sound-content-smoke.sh
./eng/sound-marker-cue-smoke.sh
./eng/sound-loop-ownership-smoke.sh
./eng/gameplay-damage-resource-smoke.sh
./eng/gameplay-defeat-lifecycle-smoke.sh
./eng/gameplay-collection-atomicity-smoke.sh
./eng/gameplay-integrated-smoke.sh
./eng/gameplay-replay-smoke.sh
./eng/m019-smoke.sh
./eng/perf-smoke.sh
./eng/perf-capture.sh --label <label> --output <directory>
./eng/perf-compare.sh <before-directory> <after-directory> --output <directory>
./eng/perf-report.sh --milestone <id> --before <before-directory> --after <after-directory> --output <directory>
./eng/m023-smoke.sh
./eng/geometry-diagnostics-smoke.sh
./eng/geometry-graphics-capture.sh # graphics-capable environment only
./eng/generated-sound-linkage-smoke.sh
./eng/scaled-performance-smoke.sh
./eng/m026-performance-report.sh
./eng/tic-tac-toe-validate.sh
./eng/tic-tac-toe-play.sh # graphics-capable environment only
./eng/tic-tac-toe-smoke.sh
./eng/tic-tac-toe-isolation.sh
./eng/tic-tac-toe-export.sh
./eng/tic-tac-toe-review.sh
./eng/m026-smoke.sh
./eng/review-migration-smoke.sh
./eng/geometry-review-pack-smoke.sh
./eng/generated-sound-review-pack-smoke.sh
./eng/consumer-authoring-review-pack-smoke.sh
./eng/scenario-diagnostics-smoke.sh
./eng/persistence-diagnostics-smoke.sh
./eng/m027-smoke.sh
./eng/asset-home-smoke.sh
./eng/asset-source-registry-smoke.sh
./eng/asset-source-profile-smoke.sh
./eng/asset-source-cleanup-smoke.sh
./eng/asset-source-annotation-smoke.sh
./eng/asset-campaign-smoke.sh
./eng/asset-batch-smoke.sh
./eng/asset-discovery-review-pack-smoke.sh
./eng/m028-smoke.sh
./eng/m028-m011-audit.sh
./eng/m028-generalization-smoke.sh
./eng/asset-workbench-session-smoke.sh
./eng/asset-workbench-alias-smoke.sh
./eng/asset-workbench-input-smoke.sh
./eng/asset-workbench-rdp-input-smoke.sh
./eng/asset-workbench-mouse-input-smoke.sh
./eng/asset-workbench-decision-smoke.sh
./eng/asset-workbench-consequence-smoke.sh
./eng/asset-preview-ipc-smoke.sh
./eng/asset-preview-recovery-smoke.sh
./eng/asset-preview-graphical-smoke.sh
./eng/asset-preview-audio-smoke.sh
./eng/asset-promotion-smoke.sh
./eng/asset-affected-rebuild-smoke.sh
./eng/asset-workbench-review-pack-smoke.sh
./eng/m029-smoke.sh
```

## Validation boundary

- Headless structural validation is mandatory in ordinary environments.
- `raylib-debug-client-smoke.sh` requires a documented graphics-capable environment.
- M015 must report graphics smoke as passed, failed, or explicitly skipped; it must not silently claim execution.
- Screenshot capture is explicit and not part of every ordinary run.

## Command rule

Commands must validate meaningful state or fail clearly. Success-only placeholders are prohibited.

## Constrained validation and review

`src/Agentic2D.Engineering` is the tested .NET host for validation plans, fingerprints, atomic receipts, fast verification, and repository-local review state. `eng/*.sh` remains the stable engineering API and only forwards arguments and exit codes.

The resumable suites are `./eng/m019-smoke.sh`, `./eng/m020-smoke.sh`, `./eng/m021-smoke.sh`, `./eng/m023-smoke.sh`, `./eng/m026-smoke.sh`, and `./eng/guide-migration-v050.sh`. Each supports `--list`, `--plan-json`, `--shard <id>`, and `--verify`; a no-argument run is for unconstrained local/CI use. Performance comparisons are advisory same-machine evidence, never deterministic validation authority. M026 marks sub-10-ms references not timing-authoritative and uses bounded scaled real workloads for elapsed comparison.

Repository-local review state is `.review/pending/`, `.review/records/`, and `.review/closed/`; generated or large evidence belongs under `artifacts/review/`. The full six-command review family, alias behavior, and reopening policy are authoritative in `docs/engineering/human-review-workflow.md`. Completed records are immutable historical evidence and do not stale because later commits change the repository.

The tested engineering baseline is Linux with Bash, Git, .NET SDK 10.0.109, and same-filesystem atomic file replacement. Native Windows and PowerShell are not supported or claimed. Graphics smoke remains conditional on its documented graphics-capable environment.
