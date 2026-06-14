# Milestone 005 — Scenario Runner and Runtime Evidence Foundation

## Goal

Introduce the first generic scenario runner foundation for the engine and expose it through the product CLI.

The milestone turns the existing minimal runtime smoke behavior into an authored scenario execution path:

```text
authored scenario JSON
→ scenario runner
→ deterministic runtime execution
→ assertion evaluation
→ result.json, events.jsonl, diagnostics.json
→ product CLI exit code
```

The milestone must produce real engine evidence. It must not create a placeholder scenario runner that only validates file existence or returns success without executing runtime behavior.

## Repository role and maturity assumptions

Repository role:

```text
capability-provider
```

This repository builds the engine/runtime/tooling capability. Milestone 005 validates the capability implementation. It does not use the engine to build or validate a separate consumer game.

Current maturity assumption:

```text
implementation-ready for base engineering, minimal runtime, and first product CLI
artifact-first
```

The milestone assumes Milestones 001–003 are implemented:

- canonical `eng/` scripts exist;
- `src/Agentic2D.Contracts`, `src/Agentic2D.Engine`, and `src/Agentic2D.Tools` exist;
- the minimal deterministic runtime exists;
- the product CLI can run `runtime smoke` and `validate`;
- `./eng/check.sh`, `./eng/cli-smoke.sh`, and `./eng/product-validate.sh` exist.

## Execution mode

```text
ai-executed-human-reviewed
```

Scope size:

```text
medium-to-large coherent vertical slice
```

Implementation autonomy:

The implementation agent may modify multiple components when the changes stay within this milestone's contracts and validation expectations.

Expected touched areas include runtime-adjacent code, a scenario runner project or module, product CLI command routing, engineering wrappers, unit tests, authored scenario data, and directly affected project documentation.

The implementation agent must not expand the milestone into asset workflows, map workflows, full content validation, packaged runtime validation, release work, or guide-system migration.

## Scope

Implement a scenario runner foundation with one authored runtime smoke scenario.

Required product capability:

```text
agentic2d scenario run <scenario-id-or-path> --output <directory>
```

Required development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- scenario run game/scenarios/smoke/runtime-smoke.json --output artifacts/scenarios/runtime-smoke
```

Required engineering validation wrapper:

```bash
./eng/scenario-smoke.sh
```

Optional engineering wrapper when simple and meaningful:

```bash
./eng/scenario.sh <scenario-id-or-path>
```

The scenario runner must load scenario source from JSON, validate the required shape, execute the deterministic runtime, evaluate assertions, and write structured artifacts.

## Non-goals

Do not implement any of the following in this milestone:

```text
asset curation workbench
asset import pipeline
map validation scenarios
animation validation scenarios
shader/material preview scenarios
UI scenarios
save/load scenarios
performance or soak scenarios
packaged-runtime validation
visual review packs
full scenario discovery across arbitrary folders
full JSON Schema registry
source generators
renderer integration
raylib-cs integration
MonoGame integration
public documentation
release readiness
NuGet packaging
GitHub Actions workflows
TBPs
issue templates
guide-system migration
```

Do not create non-root `README.md` files.

Do not copy external guide documents or prompt templates into this repository.

Do not require ordinary implementation agents to read `.guide-profile.json`, `.guide-sync/`, copied guide documents, or the external guide repository.

## Required authority documents

A later implementation agent must read only the following authority before implementing this milestone:

```text
README.md
AGENTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/specs/runtime-principles.md
docs/specs/minimal-deterministic-runtime.md
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/SCENARIOS.md
docs/scenarios/minimal-runtime-scenarios.md
docs/scenarios/scenario-runner-foundation.md
docs/ARTIFACTS.md
docs/artifacts/runtime-result-contract.md
docs/artifacts/product-cli-result-contract.md
docs/artifacts/scenario-runner-artifact-contract.md
docs/decisions/ADR-0010-scenario-runner-before-asset-workbench.md
```

Do not require the implementation agent to read all files under `docs/`.

Treat `docs/research/` as non-authoritative legacy traceability material.

## Files or areas likely affected

Likely implementation areas:

```text
dotnet-ai-first-2d-game-engine.slnx
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Tools
src/Agentic2D.ScenarioRunner
tests/unit/Agentic2D.Tests.Unit
game/scenarios/smoke/runtime-smoke.json
eng/scenario-smoke.sh
eng/scenario.sh
```

`src/Agentic2D.ScenarioRunner` is the preferred new project if the implementation creates reusable scenario runner behavior. It should reference the existing contracts and engine projects as needed.

If the implementation agent can keep the scenario runner cohesive without a new project, it may place the first implementation in existing projects, but it must explain that choice in the implementation summary. Do not create a new project merely as empty scaffolding.

Likely direct documentation updates during implementation:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
```

