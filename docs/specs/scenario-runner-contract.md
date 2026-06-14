# Scenario Runner Contract

## Authority

This document is authoritative for the first generic scenario runner foundation introduced by Milestone 005.

This document is not authoritative for:

- asset curation workflows;
- map, animation, shader, or UI scenario semantics;
- packaged-runtime validation;
- performance or soak validation;
- public documentation or release readiness;
- a full schema system for all future content.

## Purpose

The scenario runner turns authored scenario input into deterministic runtime execution and structured evidence.

The first supported execution path is:

```text
authored scenario JSON
→ scenario loader and validator
→ deterministic runtime execution
→ assertion evaluation
→ result.json, events.jsonl, diagnostics.json
→ product CLI exit code
```

The runner must execute real engine/runtime behavior. It must not be a success-only validator for a fixed file shape.

## Scenario identity

A scenario has a stable scenario ID.

Scenario IDs must:

- be strings;
- be stable across file moves;
- use lowercase dotted segments by default;
- not depend on filename, display name, or directory position.

Required initial scenario ID:

```text
runtime.smoke
```

## Scenario source format

Milestone 005 uses authored JSON scenario files.

JSON is selected for the first scenario runner because it is strict, deterministic, easy to parse with `System.Text.Json`, and avoids whitespace-sensitive semantics.

A valid Milestone 005 scenario file must include at least:

```json
{
  "schema": "agentic2d.scenario.v1",
  "id": "runtime.smoke",
  "category": "smoke",
  "title": "Runtime smoke",
  "purpose": "Validate deterministic runtime execution through the scenario runner.",
  "seedPolicy": "none",
  "runtime": {
    "ticks": 3
  },
  "initialState": {
    "entities": [
      {
        "id": "entity.player",
        "position": 0
      }
    ]
  },
  "steps": [
    {
      "id": "step.move-player",
      "command": {
        "type": "move",
        "entityId": "entity.player",
        "amount": 1
      }
    }
  ],
  "expectedEvents": [
    "runtime.started",
    "entity.created",
    "command.accepted",
    "entity.moved",
    "runtime.completed"
  ],
  "assertions": [
    {
      "id": "assert.finalTick",
      "type": "finalTickEqualsRequested"
    },
    {
      "id": "assert.playerPosition",
      "type": "entityPositionEquals",
      "entityId": "entity.player",
      "position": 1
    }
  ],
  "artifacts": {
    "result": "result.json",
    "events": "events.jsonl",
    "diagnostics": "diagnostics.json"
  },
  "humanReview": {
    "required": false
  }
}
```

The exact property order in the file is not semantically meaningful. Generated artifacts may preserve a deterministic serialization order if useful.

## Required top-level fields

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `schema` | string | Yes | Scenario file contract. Must be `agentic2d.scenario.v1` for Milestone 005. |
| `id` | string | Yes | Stable scenario ID. |
| `category` | string | Yes | Scenario category, initially `smoke`. |
| `title` | string | Yes | Human-readable display title. Not identity. |
| `purpose` | string | Yes | Short reason the scenario exists. |
| `seedPolicy` | string | Yes | Randomness policy. Initial supported value: `none`. |
| `runtime` | object | Yes | Runtime execution configuration. |
| `initialState` | object | Yes | Initial entities and minimal state needed by the runner. |
| `steps` | array | Yes | Deterministic command steps. |
| `expectedEvents` | array | Yes | Event types expected to occur in deterministic order. |
| `assertions` | array | Yes | Assertions evaluated after or during execution. |
| `artifacts` | object | Yes | Expected artifact filenames relative to the output directory. |
| `humanReview` | object | Yes | Human review policy for the scenario output. |

## Runtime configuration

Milestone 005 supports:

```json
{
  "runtime": {
    "ticks": 3
  }
}
```

Rules:

- `ticks` must be a positive integer.
- The runner must execute the runtime until `finalTick == ticks`.
- The runner must reject missing, zero, negative, non-integer, or malformed tick values with structured diagnostics.

## Initial state

Milestone 005 supports position-only entity state:

```json
{
  "id": "entity.player",
  "position": 0
}
```

Rules:

- Entity IDs must be stable strings.
- Entity IDs must be unique within a scenario.
- Position must be an integer.
- The scenario runner may translate this source state into the existing minimal runtime state model.

