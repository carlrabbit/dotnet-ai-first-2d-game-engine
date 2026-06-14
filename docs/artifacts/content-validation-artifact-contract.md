# Content Validation Artifact Contract

## Authority

This document is authoritative for artifacts produced by Milestone 006 content validation commands.

This document is not authoritative for scenario execution artifacts, asset preview artifacts, visual overlays, packaged-runtime artifacts, public documentation, or release artifacts.

## Required artifact paths

A content validation command writes artifacts under the requested output directory.

Required files:

```text
<output>/result.json
<output>/diagnostics.json
```

Recommended file:

```text
<output>/validated-items.json
```

Required example:

```text
artifacts/content/scenarios/result.json
artifacts/content/scenarios/diagnostics.json
artifacts/content/scenarios/validated-items.json
```

## `result.json` required shape

Minimum result shape:

```json
{
  "schema": "agentic2d.content-validation.result.v1",
  "command": "content validate",
  "scope": "scenarios",
  "status": "passed",
  "exitCode": 0,
  "summary": {
    "itemsValidated": 1,
    "errors": 0,
    "warnings": 0
  },
  "diagnostics": [],
  "artifacts": [
    {
      "path": "diagnostics.json",
      "kind": "diagnostics"
    },
    {
      "path": "validated-items.json",
      "kind": "validated-items"
    }
  ]
}
```

Additional fields are allowed if deterministic or explicitly documented as volatile.

## `result.json` field definitions

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `schema` | string | Yes | Contract identifier. Must be `agentic2d.content-validation.result.v1` for Milestone 006. |
| `command` | string | Yes | Canonical command name, `content validate`. |
| `scope` | string | Yes | Scope or path requested by the CLI. |
| `status` | string | Yes | `passed`, `failed`, or `error`. |
| `exitCode` | number | Yes | Product CLI exit code for the validation run. |
| `summary` | object | Yes | Aggregate counts for validated content and diagnostics. |
| `diagnostics` | array | Yes | Inline diagnostic summary. Empty array allowed. |
| `artifacts` | array | Yes | Artifact references relative to the output directory. |

## Status values

```text
passed
failed
error
```

Use:

- `passed` when validation completed and no error diagnostics exist;
- `failed` when validation completed and content contract errors exist;
- `error` when validation could not complete because of unexpected command, IO, serialization, or artifact writing errors.

## Summary shape

`summary` must include at least:

```json
{
  "itemsValidated": 1,
  "errors": 0,
  "warnings": 0
}
```

Additional deterministic counts are allowed, for example:

```text
infos
filesRead
scenariosValidated
assetsValidated
```

## Diagnostic shape

Diagnostics in `result.json` and `diagnostics.json` should use the same base shape:

```json
{
  "id": "CONTENT0001",
  "severity": "error",
  "message": "Missing required field: id",
  "target": "game/scenarios/smoke/runtime-smoke.json",
  "field": "id"
}
```

Required fields:

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `id` | string | Yes | Stable diagnostic identifier. |
| `severity` | string | Yes | `info`, `warning`, or `error`. |
| `message` | string | Yes | Human-readable explanation. |
| `target` | string | Yes | Scope, file path, or content item reference. |
| `field` | string | No | JSON field or path when applicable. |
| `itemId` | string | No | Stable content item ID when applicable. |

Allowed severities:

```text
info
warning
error
```

`diagnostics.json` must contain either:

```json
{
  "schema": "agentic2d.content-validation.diagnostics.v1",
  "diagnostics": []
}
```

or a direct diagnostics array if the implementation documents that choice in direct docs and tests. Preferred form is the object wrapper with schema.

## Validated items shape

If `validated-items.json` is produced, it should use this shape:

```json
{
  "schema": "agentic2d.content-validation.items.v1",
  "items": [
    {
      "kind": "scenario",
      "id": "runtime.smoke",
      "path": "game/scenarios/smoke/runtime-smoke.json",
      "status": "passed"
    },
    {
      "kind": "asset",
      "id": "asset.tile-atlas-smoke",
      "path": "game/assets/metadata/tile-atlas-smoke.asset.json",
      "status": "passed"
    }
  ]
}
```

Required item fields:

| Field | Type | Meaning |
|---|---|---|
| `kind` | string | Content kind, currently `scenario` or `asset`. |
| `id` | string | Stable content item ID. |
| `path` | string | Repository-relative source path. |
| `status` | string | `passed`, `failed`, or `error` for that item. |

## Artifact references

Artifact references in `result.json` should use this shape:

```json
{
  "path": "diagnostics.json",
  "kind": "diagnostics"
}
```

Paths must be relative to the output directory unless a document explicitly permits repository-relative paths.

## Deterministic comparison policy

Tests and validators should compare semantic fields such as:

```text
schema
command
scope
status
exitCode
summary.itemsValidated
summary.errors
summary.warnings
diagnostic IDs
diagnostic severities
validated item IDs
validated item statuses
artifact reference kinds and relative paths
```

Tests must not require exact equality for volatile or environment-specific values.

Avoid volatile fields in Milestone 006. If included, mark them as volatile in direct docs and do not use them in semantic equality tests.

Potential volatile fields:

```text
startedAt
completedAt
duration
absolutePath
machineName
processId
```

## Failure evidence rule

When validation fails, `result.json` and `diagnostics.json` must contain enough information to identify:

```text
which content item failed
which field or reference failed when applicable
which stable diagnostic ID explains the failure
whether the failure was content validation, invalid CLI usage, or unexpected error
```

A failed validation run must not rely only on stdout/stderr for diagnosis.
