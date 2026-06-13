# Milestone 002 — Minimal Deterministic Runtime

## Status

Implemented; synchronized after Milestones 002 and 003.

This status reflects the repository documentation state. This documentation synchronization pass did not re-run validation.

## Goal

Create the first minimal deterministic engine core and expose a runtime smoke execution path that can produce a structured result artifact.

The milestone proves this first executable engine slice:

```text
command input
→ fixed-tick runtime execution
→ factual events
→ queryable final state
→ machine-readable result artifact
```

## Repository maturity and task mode

Repository maturity after this milestone:

```text
Implementation-ready for minimal runtime smoke behavior.
Design-ready for broader scenario, asset, content, packaged-runtime, and public documentation work.
```

Task mode:

```text
Implementation
```

This was not a release-readiness, public documentation, asset-pipeline, renderer, packaged-runtime, or documentation synchronization milestone.

## Required authority

Implementation authority for this milestone:

```text
README.md
AGENTS.md
docs/SPECS.md
docs/specs/runtime-principles.md
docs/specs/agentic-workflow.md
docs/specs/minimal-deterministic-runtime.md
docs/SCENARIOS.md
docs/scenarios/minimal-runtime-scenarios.md
docs/ARTIFACTS.md
docs/artifacts/runtime-result-contract.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/future-dotnet-solution.md
docs/decisions/ADR-0007-expose-minimal-runtime-through-tools-cli.md
```

Do not require broad reading across `docs/`.

Do not treat `docs/research/` as operational authority.

## Scope

Milestone 002 introduced:

- minimal deterministic runtime behavior;
- fixed tick execution;
- stable debug-oriented string identifiers;
- command acceptance and factual event emission;
- queryable final state for the smoke path;
- `runtime.smoke` scenario semantics;
- `result.json` artifact output;
- exposure through `Agentic2D.Tools` for the runtime smoke command.

The current runtime smoke path is documented by:

```text
docs/specs/minimal-deterministic-runtime.md
docs/scenarios/minimal-runtime-scenarios.md
docs/artifacts/runtime-result-contract.md
```

## Non-goals

This milestone did not implement:

```text
renderer integration
raylib-cs integration
MonoGame integration
SDL/Silk.NET integration
asset pipeline
map authoring
animation workflow
full scenario runner
Agentic2D.ScenarioRunner
source generators
packaged runtime mode
binary resources
release validation
public documentation
human review gates
TBPs
issue templates
```

Do not create non-root `README.md` files.

## Focus areas

### Focus Area A — Minimal runtime state and tick execution

#### Goal

Create the smallest deterministic runtime that can execute a fixed number of ticks.

#### Scope

The runtime starts at tick `0`, runs for a positive tick count, and ends with final tick equal to the requested tick count.

#### Validation tier

Tier 1 — Focused validation.

#### Documentation authority

`docs/specs/minimal-deterministic-runtime.md`

### Focus Area B — Command, event, and query behavior

#### Goal

Prove the command/event/query principle through one smoke path.

#### Scope

The smoke behavior moves `entity.player` by `+1`, emits required events, and exposes final-state query data.

#### Validation tier

Tier 1 — Focused validation.

#### Documentation authority

`docs/specs/minimal-deterministic-runtime.md`

### Focus Area C — Runtime smoke scenario and result artifact

#### Goal

Produce structured evidence for the smoke path.

#### Scope

Produce:

```text
<output>/result.json
```

according to `docs/artifacts/runtime-result-contract.md`.

#### Validation tier

Tier 1 — Focused validation.

#### Documentation authority

```text
docs/scenarios/minimal-runtime-scenarios.md
docs/artifacts/runtime-result-contract.md
```

### Focus Area D — Runtime smoke exposure through tools

#### Goal

Expose the runtime smoke execution through `src/Agentic2D.Tools` without introducing a broad product CLI surface yet.

#### Scope

Use the development invocation form:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

#### Validation tier

Tier 1 focused validation, followed by Tier 2 standard local validation when integrated with the repository gate.

## Validation expectations

Expected validation commands for the completed milestone:

```bash
./eng/check.sh
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

The runtime smoke command should exit with code `0` and write:

```text
artifacts/runtime-smoke/result.json
```

Do not require Tier 3 PR validation, Tier 4 release validation, or Tier 5 human review for this milestone.

## Direct documentation impact

Direct documentation introduced or affected by this milestone:

```text
docs/specs/minimal-deterministic-runtime.md
docs/scenarios/minimal-runtime-scenarios.md
docs/artifacts/runtime-result-contract.md
docs/decisions/ADR-0007-expose-minimal-runtime-through-tools-cli.md
docs/engineering/future-dotnet-solution.md
```

## Deferred documentation synchronization

Resolved by this synchronization pass:

```text
docs/SPECS.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/DECISIONS.md
docs/MILESTONES.md
README.md
AGENTS.md
```

Remaining deferred work is limited to future milestone selection and any implementation-summary-specific details not available to this pass.

## Completion criteria

The milestone is complete when:

- the minimal deterministic runtime smoke behavior exists;
- runtime events and final-state query data are deterministic for the smoke path;
- `runtime.smoke` scenario semantics are documented;
- the runtime smoke command writes `result.json` according to `docs/artifacts/runtime-result-contract.md`;
- validation succeeds through the required commands or the implementation summary reports exact failures;
- no renderer, asset pipeline, full scenario runner, packaged runtime, TBP, issue template, or non-root `README.md` dependency is introduced.
