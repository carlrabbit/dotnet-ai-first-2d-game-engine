# Product CLI

## Authority

This document is authoritative for the repository-local product CLI command contract.

This document complements `docs/specs/product-cli-contract.md`, which defines product CLI behavior. This document records how the CLI is invoked and validated inside this repository.

## Product CLI principle

The product CLI is the engine/runtime API for agents, CI, and humans. It is separate from `eng/` repository scripts.

The product CLI must execute real product behavior and produce structured artifacts. It must not contain success-only placeholders.

## CLI host

The CLI host project is:

```text
src/Agentic2D.Tools
```

The product command identity is:

```text
agentic2d
```

Until packaging or tool installation exists, use the development invocation form:

```bash
dotnet run --project src/Agentic2D.Tools -- <args>
```

## Current command surface

The currently supported product CLI commands are:

```text
agentic2d --help
agentic2d --version
agentic2d runtime smoke --output <directory>
agentic2d runtime inspect --scenario <scenario-id-or-path> [--map <map-id-or-path>] --output <directory>
agentic2d validate --output <directory>
agentic2d scenario run <scenario-id-or-path> --output <directory>
agentic2d content validate <scope-or-path> --output <directory>
agentic2d asset inspect <asset-id-or-path> --output <directory>
agentic2d asset perceive <asset-id-or-path> --output <directory>
agentic2d asset review apply --decisions <review-file> [--dry-run] --output <directory>
agentic2d map inspect <map-id-or-path> --output <directory>
agentic2d review pack --input <artifact-root> --output <directory>
agentic2d asset curate --asset <asset-id-or-path> --review-pack <review-pack-path> --output <directory>
agentic2d input inspect <input-sequence-id> --input-map <input-map-id> --output <directory>
agentic2d input replay --scenario <scenario-id> --recording <recording> --output <directory>
agentic2d animation inspect <animation-id-or-path> --output <directory>
agentic2d animation project --scenario <scenario-id> --output <directory>
agentic2d sound inspect <sound-id-or-path> --output <directory>
agentic2d sound project --project <project-or-workspace> --scenario <scenario-id> --output <directory>
agentic2d gameplay inspect --project <project-or-workspace> --scenario <scenario-id> --output <directory>
```

Development equivalents:

```bash
dotnet run --project src/Agentic2D.Tools -- --help
dotnet run --project src/Agentic2D.Tools -- --version
dotnet run --project src/Agentic2D.Tools -- runtime smoke --output artifacts/cli/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario runtime.smoke --map map.smoke --output artifacts/runtime/inspect
dotnet run --project src/Agentic2D.Tools -- validate --output artifacts/cli/validate
dotnet run --project src/Agentic2D.Tools -- scenario run game/scenarios/smoke/runtime-smoke.json --output artifacts/scenarios/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- scenario run runtime.smoke --output artifacts/scenarios/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- content validate scenarios --output artifacts/content/scenarios
dotnet run --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/runtime-smoke.json --output artifacts/content/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- content validate assets --output artifacts/content/assets
dotnet run --project src/Agentic2D.Tools -- content validate game/assets/metadata/tile-atlas-smoke.asset.json --output artifacts/content/tile-atlas-smoke
dotnet run --project src/Agentic2D.Tools -- content validate maps --output artifacts/content/maps
dotnet run --project src/Agentic2D.Tools -- content validate game/maps/smoke/map-smoke.map.json --output artifacts/content/map-smoke
dotnet run --project src/Agentic2D.Tools -- asset inspect asset.tile-atlas-smoke --output artifacts/assets/tile-atlas-smoke
dotnet run --project src/Agentic2D.Tools -- asset inspect game/assets/metadata/tile-atlas-smoke.asset.json --output artifacts/assets/tile-atlas-smoke
dotnet run --project src/Agentic2D.Tools -- asset perceive asset.tile-atlas-smoke --output artifacts/assets/perception/tile-atlas-smoke
dotnet run --project src/Agentic2D.Tools -- asset review apply --decisions game/assets/reviews/tile-atlas-smoke.review.json --dry-run --output artifacts/asset-review/dry-run
dotnet run --project src/Agentic2D.Tools -- map inspect map.smoke --output artifacts/maps/map-smoke
dotnet run --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/latest
dotnet run --project src/Agentic2D.Tools -- asset curate --asset asset.tile-atlas-smoke --review-pack artifacts/review/latest --output artifacts/workbench/asset-curation
```

## Command table

