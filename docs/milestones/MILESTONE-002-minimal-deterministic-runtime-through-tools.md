# Milestone 002 — Minimal Deterministic Runtime Through `Agentic2D.Tools`

## Goal

Create the first product-runtime vertical slice for `dotnet-ai-first-2d-game-engine`: a minimal deterministic runtime that can be exercised only through the `Agentic2D.Tools` product CLI.

This milestone turns the repository from “engineering substrate only” into “implementation-ready for the smallest engine behavior.”

The milestone produces:

- minimal runtime contracts for deterministic ticks, commands, events, queries, diagnostics, and result artifacts;
- a small runtime implementation in `Agentic2D.Engine`;
- a product CLI project named `Agentic2D.Tools`;
- one meaningful CLI command that runs the deterministic runtime and writes a result artifact;
- one smoke runtime scenario reachable through the CLI;
- unit tests and CLI smoke validation proving the slice works.

## Repository maturity and task mode

Repository maturity after this milestone:

```text
Implementation-ready for the minimal deterministic runtime.
Design-ready for richer runtime, scenario runner, asset pipeline, packaged runtime, source generation, and renderer work.
```

Task mode for this milestone:

```text
Implementation
```

This is not a documentation synchronization task, release-readiness task, public documentation task, asset-pipeline task, renderer task, or packaged-runtime task.

## Required authority

A later implementation agent must read only the following authority before implementing this milestone:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/specs/runtime-principles.md
docs/specs/agentic-workflow.md
docs/specs/minimal-deterministic-runtime.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/SCENARIOS.md
docs/scenarios/minimal-runtime-scenarios.md
docs/ARTIFACTS.md
docs/artifacts/runtime-result-contract.md
docs/decisions/ADR-0007-expose-minimal-runtime-through-tools-cli.md
```

Do not require the implementation agent to read all documents under `docs/`.

Do not treat `docs/research/` as operational authority. Research documents may explain background, but any rule required for implementation must be present in the active authority documents listed above or in this milestone.

## Scope

Create or modify only the files needed to implement the minimal deterministic runtime and expose it through `Agentic2D.Tools`.

### Runtime contracts and engine implementation

Modify the existing projects:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
```

Implement the minimal contracts defined by `docs/specs/minimal-deterministic-runtime.md`.

The implementation must support:

```text
fixed tick execution
typed entity and tick concepts
command acceptance or rejection
event recording
queryable final state
structured diagnostics
runtime result object
stable JSON serialization for CLI artifacts
```

### Product CLI project

Create:

```text
src/Agentic2D.Tools/Agentic2D.Tools.csproj
```

Add it to:

```text
dotnet-ai-first-2d-game-engine.slnx
```

Required project references:

```text
Agentic2D.Tools -> Agentic2D.Contracts
Agentic2D.Tools -> Agentic2D.Engine
```

The CLI is the product/runtime API for this milestone. Do not expose runtime behavior through ad-hoc scripts that bypass `Agentic2D.Tools`.

### Test project updates

Modify the existing unit test project:

```text
tests/unit/Agentic2D.Tests.Unit
```

Add tests for:

```text
runtime determinism
command/event behavior
queryable final state
CLI argument/result behavior where practical without shelling out
runtime result serialization shape
```

If a separate CLI smoke test project is required for practical subprocess execution, create it only if it remains fast and is run by the existing standard local gate. Do not add a slow or E2E-style test category in this milestone.

### Engineering command usage

Use the existing base commands:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
./eng/check.sh
```

Do not add new `eng/` scripts in this milestone unless the current command contract explicitly requires a small wrapper for the product CLI smoke command and the wrapper performs meaningful validation.

Preferred approach:

- validate the CLI through unit-level command tests and direct `dotnet run` command examples during implementation;
- defer permanent `eng/cli-smoke.sh` until product CLI command coverage grows beyond one smoke command.

If the implementation adds `eng/cli-smoke.sh`, it must be documented directly in `docs/engineering/command-contract.md` and must be included in validation expectations.

## Non-goals

Do not implement any of the following in this milestone:

```text
renderer
raylib-cs integration
MonoGame integration
SDL/Silk.NET integration
asset pipeline
asset metadata schemas
map authoring
animation authoring
shader/material workflow
content validation command
full scenario runner project
Agentic2D.ScenarioRunner project
Agentic2D.Runtime project
Agentic2D.Runtime.Debug project
Agentic2D.Runtime.Packaged project
Agentic2D.SourceGen project
source generators
package/release validation
GitHub Actions workflows
NuGet packaging
public documentation
samples
TBPs
issue templates
F# behavior modules
behavior analyzers
benchmarks
visual artifacts
human review gates
packaged runtime validation
```

Do not create non-root `README.md` files.

Do not add runtime/game package dependencies such as raylib-cs, MonoGame, Aether.Physics2D, Box2D.NET, Silk.NET, SDL bindings, image-processing libraries, JSON schema libraries, or CLI framework libraries unless a compile-time blocker proves the base class library cannot satisfy the milestone. Prefer a minimal hand-written CLI parser for this first command.

## Required implementation conventions

### Product CLI name and command shape

Create a .NET console project whose assembly/root namespace is:

```text
Agentic2D.Tools
```

The product command for this milestone is:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --output artifacts/runtime-smoke
```

