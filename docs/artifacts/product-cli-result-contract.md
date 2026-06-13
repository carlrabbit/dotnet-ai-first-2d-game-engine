# Product CLI Result Contract

## Authority

This document is authoritative for `result.json` artifacts produced by Milestone 003 product CLI commands.

This document is not authoritative for future scenario, asset, map, shader, packaged-runtime, or human-review artifacts unless those commands explicitly adopt this contract.

## Required artifact

Artifact-producing product CLI commands must create:

```text
<output>/result.json
```

## Required JSON shape

The minimum result shape is:

```json
{
  "schema": "agentic2d.product-cli.result.v1",
  "command": "runtime smoke",
  "status": "passed",
  "exitCode": 0,
  "diagnostics": [],
  "artifacts": [],
  "runtime": {
    "ticksExecuted": 0,
    "eventsEmitted": 0
  }
}
```

## Required fields

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `schema` | string | Yes | Contract identifier. Must be `agentic2d.product-cli.result.v1` for this milestone. |
| `command` | string | Yes | Canonical command name without executable name, for example `runtime smoke` or `validate`. |
| `status` | string | Yes | `passed`, `failed`, or `error`. |
| `exitCode` | number | Yes | Exit code returned by the command. |
| `diagnostics` | array | Yes | Structured diagnostics. Empty array when no diagnostics exist. |
| `artifacts` | array | Yes | Additional artifact references. Empty array when no additional artifacts exist. |
| `runtime` | object | Yes for runtime-backed commands | Runtime summary for the minimal deterministic runtime. |

## `status` values

| Status | Meaning |
|---|---|
| `passed` | Command completed and validation passed. |
| `failed` | Command completed and validation failed. |
| `error` | Command could not complete normally. |

## Diagnostic shape

Diagnostics should use this shape when present:

```json
{
  "id": "CLI0001",
  "severity": "error",
  "message": "Missing required option: --output"
}
```

Required diagnostic fields:

| Field | Type | Meaning |
|---|---|---|
| `id` | string | Stable diagnostic identifier. |
| `severity` | string | `info`, `warning`, or `error`. |
| `message` | string | Human-readable diagnostic message. |

## Artifact reference shape

Artifact references should use this shape when present:

```json
{
  "path": "events.jsonl",
  "kind": "event-log"
}
```

Paths should be relative to the output directory unless there is a documented reason to use repository-relative paths.

## Runtime summary

For Milestone 003 runtime-backed commands, `runtime` must include at least:

```json
{
  "ticksExecuted": 0,
  "eventsEmitted": 0
}
```

Additional fields are allowed if they are deterministic or clearly diagnostic.

## Deterministic comparison policy

Tests and validators should compare semantic fields such as:

```text
schema
command
status
exitCode
runtime.ticksExecuted
runtime.eventsEmitted
```

Tests must not rely on machine-specific absolute paths, local SDK paths, wall-clock timestamps, or elapsed durations.
