# Milestone 003 — Introduce Product CLI Around the Minimal Runtime

## Goal

Create the first stable `agentic2d` product CLI surface around the minimal deterministic runtime created by Milestone 002.

This milestone turns the existing runtime/tool proof into an agent-operable product command surface with documented command syntax, deterministic artifact output, exit-code behavior, and engineering validation wrappers.

The milestone must not expand the engine runtime model. It must expose and validate the already-minimal runtime through a product CLI.

## Repository maturity and task mode

Repository maturity after this milestone:

```text
Implementation-ready for product CLI smoke usage.
Design-ready for broader scenario, content, asset, packaged-runtime, and public documentation work.
```

Task mode for this milestone:

```text
Implementation
Workflow/CI-adjacent only for local eng command wrappers
```

This is not a release-readiness task, public documentation task, asset-pipeline task, full scenario-runner task, or documentation synchronization task.

## Precondition

Milestone 002 must already be implemented in the working tree before this milestone starts.

Required Milestone 002 outcomes:

```text
minimal deterministic runtime exists
Agentic2D.Tools exists or is ready to become the product CLI host
a runtime smoke execution path exists or can be called without redesigning runtime semantics
a deterministic result artifact can be produced from the minimal runtime
```

If these outcomes are missing, the implementation agent must stop and report that Milestone 002 is incomplete instead of redesigning the runtime inside this milestone.

## Required authority

A later implementation agent must read only the following authority before implementing this milestone:

```text
README.md
AGENTS.md
docs/SPECS.md
docs/specs/runtime-principles.md
docs/specs/agentic-workflow.md
docs/specs/product-cli-contract.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/ARTIFACTS.md
docs/artifacts/product-cli-result-contract.md
docs/decisions/ADR-0008-product-cli-is-the-agent-facing-product-api.md
```

Also read Milestone 002's final implementation summary if it is available in the task context. Do not require broad reading across `docs/`.

Do not treat `docs/research/` as operational authority. Research documents are background only.

## Scope

Create or modify only the files needed to expose the minimal runtime through a product CLI and validate that CLI locally.

### Product CLI host

Use `src/Agentic2D.Tools` as the product CLI host.

If `Agentic2D.Tools` already exists from Milestone 002, extend it. If it does not exist but the runtime work exists elsewhere, create only the minimal console project required to expose the product CLI.

The CLI executable identity is:

```text
agentic2d
```

During local development, commands may be invoked through `dotnet run --project src/Agentic2D.Tools -- <args>` until packaging or tool installation exists.

### Required commands

Implement these commands:

```text
agentic2d --help
agentic2d --version
agentic2d runtime smoke --output <directory>
agentic2d validate --output <directory>
```

Development invocation examples:

```bash
dotnet run --project src/Agentic2D.Tools -- --help
dotnet run --project src/Agentic2D.Tools -- runtime smoke --output artifacts/cli/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- validate --output artifacts/cli/validate
```

### CLI validation wrappers

Create engineering wrappers only where they validate real product CLI behavior:

```text
eng/cli-smoke.sh
eng/product-validate.sh
```

`eng/cli-smoke.sh` must run a fast CLI smoke path.

`eng/product-validate.sh` must run the product-level local validation command for this milestone. At this stage, it may delegate to the same minimal runtime smoke path as `agentic2d validate`, but it must use the product CLI rather than directly invoking unit tests.

### Documentation updates directly required by this milestone

Replace or update:

```text
docs/engineering/product-cli.md
```

Create or update:

```text
docs/specs/product-cli-contract.md
docs/artifacts/product-cli-result-contract.md
```

