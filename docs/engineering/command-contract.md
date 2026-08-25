# Command Contract

## Authority

This document is authoritative for engineering command expectations.

## Current base commands

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh
./eng/check.sh
```

These commands are the canonical local engineering API.

| Command | Behavior | Validation tier |
|---|---|---|
| `./eng/restore.sh` | Restores `dotnet-ai-first-2d-game-engine.slnx`. | Tier 1 focused implementation |
| `./eng/build.sh` | Builds `dotnet-ai-first-2d-game-engine.slnx` with `--no-restore`. | Tier 1 focused implementation |
| `./eng/test.sh` | Runs fast unit tests under `tests/unit/Agentic2D.Tests.Unit` with `--no-build`. | Tier 1 focused implementation |
| `./eng/format.sh` | Applies `dotnet format` to the solution. | Tier 1 focused implementation |
| `./eng/format.sh --verify` | Verifies formatting with `--verify-no-changes`. | Tier 1 focused implementation |
| `./eng/check.sh` | Runs restore, build, test, and format verification. | Tier 2 standard local gate |
| `./eng/cli-smoke.sh` | Runs product CLI help/version checks and `agentic2d runtime smoke` through `src/Agentic2D.Tools`. | Tier 1/2 focused product CLI smoke |
| `./eng/product-validate.sh` | Runs `agentic2d validate` through `src/Agentic2D.Tools`. | Tier 2 product validation gate for current maturity |
| `./eng/scenario-smoke.sh` | Runs authored `runtime.smoke` through `agentic2d scenario run` and verifies `result.json`, `events.jsonl`, and `diagnostics.json` exist. | Tier 2 scenario validation gate for current maturity |
| `./eng/content-validate.sh <scope-or-path>` | Runs `agentic2d content validate <scope-or-path>` through `src/Agentic2D.Tools` and verifies content validation artifacts exist. | Tier 2 content validation gate for current maturity |
| `./eng/asset-inspect-smoke.sh` | Runs `agentic2d asset inspect asset.tile-atlas-smoke` through `src/Agentic2D.Tools` and verifies `result.json`, `diagnostics.json`, `asset-summary.json`, and `tiles.json` exist. | Tier 2 asset smoke gate for current maturity |
| `./eng/review-pack-smoke.sh` | Runs required smoke artifact producers, runs `agentic2d review pack --input artifacts`, and verifies `review-summary.md`, `review-manifest.json`, and `diagnostics.json` exist. | Tier 2 review pack smoke gate for current maturity |
| `./eng/asset-curation-smoke.sh` | Runs or refreshes the smoke review pack, runs `agentic2d asset curate` for `asset.tile-atlas-smoke`, and verifies `index.html`, `review-data.json`, and `diagnostics.json` exist. | Tier 2 asset curation smoke gate for current maturity |
| `./eng/asset-review-smoke.sh` | Runs `agentic2d asset review apply` dry-run and isolated real-apply smoke checks, verifies stale-fingerprint rejection and post-apply validation, and leaves the tracked worktree unchanged. | Tier 2 asset authoring smoke gate |
| `./eng/asset-perception-smoke.sh` | Runs `agentic2d asset perceive asset.tile-atlas-smoke` and verifies required perception artifacts exist. | Tier 2 asset perception smoke gate |
| `./eng/map-smoke.sh` | Runs map scope and direct-path content validation plus `agentic2d map inspect`, and verifies required map artifacts exist. | Tier 2 map smoke gate |
| `./eng/runtime-inspect-smoke.sh` | Runs `agentic2d runtime inspect --scenario runtime.smoke --map map.smoke` and verifies required runtime inspection artifacts exist. | Tier 2 runtime inspection smoke gate |
| `./eng/m011-smoke.sh` | Runs the bounded Milestone 011 end-to-end smoke journey and verifies final review-pack/workbench artifacts while leaving tracked source unchanged. | Tier 2 milestone smoke gate |
| `./eng/asset-home-smoke.sh`, `./eng/asset-source-registry-smoke.sh`, `./eng/asset-source-profile-smoke.sh`, `./eng/asset-source-cleanup-smoke.sh`, `./eng/asset-source-annotation-smoke.sh`, `./eng/asset-campaign-smoke.sh`, `./eng/asset-batch-smoke.sh`, `./eng/asset-discovery-review-pack-smoke.sh` | Focused M028 local asset-home, discovery, annotation, campaign, batch, and headless-evidence validation. | Tier 1/2 provider validation |
| `./eng/simulation-world-smoke.sh`, `./eng/simulation-time-smoke.sh`, `./eng/simulation-command-event-smoke.sh`, `./eng/simulation-activity-reservation-smoke.sh`, `./eng/simulation-persistence-smoke.sh`, `./eng/simulation-inspection-smoke.sh`, `./eng/m031-wood-workflow-smoke.sh` | M031 partitioned-world, semantic-time, command/event, activity/reservation, persistence, inspection, and bounded proof evidence. | Tier 1/2 focused simulation validation |

## Future focused commands

```text
./eng/test-project.sh <project-or-path>
./eng/test-filter.sh <filter>
./eng/check-affected.sh
./eng/schema-validate.sh <path-or-scope>
./eng/scenario.sh <scenario-id-or-path>
```

## Future artifact-first commands
| `./eng/workspace-directory-reference-smoke.sh` | Creates, validates, runs, and inspects a directory-reference consumer workspace without mutating engine source. | Tier 2 milestone smoke gate |
| `./eng/workspace-directory-copy-smoke.sh` | Proves deterministic copy exclusions and consumer execution. | Tier 2 milestone smoke gate |
| `./eng/workspace-local-git-smoke.sh` | Proves exact-revision local Git acquisition without network access. | Tier 2 milestone smoke gate |
| `./eng/workspace-minimal-game-run-smoke.sh` | Proves generated consumer wrappers, run manifest, inspection, and review. | Tier 2 milestone smoke gate |
| `./eng/m018-smoke.sh` | Runs the bounded M018 provider/consumer smoke suite. | Tier 2 milestone smoke gate |
| `./eng/m018-directory-reference-smoke.sh`, `./eng/m018-directory-copy-smoke.sh`, `./eng/m018-local-git-smoke.sh`, `./eng/m018-consumer-workflow-smoke.sh` | Bounded M018 shards for constrained runners; each delegates to one black-box provider or consumer journey. | Tier 2 milestone smoke gate |
| `./eng/m018-consumer-bootstrap-smoke.sh <temporary-root>`, `./eng/m018-consumer-run-smoke.sh <workspace>`, `./eng/m018-consumer-review-smoke.sh <workspace>` | Stages the generated consumer journey for constrained runners: bootstrap, run evidence, then inspection/review. | Tier 2 milestone smoke gate |

```text
./eng/scenario-packaged.sh <scenario-id>
./eng/artifacts-validate.sh <artifact-path>
```

## Rule for creating commands

A command must either validate meaningful state or fail clearly with an explanation that the required substrate has not been initialized.

Do not create success-only placeholder scripts.
| `./eng/behavior-smoke.sh` | Validates behavior registration, activation, lifecycle scheduling, and intent emission. | Tier 2 milestone smoke gate |
| `./eng/grid-spatial-smoke.sh` | Verifies accepted and expected-rejected `spatial.grid` movement evidence. | Tier 2 milestone smoke gate |
| `./eng/m012-smoke.sh` | Executes the bounded behavior/grid scenario-to-review-pack journey. | Tier 2 milestone smoke gate |

## Resumable validation commands

`m019-smoke.sh`, `m020-smoke.sh`, `m021-smoke.sh`, `m023-smoke.sh`, `m026-smoke.sh`, `m027-smoke.sh`, `m028-smoke.sh`, `m031-smoke.sh`, `m032-smoke.sh`, `m033-smoke.sh`, and `guide-migration-v050.sh` are resumable-sharded suites. They expose `--list`, `--plan-json`, `--shard <id>`, `--verify`, and no-argument local/CI aggregate mode. `--verify` is the only aggregate-success authority.

`./eng/perf-smoke.sh` captures bounded reference-workload evidence. `perf-capture`, `perf-compare`, and `perf-report` are thin launchers over the engineering host and produce advisory same-machine performance evidence; elapsed timing is never a deterministic receipt fingerprint or cross-machine claim. M026 retains small-workload counters/allocations but classifies sub-10-ms references as not timing-authoritative; scaled real workloads carry ordinary elapsed comparison authority.

`./eng/geometry-diagnostics-smoke.sh` is the mandatory headless geometry evidence path. `./eng/geometry-graphics-capture.sh` is explicitly opt-in and requires a supported graphics session; it captures the all-supported-shapes fixture through the isolated raylib debug client and can be compared with structural inspection through `geometry preview --graphical-metadata <capture-metadata.json>`.

`./eng/m029-smoke.sh` is resumable-sharded. It has `--list`, `--plan-json`, `--shard <id>`, `--verify`, and no-argument modes. Its verifier is the only aggregate-success authority; its human-review shard remains pending until the owning M029 review is approved.

`./eng/m031-smoke.sh` is resumable-sharded. It has `--list`, `--plan-json`, `--shard <id>`, `--verify`, and no-argument modes. Its verifier is the only aggregate-success authority; its human-review shard remains pending until the owning M031 review is approved.

`pwsh ./eng/suite.ps1 m040-smoke --plan-json|--shard <id>|--verify` is the canonical M040 resumable-sharded machine suite. Its verifier is the only aggregate-success authority; M040 has no human-review shard.

`./eng/m032-smoke.sh` is resumable-sharded. It has `--list`, `--plan-json`, `--shard <id>`, `--verify`, and no-argument modes. Its verifier is the only aggregate-success authority; its human-review shard remains pending until the owning M032 review is approved. `m032-detailed-region-graphics-smoke.sh` always records `passed`, `failed`, or `skipped-not-graphics-capable`; a skip does not satisfy the M032 blocking review.

`./eng/m033-smoke.sh` is resumable-sharded. It has `--list`, `--plan-json`, `--shard <id>`, `--verify`, and no-argument modes. Its verifier requires current receipts, graphics-capable switch evidence, and approved M033 blocking review. Focused M033 commands write queue, transition, persistence, equivalence, and standalone evidence under `artifacts/simulation/M033/`.

`./eng/m034-smoke.sh` is resumable-sharded. It has `--list`, `--plan-json`, `--shard <id>`, `--verify`, and no-argument modes. Its verifier requires current receipts, graphics-capable operations proof, and approved M034 blocking review. `construction-lifecycle-smoke`, `water-infrastructure-smoke`, `farm-production-smoke`, `comfort-capacity-smoke`, `maintenance-failure-smoke`, `road-travel-modifier-smoke`, `settlement-alert-smoke`, `operations-surface-smoke`, `infrastructure-persistence-smoke`, and `m034-settlement-smoke` are focused M034 structural validations; graphical evidence is conditional and an explicit skip cannot satisfy review.

`./eng/tic-tac-toe-play.sh` starts the consumer-owned graphical launcher. It requires a graphics session and accepts `--frames <count>` for bounded graphics smoke use. `./eng/tic-tac-toe-export.sh` publishes the same playable launcher to `artifacts/tic-tac-toe/export/playable-linux-x64/AutonomousTicTacToe.Playable` in addition to its headless equivalence evidence.

Receipts are generated at `artifacts/validation/<suite>/<shard>.json`. The host deletes a previous receipt before execution and only atomically replaces it after command success and evidence validation.

The canonical six-command engineering review family, including positional review targets, current aliases, atomic alias-map storage, and historical reopening restrictions, is defined in `docs/engineering/human-review-workflow.md`. Durable files always use canonical review IDs.

`./eng/m027-smoke.sh` is resumable-sharded. It has `--list`, `--plan-json`, `--shard <id>`, `--verify`, and no-argument modes. Its human-review shard remains pending until the owning M027 review is approved.

The Bash launchers delegate structured semantics to `src/Agentic2D.Engineering`; they are not product commands.

M037 is dispatched through the generic resumable interface: `pwsh ./eng/suite.ps1 m037-smoke --plan-json|--shard <id>|--verify` (or `./eng/suite.sh m037-smoke ...` on Bash). Its blocking human-review shard remains pending until the repository-user review is approved.

M038 is dispatched through the generic resumable interface: `pwsh ./eng/suite.ps1 m038-smoke --plan-json`, each required `--shard <id>` in a separate invocation, then `--verify` (or the equivalent `./eng/suite.sh` commands on Bash). The aggregate verifier is machine-only and does not require human approval. The canonical bounded human entry point is `pwsh ./eng/review-run.ps1 <review-id-or-alias>` or `./eng/review-run.sh <review-id-or-alias>`; it requires current M038 machine prerequisites before launching the isolated Raylib workbench.

The M037 graphical shell smoke is `dotnet run --project src/Agentic2D.DebugClient.Raylib -- shell --frames <count> --capture <png>` in a graphics-capable environment.

PowerShell 7 launchers delegate to the same host and are the native Windows surface:

```text
pwsh ./eng/restore.ps1
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/test-filter.ps1 <filter>
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
pwsh ./eng/review-list.ps1 [filters]
pwsh ./eng/review-show.ps1 <review-id-or-alias>
pwsh ./eng/review-check.ps1 --milestone <id>
pwsh ./eng/suite.ps1 <suite-id> --plan-json|--shard <id>|--verify
pwsh ./eng/m036-smoke.ps1 --plan-json|--shard <id>|--verify
```

M036 is the first cross-platform resumable suite. Its aggregate verifier consumes current receipts and both explicit platform reports; one platform, a graphics skip, or partial shard output is not completion.
