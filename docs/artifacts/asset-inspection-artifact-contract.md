# Asset Inspection Artifact Contract

## Authority

This document is authoritative for artifacts produced by Milestone 007 asset inspection commands.

This document is not authoritative for:

- scenario execution artifacts;
- content validation artifacts outside asset metadata;
- renderer screenshots or visual preview images;
- map, animation, shader, package, release, or public documentation artifacts;
- human review pack formats beyond the fields explicitly listed here.

## Required artifact paths

An asset inspection command writes artifacts under the requested output directory.

Required files:

```text
<output>/result.json
<output>/diagnostics.json
<output>/asset-summary.json
<output>/tiles.json
```

Required example:

```text
artifacts/assets/tile-atlas-smoke/result.json
artifacts/assets/tile-atlas-smoke/diagnostics.json
artifacts/assets/tile-atlas-smoke/asset-summary.json
artifacts/assets/tile-atlas-smoke/tiles.json
```

## `result.json` required shape

Minimum result shape:

```json
{
  "schema": "agentic2d.asset-inspection.result.v1",
  "command": "asset inspect",
  "target": "asset.tile-atlas-smoke",
  "status": "passed",
  "exitCode": 0,
  "summary": {
    "assetsInspected": 1,
    "tilesDeclared": 4,
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
      "path": "asset-summary.json",
      "kind": "asset-summary"
    },
    {
      "path": "tiles.json",
      "kind": "tile-summary"
    }
  ]
}
```

Additional fields are allowed when deterministic or explicitly documented as volatile.

## `result.json` field definitions

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `schema` | string | Yes | Contract identifier. Must be `agentic2d.asset-inspection.result.v1` for Milestone 007. |
| `command` | string | Yes | Canonical command name, `asset inspect`. |
| `target` | string | Yes | Asset ID or path requested by the CLI. |
| `status` | string | Yes | `passed`, `failed`, or `error`. |
| `exitCode` | number | Yes | Product CLI exit code. |
| `summary` | object | Yes | Aggregate counts for inspected asset, tiles, and diagnostics. |
| `diagnostics` | array | Yes | Inline diagnostic summary. Empty array allowed. |
| `artifacts` | array | Yes | Artifact references relative to the output directory. |

## Status values

```text
passed
failed
error
```

Use:

- `passed` when asset metadata and structural inspection pass;
- `failed` when inspection completes but validation or consistency checks fail;
- `error` when inspection cannot complete because of unexpected command, IO, serialization, or artifact writing errors.

## Summary shape

`summary` must include at least:

```json
{
  "assetsInspected": 1,
  "tilesDeclared": 4,
  "errors": 0,
  "warnings": 0
}
```

Additional deterministic counts are allowed, for example:

```text
visualLabelsProposed
physicalBehaviorsApproved
reviewGatedFields
```

## Diagnostic shape

Diagnostics in `result.json` and `diagnostics.json` should use the same base shape:

```json
{
  "id": "ASSET0003",
  "severity": "error",
  "message": "Declared tile grid does not match image dimensions.",
  "target": "asset.tile-atlas-smoke",
  "field": "tileAtlas"
}
```

Required fields:

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `id` | string | Yes | Stable diagnostic identifier. |
| `severity` | string | Yes | `info`, `warning`, or `error`. |
| `message` | string | Yes | Human-readable explanation. |
| `target` | string | Yes | Asset ID, file path, or content item reference. |
| `field` | string | No | Metadata field or JSON path when applicable. |
| `itemId` | string | No | Stable tile or sub-item ID when applicable. |

Allowed severities:

```text
info
warning
error
```

`diagnostics.json` must contain either:

```json
{
  "schema": "agentic2d.asset-inspection.diagnostics.v1",
  "diagnostics": []
}
```

or a direct diagnostics array if the implementation documents that choice in direct docs and tests. Preferred form is the object wrapper with schema.

## `asset-summary.json` required shape

Minimum shape:

```json
{
  "schema": "agentic2d.asset-inspection.asset-summary.v1",
  "asset": {
    "id": "asset.tile-atlas-smoke",
    "kind": "tile-atlas",
    "title": "Tile atlas smoke asset",
    "metadataPath": "game/assets/metadata/tile-atlas-smoke.asset.json",
    "sourcePath": "game/assets/raw/samples/tile-atlas-smoke.png",
    "mediaType": "image/png"
  },
  "image": {
    "width": 16,
    "height": 16
  },
  "tileAtlas": {
    "tileWidth": 8,
    "tileHeight": 8,
    "columns": 2,
    "rows": 2,
    "declaredTileCount": 4
  },
  "semantics": {
    "visualLabelsProposed": [],
    "physicalBehaviorsApproved": [],
    "reviewRequiredForApprovedPhysicalBehaviors": true
  }
}
```

The numeric values may differ if the sample fixture uses different dimensions. The values must be consistent with the metadata and raw PNG fixture.

## `tiles.json` required shape

Minimum shape:

```json
{
  "schema": "agentic2d.asset-inspection.tiles.v1",
  "assetId": "asset.tile-atlas-smoke",
  "tiles": [
    {
      "id": "tile.smoke.0",
      "x": 0,
      "y": 0,
      "visualLabelsProposed": ["grass"],
      "physicalBehaviorsApproved": [],
      "reviewStatus": "not-required-for-proposals"
    }
  ]
}
```

Required tile fields:

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `id` | string | Yes | Stable tile ID. |
| `x` | integer | Yes | Tile grid x coordinate. |
| `y` | integer | Yes | Tile grid y coordinate. |
| `visualLabelsProposed` | array | Yes | Proposed visual labels, not approved source truth. |
| `physicalBehaviorsApproved` | array | Yes | Human-approved physical/gameplay behavior list. Empty unless review evidence exists. |
| `reviewStatus` | string | Yes | Review status summary for semantics. |

## Artifact references

Artifact references in `result.json` should use this shape:

```json
{
  "path": "asset-summary.json",
  "kind": "asset-summary"
}
```

Paths must be relative to the output directory unless a document explicitly permits repository-relative paths.

## Semantic evidence rule

Asset inspection artifacts must make the semantic boundary visible.

They must distinguish:

```text
structural metadata verified by automation
visual labels proposed by authors or agents
physical/gameplay behaviors approved with review evidence
physical/gameplay behaviors absent or not approved
```

A future agent or human must be able to inspect the artifacts and determine whether gameplay-relevant semantics were approved, merely proposed, or absent.

## Deterministic comparison policy

Tests and validators should compare semantic fields such as:

```text
schema
command
target
status
exitCode
summary.assetsInspected
summary.tilesDeclared
diagnostic IDs
diagnostic severities
asset ID
source path
media type
image width
image height
tile atlas rows/columns/tile size
tile IDs and coordinates
visual label proposal lists
approved physical behavior lists
artifact reference kinds and relative paths
```

Tests must not require exact equality for volatile or environment-specific values.

Avoid volatile fields in Milestone 007. If included, mark them as volatile in direct docs and do not use them in semantic equality tests.

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

When inspection fails, `result.json` and `diagnostics.json` must contain enough information to identify:

```text
which asset failed
which metadata field or source reference failed when applicable
which stable diagnostic ID explains the failure
whether the failure was metadata validation, invalid CLI usage, structural inspection, artifact writing, or unexpected error
```

A failed inspection run must not rely only on stdout/stderr for diagnosis.