| Command | Purpose | Artifact output | Validation tier |
|---|---|---|---:|
| `agentic2d --help` | Show available product CLI commands. | None required. | Tier 1 |
| `agentic2d --version` | Show CLI/runtime version. | None required. | Tier 1 |
| `agentic2d runtime smoke --output <directory>` | Run minimal deterministic runtime smoke execution. | `<directory>/result.json` | Tier 1 |
| `agentic2d runtime inspect --scenario <scenario-id-or-path> [--map <map-id-or-path>] --output <directory>` | Execute deterministic runtime inspection and structured state projection. | `result.json`, `diagnostics.json`, `runtime-summary.json`, `entities.json`, `commands.jsonl`, `events.jsonl`, `final-state.json`, `assertions.json`, `content-references.json` | Tier 2 when called by `eng/runtime-inspect-smoke.sh` |
| `agentic2d validate --output <directory>` | Run current product validation for the minimal runtime maturity. | `<directory>/result.json` | Tier 2 when called by `eng/product-validate.sh` |
| `agentic2d scenario run <scenario-id-or-path> --output <directory>` | Run an authored scenario through the scenario runner. | `<directory>/result.json`, `<directory>/events.jsonl`, `<directory>/diagnostics.json` | Tier 2 when called by `eng/scenario-smoke.sh` |
| `agentic2d content validate <scope-or-path> --output <directory>` | Validate authored content without running runtime behavior. Supported scopes are `scenarios`, `assets`, and `maps`. | `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/validated-items.json` | Tier 2 when called by `eng/content-validate.sh` |
| `agentic2d asset inspect <asset-id-or-path> --output <directory>` | Inspect authored asset metadata and referenced raw PNG structure. | `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/asset-summary.json`, `<directory>/tiles.json` | Tier 2 when called by `eng/asset-inspect-smoke.sh` |
| `agentic2d asset perceive <asset-id-or-path> --output <directory>` | Decode bounded PNG pixels and emit deterministic feature/proposal evidence. | `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/tile-features.json`, `<directory>/semantic-proposals.json` | Tier 2 when called by `eng/asset-perception-smoke.sh` |
| `agentic2d asset review apply --decisions <review-file> [--dry-run] --output <directory>` | Validate authored review decisions, enforce source fingerprints, and safely apply approved metadata changes. | `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/mutation-plan.json`, `<directory>/validation-result.json`, `proposed-metadata.json` for dry-run | Tier 2 when called by `eng/asset-review-smoke.sh` |
| `agentic2d map inspect <map-id-or-path> --output <directory>` | Validate and inspect authored map content and resolved stable references. | `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/map-summary.json`, `<directory>/layers.json`, `<directory>/resolved-references.json` | Tier 2 when called by `eng/map-smoke.sh` |
| `agentic2d review pack --input <artifact-root> --output <directory>` | Aggregate current scenario, content validation, and asset inspection evidence into a review pack. | `<directory>/review-summary.md`, `<directory>/review-manifest.json`, `<directory>/diagnostics.json` | Tier 2 when called by `eng/review-pack-smoke.sh` |
| `agentic2d asset curate --asset <asset-id-or-path> --review-pack <review-pack-path> --output <directory>` | Generate a static, non-mutating asset curation workbench artifact. | `<directory>/index.html`, `<directory>/review-data.json`, `<directory>/diagnostics.json` | Tier 2 when called by `eng/asset-curation-smoke.sh` |

## Artifact contract

Artifact-producing commands must follow:

```text
docs/artifacts/product-cli-result-contract.md
```

Required artifact:

```text
<output>/result.json
```

Optional artifacts:

```text
<output>/events.jsonl
<output>/diagnostics.json
```

`agentic2d scenario run` uses the scenario artifact contract instead:

```text
docs/artifacts/scenario-runner-artifact-contract.md
```

`agentic2d content validate` uses the content validation artifact contract:

```text
docs/artifacts/content-validation-artifact-contract.md
```

`agentic2d asset inspect` uses the asset inspection artifact contract:

```text
docs/artifacts/asset-inspection-artifact-contract.md
```

`agentic2d asset perceive` and `agentic2d asset review apply` use the asset authoring artifact contract:

```text
docs/artifacts/asset-authoring-artifact-contract.md
```

`agentic2d map inspect` uses the map inspection artifact contract:

```text
docs/artifacts/map-inspection-artifact-contract.md
```

`agentic2d runtime inspect` uses the runtime inspection artifact contract:

