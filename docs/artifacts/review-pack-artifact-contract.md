# Review Pack Artifact Contract

## Authority

This document is authoritative for artifacts produced by Milestone 010 review pack commands.

This document is not authoritative for:

- scenario execution artifacts themselves;
- content validation artifacts themselves;
- asset inspection artifacts themselves;
- generated curation workbench artifacts;
- public documentation;
- release artifacts.

## Required artifact paths

A review pack command writes artifacts under the requested output directory.

Required files:

```text
<output>/review-summary.md
<output>/review-manifest.json
<output>/diagnostics.json
```

Optional file:

```text
<output>/artifact-index.json
```

Required example:

```text
artifacts/review/latest/review-summary.md
artifacts/review/latest/review-manifest.json
artifacts/review/latest/diagnostics.json
```

## `review-manifest.json` required shape

Minimum shape:

```json
{
  "schema": "agentic2d.review-pack.manifest.v1",
  "command": "review pack",
  "input": {
    "artifactRoot": "artifacts"
  },
  "status": "passed",
  "exitCode": 0,
  "summary": {
    "artifactGroupsIncluded": 3,
    "errors": 0,
    "warnings": 0,
    "reviewQuestions": 2
  },
  "artifactGroups": [
    {
      "kind": "scenario-runner",
      "status": "passed",
      "path": "artifacts/scenarios/runtime-smoke/result.json"
    },
    {
      "kind": "content-validation",
      "status": "passed",
      "path": "artifacts/content/assets/result.json"
    },
    {
      "kind": "asset-inspection",
      "status": "passed",
      "path": "artifacts/assets/tile-atlas-smoke/result.json"
    }
  ],
  "sourceItems": [
    {
      "kind": "asset",
      "id": "asset.tile-atlas-smoke",
      "path": "game/assets/metadata/tile-atlas-smoke.asset.json"
    }
  ],
  "reviewQuestions": [
    {
      "id": "review.asset.tile-atlas-smoke.semantic-proposals",
      "target": "asset.tile-atlas-smoke",
      "question": "Are proposed visual labels acceptable as proposals?"
    }
  ],
  "diagnostics": [],
  "artifacts": [
    {
      "path": "review-summary.md",
      "kind": "review-summary"
    },
    {
      "path": "diagnostics.json",
      "kind": "diagnostics"
    }
  ]
}
```

Additional deterministic fields are allowed.

## Field definitions

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `schema` | string | Yes | Must be `agentic2d.review-pack.manifest.v1`. |
| `command` | string | Yes | Canonical command name, `review pack`. |
| `input` | object | Yes | Input artifact root information. |
| `status` | string | Yes | `passed`, `failed`, or `error`. |
| `exitCode` | number | Yes | Product CLI exit code. |
| `summary` | object | Yes | Aggregate counts for artifact groups, diagnostics, and review questions. |
| `artifactGroups` | array | Yes | Included known artifact groups. |
| `sourceItems` | array | Yes | Source content items represented by the pack when known. |
| `reviewQuestions` | array | Yes | Questions requiring human judgment. Empty array allowed. |
| `diagnostics` | array | Yes | Inline diagnostic summary. Empty array allowed. |
| `artifacts` | array | Yes | Artifact references relative to output directory. |

## Status values

```text
passed
failed
error
```

Use:

- `passed` when pack generation completes and no error diagnostics exist;
- `failed` when generation completes but known artifact groups contain failed statuses or contract-level errors;
- `error` when pack generation cannot complete because of unexpected IO, serialization, or artifact writing errors.

## Artifact group shape

Minimum artifact group shape:

```json
{
  "kind": "asset-inspection",
  "status": "passed",
  "path": "artifacts/assets/tile-atlas-smoke/result.json"
}
```

Required fields:

| Field | Type | Meaning |
|---|---|---|
| `kind` | string | Known artifact family. |
| `status` | string | Observed artifact group status. |
| `path` | string | Repository-relative or input-root-relative artifact path. |

Known initial `kind` values:

```text
scenario-runner
content-validation
asset-inspection
```

## Review question shape

Minimum review question shape:

```json
{
  "id": "review.asset.tile-atlas-smoke.semantic-proposals",
  "target": "asset.tile-atlas-smoke",
  "question": "Are proposed visual labels acceptable as proposals?"
}
```

Required fields:

| Field | Type | Meaning |
|---|---|---|
| `id` | string | Stable review question ID. |
| `target` | string | Source item, artifact group, or semantic item. |
| `question` | string | Human-readable review question. |

## `review-summary.md` required sections

The Markdown summary must include at least:

```text
# Review Pack
Status
Included artifact groups
Diagnostics
Source items
Human review questions
Artifact references
```

The summary must be useful without reading implementation source code.

## `diagnostics.json` required shape

Preferred shape:

```json
{
  "schema": "agentic2d.review-pack.diagnostics.v1",
  "diagnostics": []
}
```

Diagnostic objects should follow the repository diagnostic convention:

```json
{
  "id": "REVIEW0001",
  "severity": "warning",
  "message": "Known artifact group was not found.",
  "target": "artifacts/assets/tile-atlas-smoke"
}
```

Recommended diagnostic IDs:

| ID | Meaning |
|---|---|
| `REVIEW0001` | Known artifact group missing. |
| `REVIEW0002` | Known artifact group malformed. |
| `REVIEW0003` | Included artifact group failed. |
| `REVIEW0004` | Source item reference incomplete. |
| `REVIEW0005` | Review question generated for missing approval evidence. |

Exact IDs may vary if they are stable and tested.

## Deterministic comparison policy

Tests should compare semantic fields such as:

```text
schema
status
exitCode
summary counts
artifact group kinds and statuses
source item IDs
review question IDs
diagnostic IDs and severities
artifact reference kinds and relative paths
```

Tests must not require exact equality for volatile or environment-specific values.