The command must also support an explicit tick count and optional output path:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

For this milestone, `runtime smoke` is the only required product command.

Do not implement broader command groups such as `asset`, `map`, `shader`, `package`, or `content`.

### CLI behavior

The `runtime smoke` command must:

1. create the minimal deterministic runtime;
2. create a minimal test entity or equivalent stable state object;
3. submit one or more deterministic commands;
4. advance the runtime for the requested tick count;
5. emit deterministic events;
6. query final state;
7. write a `result.json` artifact under the output directory;
8. return exit code `0` when the runtime result status is `passed`;
9. return non-zero exit code when the runtime result status is `failed` or `error`.

The command must not require a graphical environment, external services, network access, wall-clock-dependent behavior, real assets, or human review.

### Determinism rule

For identical CLI arguments and identical source revision, the meaningful contents of `result.json` must be stable across repeated runs.

Allowed to vary:

```text
startedAt
completedAt
duration fields if implemented
absolute local paths if included only in diagnostics
```

Recommended: avoid timestamps entirely in the first implementation unless the artifact contract requires them. If timestamps are included, tests must not depend on exact timestamp values.

### Minimal runtime behavior

The runtime may be intentionally small, but it must be real behavior, not dead scaffolding.

A sufficient smoke behavior is:

```text
initial state:
  entity `entity.player` exists at logical position 0

command:
  move entity.player by +1 on tick 1

execution:
  run 3 fixed ticks

events:
  runtime.started
  entity.created
  command.accepted
  entity.moved
  runtime.completed

final query:
  entity.player position == 1
  current tick == 3
```

The exact model may differ if it conforms to `docs/specs/minimal-deterministic-runtime.md` and `docs/scenarios/minimal-runtime-scenarios.md`.

### IDs and JSON

Use stable string representations in the first debug-oriented artifact, for example:

```text
entity.player
runtime.started
command.accepted
entity.moved
```

Do not introduce integer release IDs or generated ID registries in this milestone.

### Error handling

The CLI must produce useful diagnostics for:

```text
unknown command
invalid --ticks value
missing --output value
runtime assertion failure
artifact write failure
```

Diagnostics may be written to stderr and to `result.json` when the runtime has enough context to write the artifact.

### Serialization

Use `System.Text.Json` from the base class library.

Do not add external serialization libraries.

Use stable, explicit JSON property names. Avoid relying on reflection-heavy or hidden behavior beyond ordinary `System.Text.Json` serialization.

## Focus areas

### Focus Area A — Minimal runtime contract model

#### Goal

Create the shared contracts needed for a deterministic runtime result slice.

#### Scope

Modify:

```text
src/Agentic2D.Contracts
```

Implement only the contract types needed by this milestone.

Expected contract concepts:

```text
Tick
EntityId
CommandId or command type identifier
EventId or event type identifier
Diagnostic record
RuntimeResult
RuntimeStatus
RuntimeEvent
RuntimeCommandResult
```

The exact names may vary, but the implementation must preserve the semantics defined in `docs/specs/minimal-deterministic-runtime.md` and `docs/artifacts/runtime-result-contract.md`.

#### Validation tier

Tier 1 — Focused implementation.

#### Required validation

Run a focused build/test command that covers `Agentic2D.Contracts`, then run the full Tier 2 gate at the end of the milestone.

If no focused command exists, use:

```bash
./eng/build.sh
./eng/test.sh
```

#### Direct documentation impact

Update a directly relevant spec only if implementation discovers a contract impossibility in the milestone package.

Do not broaden terminology or index docs as part of this focus area.

#### Deferred documentation impact

A later documentation synchronization pass may update `docs/TERMINOLOGY.md` if the final contract names introduce durable vocabulary not already covered.

### Focus Area B — Minimal deterministic engine behavior

#### Goal

Implement enough engine behavior to make the runtime smoke command meaningful.

#### Scope

Modify:

```text
src/Agentic2D.Engine
```

Implement:

```text
runtime initialization
fixed tick advance
command handling
event recording
final state query
runtime result creation
```

The implementation may use simple in-memory collections. Do not introduce an ECS framework, source-generated dispatch, reflection routing, dependency injection pipeline, or async command handlers.

#### Validation tier

Tier 1 — Focused implementation.

#### Required validation

Run:

```bash
./eng/test.sh
```

Tests must prove deterministic behavior for at least two identical runs.

#### Direct documentation impact

Update `docs/specs/minimal-deterministic-runtime.md` only if the implementation changes the directly specified runtime semantics.

#### Deferred documentation impact

A later documentation synchronization pass may decide whether the proven runtime shape should be reflected in broader architecture docs.

### Focus Area C — Product CLI through `Agentic2D.Tools`

#### Goal

Expose the minimal runtime only through the product CLI project for this milestone.

#### Scope

Create:

```text
src/Agentic2D.Tools/Agentic2D.Tools.csproj
```

Add it to the solution.