```text
docs/artifacts/runtime-inspection-artifact-contract.md
```

`agentic2d review pack` uses the review pack artifact contract:

```text
docs/artifacts/review-pack-artifact-contract.md
```

`agentic2d asset curate` uses the asset curation workbench artifact contract:

```text
docs/artifacts/asset-curation-workbench-artifact-contract.md
```

## Exit codes

| Exit code | Meaning |
|---:|---|
| 0 | Command completed and validation passed. |
| 1 | Command completed and validation failed. |
| 2 | Invalid command-line usage or invalid scenario input. |
| 3 | Runtime execution error, artifact writing failure, or unhandled command failure. |

## Engineering wrappers

The current repository engineering wrappers for the product CLI are:

```text
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/content-validate.sh maps
./eng/asset-inspect-smoke.sh
./eng/review-pack-smoke.sh
./eng/asset-curation-smoke.sh
./eng/asset-review-smoke.sh
./eng/asset-perception-smoke.sh
./eng/map-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/m011-smoke.sh
```

Expected behavior:

```text
./eng/cli-smoke.sh
  runs product CLI help/version checks and runtime smoke execution

./eng/product-validate.sh
  runs `agentic2d validate` through the development invocation path

./eng/scenario-smoke.sh
  runs `agentic2d scenario run game/scenarios/smoke/runtime-smoke.json` and verifies required scenario artifacts exist

./eng/content-validate.sh scenarios
  runs `agentic2d content validate scenarios` and verifies required content validation artifacts exist

./eng/content-validate.sh assets
  runs `agentic2d content validate assets` and verifies required content validation artifacts exist

./eng/asset-inspect-smoke.sh
  runs `agentic2d asset inspect asset.tile-atlas-smoke` and verifies required asset inspection artifacts exist

./eng/review-pack-smoke.sh
  runs required smoke artifact producers, runs `agentic2d review pack`, and verifies required review pack artifacts exist

./eng/asset-curation-smoke.sh
  runs or refreshes the smoke review pack, runs `agentic2d asset curate`, and verifies required workbench artifacts exist
```

The wrappers are allowed to call:

```bash
dotnet run --project src/Agentic2D.Tools -- <args>
```

They must fail with a non-zero exit code when the product CLI fails.

## Current non-goals

The following commands are not required yet:

```text
agentic2d map preview <map-id>
agentic2d package build
```

Do not document these as supported until implemented by a later milestone.
## Behavior/grid validation wrappers

`./eng/behavior-smoke.sh`, `./eng/grid-spatial-smoke.sh`, and `./eng/m012-smoke.sh` validate the compiled behavior and `spatial.grid` reference slice. The grid wrapper executes both accepted and expected-rejected movement inspection paths.

## M015 render projection

`agentic2d render project --scenario <scenario-id> --tick final --output <directory>` produces headless, backend-neutral render evidence: `render-result.json`, `render-snapshot.json`, `render-frame.json`, `render-items.jsonl`, `render-commands.jsonl`, `asset-bindings.json`, and `render-diagnostics.json`. It does not initialize raylib or a graphics context.

The separate client is invoked with `dotnet run --project src/Agentic2D.DebugClient.Raylib -- scenario --scenario interaction.npc-smoke` or `snapshot --input <render-snapshot.json>`. Graphics smoke requires a native raylib 6.0-capable desktop display and reports skipped when `DISPLAY`/`WAYLAND_DISPLAY` is absent.

## M018 consumer workflow

`workspace create` transactionally scaffolds a `minimal-game` workspace from a source directory reference/copy or exact Git revision. It rejects non-empty targets and writes structured creation evidence. `workspace validate`, `project validate`, `project run`, `run inspect`, and `run review` are the consumer-facing workflow. Generated `eng/` wrappers delegate to one Bash launcher, which resolves the generated bootstrap projection and workspace validation verifies against authoritative `agentic2d.workspace.json`.

```bash
dotnet run --project src/Agentic2D.Tools -- workspace validate <workspace> --output <directory>
dotnet run --project src/Agentic2D.Tools -- project validate <project-or-workspace> --output <directory>
dotnet run --project src/Agentic2D.Tools -- project run <project-or-workspace> --scenario <scenario-id> --output <run-directory>
dotnet run --project src/Agentic2D.Tools -- run inspect <run-directory> --output <directory>
dotnet run --project src/Agentic2D.Tools -- run review <run-directory> --output <directory>
```
