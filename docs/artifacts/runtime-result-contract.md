# Runtime Result Contract

## Authority

This document is authoritative for `result.json` artifacts produced by Milestone 002 runtime smoke execution.

This document is not authoritative for future asset, map, shader, visual preview, package, release, or human review artifacts.

## Artifact path

The `runtime smoke` command writes:

```text
<output>/result.json
```

Required example:

```text
artifacts/runtime-smoke/result.json
```

## Required shape

The artifact must be valid JSON and include at least these top-level fields:

```json
{
  "schemaVersion": 1,
  "command": "runtime smoke",
  "status": "passed",
  "ticksRequested": 3,
  "finalTick": 3,
  "entities": [],
  "events": [],
  "assertions": [],
  "diagnostics": []
}
```

Additional fields are allowed if they are deterministic or explicitly documented as volatile.

## Field definitions

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `schemaVersion` | integer | Yes | Artifact schema version. Must be `1` for Milestone 002. |
| `command` | string | Yes | Product CLI command that produced the artifact. |
| `status` | string | Yes | `passed`, `failed`, or `error`. |
| `ticksRequested` | integer | Yes | Requested tick count from the CLI. |
| `finalTick` | integer | Yes | Runtime final tick after execution. |
| `entities` | array | Yes | Final-state entity summaries needed by assertions. |
| `events` | array | Yes | Ordered runtime events. |
| `assertions` | array | Yes | Assertion results evaluated by the smoke scenario. |
| `diagnostics` | array | Yes | Structured diagnostics. Empty array allowed. |

## Status values

```text
passed
failed
error
```

Use:

- `passed` when runtime execution completes and all assertions pass;
- `failed` when runtime execution completes but one or more assertions fail;
- `error` when CLI execution, runtime execution, or artifact writing fails unexpectedly.

## Entity summary shape

Each entry in `entities` must include at least:

```json
{
  "id": "entity.player",
  "position": 1
}
```

Additional state fields are allowed only when they are deterministic and useful for diagnostics.

## Event shape

Each entry in `events` must include at least:

```json
{
  "type": "entity.moved",
  "tick": 1,
  "message": "entity.player moved from 0 to 1"
}
```

Required event types for the smoke scenario:

```text
runtime.started
entity.created
command.accepted
entity.moved
runtime.completed
```

Event order must match occurrence order.

## Assertion shape

Each entry in `assertions` must include at least:

```json
{
  "id": "assert.finalTick",
  "passed": true,
  "message": "final tick equals requested tick count"
}
```

Required assertion IDs or equivalent stable names:

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

Exact assertion IDs may vary if they are stable and descriptive.

## Diagnostic shape

Each entry in `diagnostics` must include at least:

```json
{
  "severity": "error",
  "code": "cli.invalidTicks",
  "message": "--ticks must be a positive integer"
}
```

Allowed severities:

```text
info
warning
error
```

Diagnostics should be stable enough for tests to assert codes without depending on prose.

## Volatile fields

Avoid volatile fields in Milestone 002.

If included, these fields are volatile and tests must not require exact equality:

```text
startedAt
completedAt
duration
absolutePath
```

## Deterministic comparison rule

For repeated smoke runs with identical arguments, the following must be equivalent:

```text
schemaVersion
command
status
ticksRequested
finalTick
entities
events excluding explicitly volatile fields
assertions
diagnostics excluding environment-specific details
```