Documentation index updates may be deferred to `.guide-sync/pending/` unless an existing active document becomes misleading after implementation.

## Focus areas

### Focus Area A — Scenario source contract and authored smoke scenario

#### Goal

Create the first authored scenario source file for deterministic runtime smoke validation.

#### Scope

Create:

```text
game/scenarios/smoke/runtime-smoke.json
```

The scenario must conform to:

```text
docs/specs/scenario-runner-contract.md
docs/scenarios/scenario-runner-foundation.md
```

Required scenario identity:

```text
id: runtime.smoke
category: smoke
schema: agentic2d.scenario.v1
```

Required semantics:

```text
initial entity.player position: 0
requested ticks: 3
move entity.player by +1 exactly once
final tick: 3
final entity.player position: 1
required events present in deterministic order
required assertions pass
```

#### Implementation constraints

Use JSON for the authored scenario file.

Do not introduce YAML, TOML, custom DSL syntax, a database, a schema registry, or a broad content model in this milestone.

#### Validation tier

Tier 1 — Focused implementation validation.

#### Required validation

Add focused unit tests proving:

- the scenario file can be loaded;
- required fields are validated;
- malformed scenario input produces structured diagnostics;
- scenario ID and scenario file path behavior are deterministic.

### Focus Area B — Scenario runner execution foundation

#### Goal

Implement the scenario runner behavior that turns authored scenario source into deterministic runtime execution and assertions.

#### Scope

Implement a small scenario runner that can:

```text
load scenario JSON
validate required fields
translate initial state into minimal runtime state
execute supported steps
collect events
evaluate assertions
produce a scenario result model
```

Required supported command type:

```text
move
```

Required assertion types:

```text
finalTickEqualsRequested
entityExists
entityPositionEquals
eventOccurred
```

#### Implementation constraints

Use `System.Text.Json` at the scenario/artifact boundary.

Do not parse JSON inside runtime hot paths.

Do not introduce reflection-based runtime dispatch, source generators, DI-heavy pipelines, network access, wall-clock-driven behavior, or asynchronous command dispatch inside the simulation tick.

The runner may use debug-oriented string IDs because this milestone remains a debug/runtime evidence foundation, not packaged-runtime work.

#### Validation tier

Tier 1 — Focused implementation validation.

#### Required validation

Add tests proving:

- identical scenario runs produce semantically equivalent results;
- final tick equals requested tick count;
- the player moves exactly once from `0` to `1`;
- required events are emitted in deterministic order;
- required assertions pass for the valid smoke scenario;
- invalid scenario input fails with stable diagnostic IDs.

### Focus Area C — Scenario artifacts

#### Goal

Write useful, deterministic evidence for scenario execution.

#### Scope

Scenario runs must write:

```text
<output>/result.json
<output>/events.jsonl
<output>/diagnostics.json
```

The artifacts must conform to:

```text
docs/artifacts/scenario-runner-artifact-contract.md
```

Required smoke output:

```text
artifacts/scenarios/runtime-smoke/result.json
artifacts/scenarios/runtime-smoke/events.jsonl
artifacts/scenarios/runtime-smoke/diagnostics.json
```

#### Implementation constraints

Generated artifacts must remain generated outputs. Do not treat generated run artifacts as hand-authored source truth.

Avoid volatile fields. If volatile fields are included, they must be excluded from deterministic tests and explicitly documented in the implementation summary.

Diagnostics must use stable IDs.

#### Validation tier

Tier 1 — Focused artifact validation.

#### Required validation

Add tests or validation code proving:

