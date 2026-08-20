# Engineering

## Authority

This document indexes build, validation, commands, and project-layout policy.

## Current status

The repository includes deterministic runtime, product CLI, scenarios, content validation, asset workflows, maps, runtime inspection, behavior/spatial systems, entity/component runtime, definitions and interactions, backend-neutral rendering, and an isolated raylib-cs debug client.

M037 development support is native Linux/Bash and Windows/PowerShell 7. The active development epoch is Windows beginning at M036; Linux is supported but inactive. Linux platform-sensitive verification from M036 and M037 is deferred and tracked in `eng/platform-verification.json`; current milestone completion does not claim fresh evidence on both platforms. `src/Agentic2D.Engineering` owns suite definitions, process selection, fingerprints, receipts, temporary-file/atomic replacement policy, review operations, and platform evidence. `eng/*.sh` and `eng/*.ps1` are thin adapters. Linux export remains platform-specific; Windows export remains out of scope.

## Indexed documents

| Document | Purpose |
|---|---|
| `docs/engineering/command-contract.md` | Canonical engineering commands. |
| `docs/engineering/validation-tiers.md` | Validation tiers and graphics-capable distinction. |
| `docs/engineering/platform-verification.md` | Platform epochs, active-platform proof, and deferred inactive-platform verification. |
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

PowerShell 7 equivalents are `pwsh ./eng/restore.ps1`, `build.ps1`, `test.ps1`, `format.ps1 --verify`, and `check.ps1`. The shared resumable interface is `pwsh ./eng/suite.ps1 <suite-id> --plan-json|--shard <id>|--verify`; M036 also has the documented convenience wrapper `pwsh ./eng/m036-smoke.ps1`.

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
./eng/simulation-world-smoke.sh
./eng/simulation-time-smoke.sh
./eng/simulation-command-event-smoke.sh
./eng/simulation-activity-reservation-smoke.sh
./eng/simulation-persistence-smoke.sh
./eng/simulation-inspection-smoke.sh
./eng/m031-wood-workflow-smoke.sh
./eng/m031-smoke.sh
./eng/designation-work-smoke.sh
./eng/worker-selection-smoke.sh
./eng/detailed-grid-navigation-smoke.sh
./eng/detailed-activity-execution-smoke.sh
./eng/logistics-conservation-smoke.sh
./eng/basic-needs-interruption-smoke.sh
./eng/detailed-region-persistence-smoke.sh
./eng/detailed-region-projection-smoke.sh
./eng/m032-forest-logistics-smoke.sh
./eng/m032-detailed-region-graphics-smoke.sh # graphics-capable environment required for passing evidence
./eng/m032-smoke.sh
./eng/discrete-event-scheduler-smoke.sh
./eng/abstract-activity-smoke.sh
./eng/abstract-travel-smoke.sh
./eng/abstract-needs-smoke.sh
./eng/region-fidelity-smoke.sh
./eng/region-reconciliation-smoke.sh
./eng/multi-fidelity-persistence-smoke.sh
./eng/multi-fidelity-equivalence-smoke.sh
./eng/standalone-simulation-smoke.sh
./eng/m033-multi-region-smoke.sh
./eng/m033-region-switch-graphics-smoke.sh # graphics-capable environment required for passing review evidence
./eng/m033-smoke.sh
./eng/construction-lifecycle-smoke.sh
./eng/water-infrastructure-smoke.sh
./eng/farm-production-smoke.sh
./eng/comfort-capacity-smoke.sh
./eng/maintenance-failure-smoke.sh
./eng/road-travel-modifier-smoke.sh
./eng/settlement-alert-smoke.sh
./eng/operations-surface-smoke.sh
./eng/infrastructure-persistence-smoke.sh
./eng/m034-settlement-smoke.sh
./eng/m034-settlement-graphics-smoke.sh # graphics-capable environment required for passing review evidence
./eng/m034-smoke.sh
./eng/performance-budget-smoke.sh
./eng/runtime-health-smoke.sh
./eng/deadlock-detection-smoke.sh
./eng/fault-injection-smoke.sh
./eng/save-compatibility-smoke.sh
./eng/save-recovery-smoke.sh
./eng/reproduction-bundle-smoke.sh
./eng/internal-test-session-smoke.sh
./eng/m035-readiness-smoke.sh
./eng/m035-graphical-soak-smoke.sh # graphics-capable environment required for passing readiness evidence
./eng/m035-smoke.sh
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

The resumable suites are `./eng/m019-smoke.sh`, `./eng/m020-smoke.sh`, `./eng/m021-smoke.sh`, `./eng/m023-smoke.sh`, `./eng/m026-smoke.sh`, `./eng/m029-smoke.sh`, `./eng/m031-smoke.sh`, `./eng/m032-smoke.sh`, `./eng/m033-smoke.sh`, `./eng/m034-smoke.sh`, `./eng/m035-smoke.sh`, and `./eng/guide-migration-v050.sh`. Each supports `--list`, `--plan-json`, `--shard <id>`, and `--verify`; a no-argument run is for unconstrained local/CI use. Performance comparisons are advisory same-machine evidence, never deterministic validation authority. M026 marks sub-10-ms references not timing-authoritative and uses bounded scaled real workloads for elapsed comparison.

Repository-local review state is `.review/pending/`, `.review/records/`, and `.review/closed/`; generated or large evidence belongs under `artifacts/review/`. The full six-command review family, alias behavior, and reopening policy are authoritative in `docs/engineering/human-review-workflow.md`. Completed records are immutable historical evidence and do not stale because later commits change the repository.

The tested engineering baseline is Linux/Bash and native Windows/PowerShell 7 with Git, .NET SDK 10.0.109, and same-filesystem atomic file replacement. Graphics smoke remains conditional on its documented graphics-capable environment.

`./eng/m034-smoke.sh` is resumable-sharded. It has `--list`, `--plan-json`, `--shard <id>`, `--verify`, and no-argument modes. Its verifier requires current receipts, graphics-capable operations proof, and approved M034 blocking review. Focused commands write construction, flow, production, maintenance, operations, persistence, and sustained-settlement evidence under `artifacts/simulation/M034/`.

`./eng/m035-smoke.sh` is resumable-sharded. Its verifier requires current direct and nested campaign receipts, a completed four-hour graphics-capable soak, an allowed readiness decision, and approved M035 blocking review. It writes readiness evidence under `artifacts/readiness/M035/`; `--verify` is the only aggregate-success authority.

M037 uses the shared `suite.ps1` / `suite.sh` interface with `m037-smoke`; its receipt root is `artifacts/validation/m037-smoke/` and its product-shell evidence root is `artifacts/application/M037/`.

The standalone host exposes `agentic2d-game --product-shell` for the player-facing Raylib shell, with `--safe-mode` and `--reset-user-settings` startup recovery options. The shell writes a bounded startup projection before any scenario execution.

The isolated Raylib adapter also exposes `dotnet run --project src/Agentic2D.DebugClient.Raylib -- shell --frames <count> --capture <png>` for bounded graphical shell evidence; it remains adapter-only and does not become runtime authority.
