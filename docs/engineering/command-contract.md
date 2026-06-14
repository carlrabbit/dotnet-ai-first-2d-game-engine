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

## Future focused commands

```text
./eng/test-project.sh <project-or-path>
./eng/test-filter.sh <filter>
./eng/check-affected.sh
./eng/schema-validate.sh <path-or-scope>
./eng/scenario.sh <scenario-id-or-path>
```

## Future artifact-first commands

```text
./eng/scenario-packaged.sh <scenario-id>
./eng/artifacts-validate.sh <artifact-path>
./eng/review-pack.sh <run-id-or-artifact-path>
```

## Rule for creating commands

A command must either validate meaningful state or fail clearly with an explanation that the required substrate has not been initialized.

Do not create success-only placeholder scripts.