Update directly affected engineering docs only if the implemented command surface would otherwise contradict them:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/future-dotnet-solution.md
```

Do not perform broad documentation synchronization.

## Non-goals

Do not implement any of the following in this milestone:

```text
new runtime semantics beyond adapting the existing minimal runtime
full scenario runner
agentic2d scenario run <scenario-id>
asset pipeline
content validation
map preview
animation workflow
renderer integration
raylib-cs integration
MonoGame integration
SDL/Silk.NET integration
source generators
packaged runtime mode
package installation as a .NET tool
NuGet packaging
GitHub Actions workflows
public documentation
release validation
benchmarks
human review gates
TBPs
issue templates
```

Do not create non-root `README.md` files.

Do not add runtime/game package dependencies.

Do not make `eng/` scripts the product API. `eng/` scripts are repository engineering wrappers; `agentic2d` is the product/runtime CLI.

## Required implementation conventions

### Product CLI shape

The CLI must use this command model:

```text
agentic2d <command> [subcommand] [options]
```

The first milestone-supported commands are:

```text
agentic2d runtime smoke --output <directory>
agentic2d validate --output <directory>
```

`runtime smoke` runs the minimal deterministic runtime smoke execution.

`validate` is the initial product validation command and must run the minimal runtime validation through the product CLI surface.

Both commands must produce a result artifact.

### Output directory behavior

Every artifact-producing command must accept:

```text
--output <directory>
```

If the output directory does not exist, the command must create it.

If the output directory already exists, the command may overwrite files it owns for the same command, but it must not delete unrelated files.

Required artifact path:

```text
<output>/result.json
```

Optional artifacts may include:

```text
<output>/events.jsonl
<output>/diagnostics.json
```

### Standard output behavior

For successful artifact-producing commands, stdout should be concise and human-readable.

It must include the output directory or `result.json` path so agents can locate evidence.

Do not rely on stdout as the source of truth. `result.json` is the command result contract.

### Exit codes

Use these initial exit codes:

| Exit code | Meaning |
|---:|---|
| 0 | Command completed and validation passed. |
| 1 | Command completed and validation failed. |
| 2 | Invalid command-line usage. |
| 3 | Runtime execution error or unhandled command failure. |

Unhandled exceptions should be converted into exit code `3` and a diagnostic artifact where possible.

### Determinism

The same command with the same runtime implementation and deterministic inputs must produce the same semantic result.

Timestamps, elapsed duration, absolute paths, environment-specific SDK paths, and machine-specific details must not be used for semantic assertions.

### No CLI framework requirement

A CLI framework may be used only if it is already available or if the implementation agent can justify that it materially reduces complexity. Prefer minimal built-in argument parsing for this milestone.

Do not add a heavy CLI framework or dependency graph solely for `--help`, `--version`, `runtime smoke`, and `validate`.

### Tests

Add or update fast unit tests for:

```text
argument parsing
exit-code mapping
result artifact creation
runtime smoke command success path
invalid usage failure path
```

Do not add slow tests, E2E tests, package smoke tests, or benchmarks.

## Focus areas

### Focus Area A — Product CLI contract and argument surface

#### Goal

Create a minimal but stable `agentic2d` command surface around the existing runtime.

#### Scope

Implement:

```text
agentic2d --help
agentic2d --version
agentic2d runtime smoke --output <directory>
agentic2d validate --output <directory>
```

The implementation may be a small hand-written parser.

#### Likely files or areas

```text
src/Agentic2D.Tools
src/Agentic2D.Tools/Program.cs
src/Agentic2D.Tools/**/*.cs
tests/unit/Agentic2D.Tests.Unit/**/*.cs
```

#### Validation tier

Tier 1 — Focused implementation.

#### Required validation

Run focused tests for the CLI parser/command behavior if a focused test command exists; otherwise run:

```bash
./eng/test.sh
```

#### Direct documentation impact

Update `docs/engineering/product-cli.md` if the implemented syntax differs from the planned syntax.

#### Deferred documentation impact

A later documentation synchronization pass may add CLI examples to `README.md` and agent-routing refinements to `AGENTS.md`.

### Focus Area B — Runtime smoke command artifact output

#### Goal

Expose the minimal deterministic runtime smoke execution through the product CLI and write structured evidence.

#### Scope

Implement:

```text
agentic2d runtime smoke --output <directory>
```

Required artifact:

```text
<output>/result.json
```

The command must call the existing minimal deterministic runtime rather than duplicating runtime behavior in the CLI layer.

#### Likely files or areas

```text
src/Agentic2D.Tools
src/Agentic2D.Engine
src/Agentic2D.Contracts
tests/unit/Agentic2D.Tests.Unit
```

#### Validation tier

Tier 1 — Focused implementation.

#### Required validation

Run:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --output artifacts/cli/runtime-smoke
```

Then verify that:

```text
artifacts/cli/runtime-smoke/result.json
```

exists and conforms to `docs/artifacts/product-cli-result-contract.md`.

#### Direct documentation impact

Update `docs/artifacts/product-cli-result-contract.md` only if the final artifact shape changes.

#### Deferred documentation impact

A later documentation synchronization pass may add artifact examples after the shape stabilizes.

### Focus Area C — Product validation command

#### Goal

Create the first product-level validation command.

#### Scope

Implement:

```text
agentic2d validate --output <directory>
```

At this milestone, `validate` must run the minimal deterministic runtime validation through the product CLI. It may be narrow, but it must be real.

It must not pretend to validate content, assets, maps, shaders, scenarios, packaged runtime, or public documentation.

#### Likely files or areas

```text
src/Agentic2D.Tools
tests/unit/Agentic2D.Tests.Unit
```

#### Validation tier

Tier 1 — Focused implementation.

#### Required validation

Run:

```bash
dotnet run --project src/Agentic2D.Tools -- validate --output artifacts/cli/validate
```

Then verify that:

```text
artifacts/cli/validate/result.json
```

exists and reports a passed validation status for the minimal runtime smoke path.

#### Direct documentation impact

Update `docs/engineering/product-cli.md` only if the final syntax or exit-code behavior changes.

#### Deferred documentation impact

A later documentation synchronization pass may decide whether `validate` should be mentioned in `README.md`.