- required files are written;
- `result.json` is valid JSON and contains required fields;
- `events.jsonl` contains one valid JSON object per line;
- `diagnostics.json` is valid JSON and contains a diagnostics array;
- artifact references in `result.json` are relative to the output directory;
- artifact semantic fields are deterministic across repeated runs.

### Focus Area D — Product CLI command surface

#### Goal

Expose scenario execution through the product CLI.

#### Scope

Add support for:

```text
agentic2d scenario run <scenario-id-or-path> --output <directory>
```

Required development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- scenario run game/scenarios/smoke/runtime-smoke.json --output artifacts/scenarios/runtime-smoke
```

Preferred ID lookup invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- scenario run runtime.smoke --output artifacts/scenarios/runtime-smoke
```

If ID lookup cannot be implemented cleanly in this milestone, file-path execution is required and the implementation summary must explicitly defer broader ID lookup.

#### Exit codes

Use the product CLI exit-code contract:

```text
0: scenario completed and all assertions passed
1: scenario completed but one or more assertions failed
2: invalid CLI usage or invalid scenario input
3: runtime execution error, artifact writing failure, or unhandled command failure
```

#### Implementation constraints

The product CLI must call scenario runner behavior. It must not duplicate scenario execution logic inside argument parsing code.

Do not change existing `runtime smoke` or `validate` behavior except where needed to keep command routing consistent.

#### Validation tier

Tier 1 — Focused product CLI validation.

Tier 2 — Product validation gate when invoked by engineering wrappers.

#### Required validation

Add tests or command validation proving:

- `scenario run` succeeds for the valid smoke scenario;
- missing `--output` returns exit code `2`;
- unknown scenario path or ID returns exit code `2` with structured diagnostics;
- invalid scenario file returns exit code `2` with structured diagnostics;
- runtime/artifact errors return exit code `3` where they can be induced safely.

### Focus Area E — Engineering wrappers and command contracts

#### Goal

Make scenario validation available through canonical repository engineering commands.

#### Scope

Create:

```text
./eng/scenario-smoke.sh
```

Expected behavior:

```text
runs the authored runtime.smoke scenario through src/Agentic2D.Tools
writes artifacts under artifacts/scenarios/runtime-smoke
fails when the CLI exits non-zero
fails when required artifacts are missing
```

Optional when implemented meaningfully:

```text
./eng/scenario.sh <scenario-id-or-path>
```

If `./eng/scenario.sh` is created, it must validate actual scenario behavior and must not be a success-only placeholder.

#### Implementation constraints

Scripts must be executable.

Scripts must use the repository's existing script conventions.

Do not add CI workflows in this milestone.

#### Validation tier

Tier 2 — Standard local/product/scenario gate.

#### Required validation

Run:

```bash
./eng/scenario-smoke.sh
```

The command must exit `0` and produce required scenario artifacts.

### Focus Area F — Direct documentation updates and implementation summary

#### Goal

Keep directly affected project-truth docs accurate without performing broad documentation synchronization.

#### Scope

Update active documentation that becomes false because of implementation.

Required direct updates after implementation:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
```

Update these only as needed:

```text
docs/SPECS.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/DECISIONS.md
docs/MILESTONES.md
```

The implementation may defer index normalization and broader cross-link cleanup to `.guide-sync/pending/` unless an index becomes actively misleading.

#### Implementation constraints

Do not perform broad unrelated documentation cleanup.

Do not add public docs, release docs, TBPs, issue templates, workflow docs, or copied guide material.

#### Validation tier

Tier 1 — Documentation sanity for directly changed docs.

#### Required validation

Confirm by inspection that:

- implemented scenario commands are no longer documented only as future commands;
- new current project layout is accurate if `Agentic2D.ScenarioRunner` is added;
- product CLI docs describe the new command accurately;
- command-contract docs describe any new `eng/` wrapper commands accurately.

## Required final validation

Run, in order:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
```

Required final validation tier:

```text
Tier 2 — standard local gate plus product/scenario validation for current maturity
```

Do not require release validation, package smoke tests, packaged-runtime validation, performance benchmarks, CI workflows, public documentation validation, or human visual review packs.