Implement:

```text
runtime smoke
--ticks <positive integer>
--output <directory>
exit codes
stderr diagnostics
result.json writing
```

#### Validation tier

Tier 1 — Focused implementation for CLI behavior.

Tier 2 — Standard local gate at milestone completion.

#### Required validation

Run the CLI manually or through tests:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

Verify that:

```text
artifacts/runtime-smoke/result.json exists
result status is passed
final tick is 3
expected events are present
exit code is 0
```

Then run:

```bash
./eng/check.sh
```

#### Direct documentation impact

Update:

```text
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
```

only if the actual implemented command or project layout differs from the milestone package.

If `eng/cli-smoke.sh` is added, update:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

#### Deferred documentation impact

A later documentation synchronization pass may update root `README.md`, `AGENTS.md`, and engineering indexes with CLI examples.

### Focus Area D — Runtime result artifact

#### Goal

Produce a machine-readable runtime result artifact that agents and tests can inspect.

#### Scope

Implement `result.json` as defined by:

```text
docs/artifacts/runtime-result-contract.md
```

The artifact must include enough information to diagnose a failed smoke run without reading console output alone.

#### Validation tier

Tier 1 — Focused implementation.

#### Required validation

Add tests or assertions proving that `result.json` contains:

```text
schemaVersion
command
status
ticksRequested
finalTick
events
diagnostics
assertions
```

For a passing smoke run, diagnostics may be empty.

#### Direct documentation impact

Update `docs/artifacts/runtime-result-contract.md` only if the implementation requires a contract correction.

#### Deferred documentation impact

A later documentation synchronization pass may update `docs/ARTIFACTS.md` to list the new concrete runtime result contract.

### Focus Area E — Final validation and implementation report

#### Goal

Prove the full milestone works through the canonical engineering gate and summarize implementation results.

#### Required validation

Run:

```bash
./eng/check.sh
```

Also run or report the CLI smoke command:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

The milestone is incomplete unless `./eng/check.sh` exits with code `0` or the implementation summary reports the exact failing command and concise failure reason.

#### Validation tier

Tier 2 — Standard local gate.

CLI smoke is Tier 1 focused product validation unless the implementation adds a permanent `eng/cli-smoke.sh`, in which case document the tier mapping directly.

#### Direct documentation impact

Update direct docs only when the implemented behavior differs from this milestone package.

#### Deferred documentation impact

A later documentation synchronization pass may update milestone indexes, decision indexes, root quickstart text, agent routing, and command examples.

## Validation expectations

### Required final validation

Run:

```bash
./eng/check.sh
```

This is Tier 2 — Standard local gate.

### Required product smoke validation

Run:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

This is Tier 1 — Focused product validation for the first product CLI command.

### What must not be required

Do not require:

```text
release validation
package smoke tests
benchmarks
visual regression
human review gates
packaged runtime validation
E2E tests
public documentation validation
asset import validation
scenario soak validation
```

Those validation classes are outside this milestone.

## Direct documentation impact

The implementation agent must update documentation directly only where repository behavior changes immediately.

Required direct documentation updates if the implementation follows this package exactly:

```text
docs/engineering/future-dotnet-solution.md
```

because `Agentic2D.Tools` changes from candidate future project to current project.

Required direct documentation updates if implementation details differ from the package:

```text
docs/specs/minimal-deterministic-runtime.md
docs/engineering/product-cli.md
docs/scenarios/minimal-runtime-scenarios.md
docs/artifacts/runtime-result-contract.md
```

Update these only to keep active authority accurate; do not broaden them.

Required direct documentation updates if new engineering scripts are added:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

Do not perform a broad documentation synchronization pass.

## Completion criteria

The milestone is complete when all of the following are true:

- `src/Agentic2D.Tools/Agentic2D.Tools.csproj` exists and is included in the solution.
- `Agentic2D.Tools` references `Agentic2D.Contracts` and `Agentic2D.Engine`.
- The minimal deterministic runtime behavior exists in `Agentic2D.Engine`.
- Runtime contracts required by the milestone exist in `Agentic2D.Contracts`.
- `runtime smoke` is executable through `Agentic2D.Tools`.
- `runtime smoke --ticks 3 --output artifacts/runtime-smoke` writes `artifacts/runtime-smoke/result.json`.
- The result artifact conforms to `docs/artifacts/runtime-result-contract.md`.
- Repeated identical smoke runs produce the same meaningful result values, ignoring explicitly allowed volatile fields.
- Unit tests cover deterministic runtime behavior and result artifact shape.
- `./eng/check.sh` exits with code `0`, or the implementation summary reports the exact failing command and concise failure reason.
- No runtime graphics package dependency has been added.
- No asset pipeline, renderer, source generator, packaged runtime, public docs, TBP, issue template, or non-root `README.md` has been introduced.

## Implementation summary requirements

The implementation agent’s final response must include:

```text
Files created/modified
Projects created/modified
Product CLI command implemented
Runtime behavior implemented
Artifact path produced
Validation commands executed
Validation results
Any deviations from this milestone and why
Deferred documentation synchronization notes
```