### Focus Area D — Engineering wrappers for CLI smoke and product validation

#### Goal

Expose canonical repository validation commands for the product CLI without making `eng/` scripts the product API.

#### Scope

Create:

```text
eng/cli-smoke.sh
eng/product-validate.sh
```

Expected behavior:

```text
eng/cli-smoke.sh -> runs agentic2d --help and runtime smoke through src/Agentic2D.Tools
eng/product-validate.sh -> runs agentic2d validate through src/Agentic2D.Tools
```

Both scripts must fail non-zero when the product CLI fails.

#### Likely files or areas

```text
eng/cli-smoke.sh
eng/product-validate.sh
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

#### Validation tier

Tier 2 — Standard local gate plus product CLI smoke.

#### Required validation

Run:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
```

#### Direct documentation impact

Update:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

The docs must state that these two commands now exist and map them to validation tiers.

#### Deferred documentation impact

A later documentation synchronization pass may update `AGENTS.md` to mention these wrappers for future CLI/product tasks.

### Focus Area E — Final validation and implementation summary

#### Goal

Prove that the product CLI works through both direct `dotnet run` invocation and canonical engineering wrappers.

#### Required validation

Run:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
```

The implementation is incomplete unless these commands exit with code `0`, or the implementation summary reports the exact failing command and concise failure reason.

#### Validation tier

Tier 2 — Standard local gate plus product CLI smoke.

Do not require Tier 3 PR validation, Tier 4 release validation, or Tier 5 human review.

#### Direct documentation impact

If any required command cannot be implemented exactly as specified, update the directly affected command/spec/artifact document in the same implementation.

#### Deferred documentation impact

A later documentation synchronization pass may normalize milestone status, index docs, README examples, and agent routing.

## Validation expectations

### Required final validation

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
```

### Validation tiers

| Validation | Tier | Required for completion |
|---|---:|---:|
| `./eng/check.sh` | Tier 2 — Standard local gate | Yes |
| `./eng/cli-smoke.sh` | Tier 1/2 focused product CLI smoke | Yes |
| `./eng/product-validate.sh` | Tier 2 product validation gate for current maturity | Yes |
| Release validation | Tier 4 | No |
| Human review | Tier 5 | No |

### What must not be required

Do not require:

```text
release validation
package smoke tests
benchmarks
full scenario validation
packaged runtime validation
E2E tests
public documentation validation
human review gates
```

Those validation classes are outside this milestone.

## Direct documentation impact

The implementation agent must update documentation directly only where repository behavior changes immediately.

Required direct documentation updates:

```text
docs/engineering/product-cli.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/future-dotnet-solution.md
docs/specs/product-cli-contract.md
docs/artifacts/product-cli-result-contract.md
```

Update `README.md` and `AGENTS.md` only if their existing instructions would become misleading after the CLI and wrappers exist.

Do not perform broad documentation synchronization.

## Deferred documentation synchronization hints

This milestone intentionally defers broad documentation cleanup.

Potential follow-up documentation synchronization areas:

```text
docs/MILESTONES.md milestone status/index updates
docs/SPECS.md index update for product CLI contract
docs/ARTIFACTS.md index update for product CLI result contract
docs/DECISIONS.md index update for ADR-0008
README.md contributor quickstart/product CLI examples
AGENTS.md routing refinements for future CLI/product tasks
docs/engineering/* examples and consistency cleanup
```

These are not required to complete this milestone unless the implementation itself makes an existing authoritative statement false.

## Completion criteria

The milestone is complete when all of the following are true:

- `src/Agentic2D.Tools` exists and is included in the solution.
- The product CLI can be invoked through `dotnet run --project src/Agentic2D.Tools -- <args>`.
- `agentic2d --help` and `agentic2d --version` behavior exists through the development invocation path.
- `agentic2d runtime smoke --output <directory>` runs the minimal deterministic runtime and writes `<directory>/result.json`.
- `agentic2d validate --output <directory>` runs real product validation for the current maturity and writes `<directory>/result.json`.
- `eng/cli-smoke.sh` exists, is executable, and validates the product CLI smoke path.
- `eng/product-validate.sh` exists, is executable, and validates the product CLI validation path.
- `./eng/check.sh`, `./eng/cli-smoke.sh`, and `./eng/product-validate.sh` exit with code `0`, or the implementation summary reports the exact failing command and concise failure reason.
- CLI result artifacts conform to `docs/artifacts/product-cli-result-contract.md`.
- The CLI does not add renderer, asset pipeline, full scenario runner, packaged runtime, or release packaging behavior.
- No non-root `README.md`, TBP, or issue-template dependency has been introduced.

## Implementation summary requirements

The implementation agent's final response must include:

```text
Files created/modified
Commands implemented
Engineering wrappers created
Artifact paths produced
Validation commands executed
Validation result
Any deviations from this milestone and why
Deferred documentation synchronization notes
```