If a command cannot run because of an environmental limitation, the implementation summary must include:

```text
exact command
exit code if available
concise failure reason
whether the failure is environmental or repository-caused
```

## Acceptance criteria

The milestone is complete when all of the following are true:

- An authored scenario exists at `game/scenarios/smoke/runtime-smoke.json`.
- The authored scenario conforms to the Milestone 005 scenario runner contract.
- `agentic2d scenario run <scenario-id-or-path> --output <directory>` is implemented through `src/Agentic2D.Tools`.
- File-path execution of the smoke scenario works.
- `runtime.smoke` ID execution works, or the implementation summary explicitly defers broader ID lookup while file-path execution works.
- The scenario runner executes real minimal runtime behavior.
- The player moves exactly once from position `0` to `1`.
- Final tick equals requested ticks.
- Required events are emitted in deterministic order.
- Required assertions are evaluated and represented in `result.json`.
- Scenario runs write `result.json`, `events.jsonl`, and `diagnostics.json`.
- Generated scenario artifacts conform to `docs/artifacts/scenario-runner-artifact-contract.md`.
- Invalid scenario input produces structured diagnostics and exit code `2`.
- Runtime or artifact execution errors produce exit code `3` when safely testable.
- `./eng/scenario-smoke.sh` exists, is executable, and validates meaningful scenario state.
- Required final validation commands pass, or failures are reported precisely.
- Directly affected engineering and product CLI docs are updated.
- No public docs, release docs, TBPs, issue templates, workflows, copied guide docs, or copied prompt templates are introduced.

## Direct documentation impact

The implementation agent must update documentation directly only where repository behavior changes immediately.

Required direct documentation updates:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
```

These updates are required because Milestone 005 adds new current commands and may add a new current project.

Potential direct updates when kept narrow:

```text
docs/SPECS.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/DECISIONS.md
docs/MILESTONES.md
```

These may be updated during implementation if the change is small and directly indexes the new Milestone 005 authority. Otherwise, defer to `.guide-sync/pending/`.

## Deferred documentation synchronization hints

This milestone package includes pending sync hints under:

```text
.guide-sync/pending/
```

Ordinary implementation agents must ignore `.guide-sync/` unless explicitly assigned documentation synchronization work.

Deferred sync topics include:

- indexing Milestone 005 and ADR-0010;
- indexing the new scenario runner spec, scenario document, and artifact contract;
- cleaning up future-command lists after implementation;
- recording any human-review follow-up about scenario evidence quality.

Do not perform broad documentation synchronization as part of ordinary implementation.

## Human review requirements

Automation must determine whether the scenario passes.

Human review is required for milestone acceptance to judge evidence quality and future usefulness.

The human reviewer should inspect the implementation summary and generated scenario artifacts, especially:

```text
artifacts/scenarios/runtime-smoke/result.json
artifacts/scenarios/runtime-smoke/events.jsonl
artifacts/scenarios/runtime-smoke/diagnostics.json
```

Review questions:

- Can a failed scenario be diagnosed from artifacts without guessing?
- Are scenario IDs, event IDs, assertion IDs, and diagnostic IDs stable and meaningful?
- Does `events.jsonl` provide useful event ordering evidence?
- Does `diagnostics.json` help agents understand invalid scenario input?
- Is the scenario source format minimal enough, or did implementation over-design a content system?
- Is the product CLI shape suitable for future scenario categories?

Human review is not required for visual quality, gameplay feel, asset semantics, UX, packaging, or public documentation in this milestone.

## Out-of-scope guide migration work

Guide-system migration is out of scope for this milestone.

Do not modify `.guide-profile.json` unless a directly necessary correction is discovered.

Do not require implementation agents to read `.guide-profile.json`, `.guide-sync/`, the external guide repository, copied guide documents, or prompt templates.

Do not copy guide documents or prompt templates into this repository.

## Implementation summary requirements

The implementation agent's final response must include:

```text
Files created/modified
Projects created or project-layout decisions
Scenario source path
Product CLI commands added
Engineering wrappers added
Artifact files produced during validation
Validation commands executed
Validation results
Any deviations from the milestone and why
Human review notes about evidence quality
Deferred documentation synchronization notes
```
