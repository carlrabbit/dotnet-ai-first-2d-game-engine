# Scenario Runner Artifact Contract

## Authority

This document is authoritative for artifacts produced by Milestone 005 scenario runner commands.

This document is not authoritative for:

- runtime smoke artifacts produced by older `runtime smoke` commands unless those commands explicitly adopt this contract;
- asset previews;
- visual overlays;
- packaged-runtime validation artifacts;
- release artifacts;
- public documentation.

## Required artifact directory

A scenario run writes artifacts under the output directory supplied to:

```text
agentic2d scenario run <scenario-id-or-path> --output <directory>
```

The first required smoke output path is:

```text
artifacts/scenarios/runtime-smoke
```

## Required files

A successful or failed scenario run must write:

```text
<output>/result.json
<output>/events.jsonl
<output>/diagnostics.json
```

If the runner cannot create the output directory or cannot write artifacts, it must return exit code `3` and write a concise console diagnostic. It may not be able to write artifact files in that case.

## `result.json`

### Required shape

The minimum result shape is:

```json
{
  "schema": "agentic2d.scenario.result.v1",
  "scenario": {
    "id": "runtime.smoke",
    "category": "smoke",
    "source": "game/scenarios/smoke/runtime-smoke.json"
  },
  "command": "scenario run",
  "status": "passed",
  "exitCode": 0,
  "runtime": {
    "ticksRequested": 3,
    "finalTick": 3
  },
  "summary": {
    "eventsEmitted": 5,
    "assertionsPassed": 8,
    "assertionsFailed": 0,
    "diagnostics": 0
  },
  "entities": [
    {
      "id": "entity.player",
      "position": 1
    }
  ],
  "assertions": [
    {
      "id": "assert.finalTick",
      "passed": true,
      "message": "final tick equals requested tick count"
    }
  ],
  "artifacts": [
    {
      "path": "events.jsonl",
      "kind": "event-log"
    },
    {
      "path": "diagnostics.json",
      "kind": "diagnostics"
    }
  ]
}
```

Additional fields are allowed when they are deterministic or explicitly marked as volatile.

### Required top-level fields

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `schema` | string | Yes | Must be `agentic2d.scenario.result.v1`. |
| `scenario` | object | Yes | Scenario identity and source summary. |
| `command` | string | Yes | Must be `scenario run` for this milestone. |
| `status` | string | Yes | `passed`, `failed`, or `error`. |
| `exitCode` | integer | Yes | Product CLI exit code. |
| `runtime` | object | Yes | Runtime execution summary. |
| `summary` | object | Yes | Event/assertion/diagnostic counts. |
| `entities` | array | Yes | Final-state entity summaries relevant to assertions. |
| `assertions` | array | Yes | Assertion results. |
| `artifacts` | array | Yes | Relative references to additional artifact files. |

## Status values

| Status | Exit code | Meaning |
|---|---:|---|
| `passed` | 0 | Scenario completed and all assertions passed. |
| `failed` | 1 | Scenario completed but one or more assertions failed. |
| `error` | 2 or 3 | CLI usage, scenario input, runtime execution, or artifact writing prevented normal validation. |

Invalid scenario input should normally produce `status: "error"` and exit code `2`.

Runtime execution or artifact writing failures should normally produce `status: "error"` and exit code `3`.

## Scenario summary shape

The `scenario` object must include at least:

```json
{
  "id": "runtime.smoke",
  "category": "smoke",
  "source": "game/scenarios/smoke/runtime-smoke.json"
}
```

`source` should be repository-relative when the scenario was loaded from a repository file. It may be the resolved scenario ID when the scenario was loaded through ID lookup.

## Runtime summary shape

The `runtime` object must include at least:

```json
{
  "ticksRequested": 3,
  "finalTick": 3
}
```

Additional deterministic fields are allowed, for example:

```text
commandsAccepted
commandsRejected
```

## Assertion shape

Each assertion entry must include at least:

```json
{
  "id": "assert.playerPosition",
  "passed": true,
  "message": "entity.player position equals 1"
}
```

Allowed additional fields:

```text
expected
actual
severity
```

Assertion IDs must be stable.

## Entity shape

Each entity summary must include at least:

```json
{
  "id": "entity.player",
  "position": 1
}
```

Milestone 005 does not require component dumps, hierarchy dumps, visual labels, physics state, navigation state, or asset references.

## `events.jsonl`

Each line must be a valid JSON object.

Minimum event shape:

```json
{"sequence":1,"tick":0,"type":"runtime.started","message":"Runtime started"}
```

Required fields:

| Field | Type | Meaning |
|---|---|---|
| `sequence` | integer | 1-based deterministic event sequence number. |
| `tick` | integer | Runtime tick associated with the event. |
| `type` | string | Stable event type. |
| `message` | string | Human-readable diagnostic text. |

The required smoke events must appear in deterministic occurrence order:

```text
runtime.started
entity.created
command.accepted
entity.moved
runtime.completed
```

## `diagnostics.json`

The diagnostics file must contain a JSON object:

```json
{
  "schema": "agentic2d.diagnostics.v1",
  "diagnostics": []
}
```

Diagnostic entries must include at least:

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

Diagnostics should use stable IDs. Human-readable messages may change, but tests should not rely on exact prose unless the message is the only diagnostic detail available.

## Volatile fields

Avoid volatile fields in Milestone 005 artifacts.

If included, these fields are volatile and must not be part of deterministic equality tests:

```text
startedAt
completedAt
duration
absolutePath
machineName
processId
```

## Deterministic comparison policy

For repeated scenario runs with identical scenario source and CLI arguments, tests should compare:

```text
schema
scenario.id
scenario.category
command
status
exitCode
runtime.ticksRequested
runtime.finalTick
summary counts
entities
assertion IDs and pass/fail states
event sequence/tick/type values
diagnostic IDs and severities
```

Tests must not compare wall-clock or machine-specific values.