## Step semantics

Milestone 005 supports one command type:

```json
{
  "type": "move",
  "entityId": "entity.player",
  "amount": 1
}
```

Rules:

- `type` must be `move`.
- `entityId` must reference an entity declared in initial state.
- `amount` must be an integer.
- The command is applied once.
- The command must not be applied once per tick unless a future scenario contract explicitly adds repeated commands.

## Expected events

The runner must compare required event types by deterministic occurrence order.

For `runtime.smoke`, required events are:

```text
runtime.started
entity.created
command.accepted
entity.moved
runtime.completed
```

Additional deterministic events are allowed only when:

- the required events remain present;
- required event ordering remains unambiguous;
- artifacts include enough information to diagnose additional events.

## Assertion semantics

Milestone 005 must support at least these assertion types:

| Assertion type | Required fields | Meaning |
|---|---|---|
| `finalTickEqualsRequested` | none beyond `id` and `type` | Final runtime tick equals `runtime.ticks`. |
| `entityExists` | `entityId` | Entity exists in final state. |
| `entityPositionEquals` | `entityId`, `position` | Entity position equals expected integer. |
| `eventOccurred` | `eventType` | Event type exists in emitted events. |

The implementation may add more assertion types only if they are deterministic and documented in the implementation summary.

Assertion IDs must be stable strings. Assertion messages may be human-readable prose, but tests should prefer stable IDs and boolean result fields.

## Diagnostics

Scenario loading, validation, runtime execution, artifact writing, and assertion evaluation must produce structured diagnostics when something fails.

Diagnostics must include at least:

```json
{
  "id": "SCENARIO0001",
  "severity": "error",
  "message": "Scenario file is missing required field: id"
}
```

Allowed severities:

```text
info
warning
error
```

Diagnostic IDs must be stable enough for tests to assert.

## Artifact output

A scenario run must write these files when an output directory is provided:

```text
<output>/result.json
<output>/events.jsonl
<output>/diagnostics.json
```

The artifact contract is defined in:

```text
docs/artifacts/scenario-runner-artifact-contract.md
```

## Product CLI command

Milestone 005 introduces:

```text
agentic2d scenario run <scenario-id-or-path> --output <directory>
```

Development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- scenario run game/scenarios/smoke/runtime-smoke.json --output artifacts/scenarios/runtime-smoke
```

Supported scenario reference forms:

1. Repository-relative file path ending in `.json`.
2. Scenario ID `runtime.smoke`, resolved by the runner to the repository's built-in authored smoke scenario.

If both forms are difficult to implement cleanly in this milestone, file path support is required and ID lookup may be implemented only for `runtime.smoke`.

## Exit codes

The product CLI must use the existing product CLI exit-code policy:

| Exit code | Meaning |
|---:|---|
| 0 | Scenario completed and all assertions passed. |
| 1 | Scenario completed but one or more assertions failed. |
| 2 | Invalid CLI usage or invalid scenario input. |
| 3 | Runtime execution error, artifact writing failure, or unhandled command failure. |

Invalid scenario shape is exit code `2` unless the runtime had already started and failed due to execution behavior.

## Determinism requirements

For the same scenario source file, same CLI arguments, and same source revision, repeated scenario runs must produce equivalent semantic artifacts.

Semantic comparison includes:

```text
scenario ID
status
exit code
runtime final tick
events excluding explicitly volatile fields
assertion IDs and pass/fail states
diagnostic IDs and severities
entity final state
```

Tests must not depend on:

```text
absolute paths
wall-clock timestamps
elapsed duration
local SDK path
machine name
process ID
```

## Human review policy

Milestone 005 does not require human review to decide whether `runtime.smoke` passes.

Human review is required for milestone acceptance only to judge the quality of the generated evidence:

- Can a failed scenario be diagnosed from artifacts without guessing?
- Are scenario IDs and assertion IDs stable enough for future agents?
- Is the CLI shape acceptable for future scenario categories?
- Is the scenario source format still minimal and not over-generalized?

## Provider boundary

This repository is the capability provider for scenario execution.

Milestone 005 validates that the engine can run and report scenarios. It does not use the scenario runner to validate a real consumer game.
