# Scenario Runner Foundation Scenarios

## Authority

This document is authoritative for the scenario source and smoke validation scope introduced by Milestone 005.

It complements:

```text
docs/specs/scenario-runner-contract.md
docs/artifacts/scenario-runner-artifact-contract.md
```

## Purpose

Milestone 005 converts the existing built-in runtime smoke behavior into the first authored scenario runner path.

The repository must contain one authored scenario file that can be executed through:

```text
agentic2d scenario run <scenario-id-or-path> --output <directory>
```

## Required authored scenario

Create:

```text
game/scenarios/smoke/runtime-smoke.json
```

The scenario ID is:

```text
runtime.smoke
```

The scenario category is:

```text
smoke
```

## Scenario purpose

`runtime.smoke` proves that the scenario runner can:

```text
load authored scenario JSON
validate scenario shape
initialize deterministic runtime state
execute one deterministic move command
run fixed ticks
collect runtime events
evaluate assertions
write scenario artifacts
return a product CLI exit code
```

## Required source semantics

The authored scenario must describe this behavior:

```text
initial state:
  entity.player exists at position 0

runtime:
  requested ticks: 3

steps:
  move entity.player by +1 exactly once

expected final state:
  final tick == 3
  entity.player position == 1

minimum events:
  runtime.started
  entity.created
  command.accepted
  entity.moved
  runtime.completed
```

## Required assertions

The scenario must evaluate at least these assertions:

```text
assert.finalTick
assert.playerExists
assert.playerPosition
assert.runtimeStartedEvent
assert.entityCreatedEvent
assert.commandAcceptedEvent
assert.entityMovedEvent
assert.runtimeCompletedEvent
```

Exact assertion IDs may vary only if they remain stable and equivalent in meaning.

## Required artifact output

The standard smoke output directory is:

```text
artifacts/scenarios/runtime-smoke
```

The run must produce:

```text
artifacts/scenarios/runtime-smoke/result.json
artifacts/scenarios/runtime-smoke/events.jsonl
artifacts/scenarios/runtime-smoke/diagnostics.json
```

These generated files are evidence outputs. They are not hand-authored source truth unless a future milestone explicitly creates committed baselines.

## Required product CLI validation

The implementation must support:

```bash
dotnet run --project src/Agentic2D.Tools -- scenario run game/scenarios/smoke/runtime-smoke.json --output artifacts/scenarios/runtime-smoke
```

Expected result:

```text
exit code: 0
result status: passed
final tick: 3
entity.player position: 1
all required assertions pass
all required events are present in deterministic order
```

The implementation should also support scenario ID lookup for:

```bash
dotnet run --project src/Agentic2D.Tools -- scenario run runtime.smoke --output artifacts/scenarios/runtime-smoke
```

If the implementation cannot support general ID lookup cleanly in Milestone 005, it must at least support file-path execution and document the deferred ID-lookup work in the implementation summary.

## Required engineering validation

Milestone 005 should introduce:

```bash
./eng/scenario-smoke.sh
```

Expected behavior:

```text
runs the runtime.smoke authored scenario through Agentic2D.Tools
writes artifacts under artifacts/scenarios/runtime-smoke
fails when the CLI exits non-zero
fails when required artifacts are missing
```

The implementation may also introduce:

```bash
./eng/scenario.sh <scenario-id-or-path>
```

If introduced, it must validate meaningful state and must not be a success-only placeholder.

## Human review scope

Automation decides whether `runtime.smoke` passes.

Human review for Milestone 005 evaluates evidence quality:

- Is `result.json` understandable without reading source code?
- Is `events.jsonl` useful for diagnosing order-dependent behavior?
- Is `diagnostics.json` useful when input is invalid?
- Are scenario and assertion IDs stable enough for agentic work?
- Is the scenario source format still small enough to avoid premature content-system design?

## Non-goals

Milestone 005 does not introduce:

```text
asset-import scenarios
map-validation scenarios
animation-validation scenarios
shader/material preview scenarios
save/load scenarios
performance scenarios
soak scenarios
packaged-runtime scenarios
visual review packs
full scenario discovery across all folders
schema registry project
JSON Schema package dependency unless the implementation justifies it
```
