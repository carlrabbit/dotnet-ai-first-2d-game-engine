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
agentic2d validate --output <directory>
agentic2d scenario run <scenario-id-or-path> --output <directory>
agentic2d content validate <scope-or-path> --output <directory>
agentic2d asset inspect <asset-id-or-path> --output <directory>
```

Development equivalents:

```bash
dotnet run --project src/Agentic2D.Tools -- --help
dotnet run --project src/Agentic2D.Tools -- --version
dotnet run --project src/Agentic2D.Tools -- runtime smoke --output artifacts/cli/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- validate --output artifacts/cli/validate
dotnet run --project src/Agentic2D.Tools -- scenario run game/scenarios/smoke/runtime-smoke.json --output artifacts/scenarios/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- scenario run runtime.smoke --output artifacts/scenarios/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- content validate scenarios --output artifacts/content/scenarios
dotnet run --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/runtime-smoke.json --output artifacts/content/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- content validate assets --output artifacts/content/assets
dotnet run --project src/Agentic2D.Tools -- content validate game/assets/metadata/tile-atlas-smoke.asset.json --output artifacts/content/tile-atlas-smoke
dotnet run --project src/Agentic2D.Tools -- asset inspect asset.tile-atlas-smoke --output artifacts/assets/tile-atlas-smoke
dotnet run --project src/Agentic2D.Tools -- asset inspect game/assets/metadata/tile-atlas-smoke.asset.json --output artifacts/assets/tile-atlas-smoke
```

## Command table

| Command | Purpose | Artifact output | Validation tier |
|---|---|---|---:|
| `agentic2d --help` | Show available product CLI commands. | None required. | Tier 1 |
| `agentic2d --version` | Show CLI/runtime version. | None required. | Tier 1 |
| `agentic2d runtime smoke --output <directory>` | Run minimal deterministic runtime smoke execution. | `<directory>/result.json` | Tier 1 |
| `agentic2d validate --output <directory>` | Run current product validation for the minimal runtime maturity. | `<directory>/result.json` | Tier 2 when called by `eng/product-validate.sh` |
| `agentic2d scenario run <scenario-id-or-path> --output <directory>` | Run an authored scenario through the scenario runner. | `<directory>/result.json`, `<directory>/events.jsonl`, `<directory>/diagnostics.json` | Tier 2 when called by `eng/scenario-smoke.sh` |
| `agentic2d content validate <scope-or-path> --output <directory>` | Validate authored content without running runtime behavior. Supported scopes are `scenarios` and `assets`. | `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/validated-items.json` | Tier 2 when called by `eng/content-validate.sh` |
| `agentic2d asset inspect <asset-id-or-path> --output <directory>` | Inspect authored asset metadata and referenced raw PNG structure. | `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/asset-summary.json`, `<directory>/tiles.json` | Tier 2 when called by `eng/asset-inspect-smoke.sh` |

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
./eng/asset-inspect-smoke.sh
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
