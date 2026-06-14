# Asset Curation Workbench Artifact Contract

## Authority

This document is authoritative for generated asset curation workbench artifacts introduced by Milestone 010.

This document is not authoritative for:

- source asset metadata format;
- asset inspection artifacts;
- interactive editor persistence;
- UI framework architecture;
- public documentation;
- release artifacts.

## Required artifact paths

An asset curation command writes artifacts under the requested output directory.

Required files:

```text
<output>/index.html
<output>/review-data.json
<output>/diagnostics.json
```

Optional file:

```text
<output>/review-board.md
```

Required example:

```text
artifacts/workbench/asset-curation/index.html
artifacts/workbench/asset-curation/review-data.json
artifacts/workbench/asset-curation/diagnostics.json
```

## `review-data.json` required shape

Minimum shape:

```json
{
  "schema": "agentic2d.asset-curation-workbench.review-data.v1",
  "command": "asset curate",
  "asset": {
    "id": "asset.tile-atlas-smoke",
    "metadataPath": "game/assets/metadata/tile-atlas-smoke.asset.json",
    "sourcePath": "game/assets/raw/samples/tile-atlas-smoke.png"
  },
  "reviewPack": {
    "path": "artifacts/review/latest/review-manifest.json"
  },
  "status": "passed",
  "exitCode": 0,
  "tiles": [
    {
      "id": "tile.smoke.0",
      "x": 0,
      "y": 0,
      "visualLabels": [
        {
          "value": "grass",
          "reviewState": "proposed"
        }
      ],
      "physicalBehaviors": [],
      "reviewQuestions": []
    }
  ],
  "diagnostics": [],
  "artifacts": [
    {
      "path": "index.html",
      "kind": "static-html-workbench"
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
| `schema` | string | Yes | Must be `agentic2d.asset-curation-workbench.review-data.v1`. |
| `command` | string | Yes | Canonical command name, `asset curate`. |
| `asset` | object | Yes | Asset identity and source references. |
| `reviewPack` | object | Yes | Review pack input reference. |
| `status` | string | Yes | `passed`, `failed`, or `error`. |
| `exitCode` | number | Yes | Product CLI exit code. |
| `tiles` | array | Yes | Tile-level workbench review data. |
| `diagnostics` | array | Yes | Inline diagnostic summary. Empty array allowed. |
| `artifacts` | array | Yes | Artifact references relative to output directory. |

## Review-state values

Allowed values:

```text
proposed
approved
rejected
needs-revision
not-required
```

The workbench must not emit `approved` for physical/gameplay behavior unless the source metadata or review evidence supports approval.

## Tile review data shape

Minimum tile shape:

```json
{
  "id": "tile.smoke.0",
  "x": 0,
  "y": 0,
  "visualLabels": [
    {
      "value": "grass",
      "reviewState": "proposed"
    }
  ],
  "physicalBehaviors": [],
  "reviewQuestions": []
}
```

Required tile fields:

| Field | Type | Meaning |
|---|---|---|
| `id` | string | Stable tile ID. |
| `x` | number | Tile grid x coordinate. |
| `y` | number | Tile grid y coordinate. |
| `visualLabels` | array | Proposed visual labels and review states. |
| `physicalBehaviors` | array | Approved or review-gated physical/gameplay behaviors. |
| `reviewQuestions` | array | Human-review questions for this tile. |

## `index.html` requirements

The generated HTML must:

- be static;
- not require a web server;
- not require network access;
- not require a package install or JavaScript build pipeline;
- include the asset ID;
- include a structural tile atlas summary;
- include tile IDs;
- visibly distinguish proposed visual labels from approved physical/gameplay behavior;
- include diagnostics or a link/reference to diagnostics.

Inline CSS and minimal inline JavaScript are allowed if deterministic and self-contained. JavaScript is not required.

## `diagnostics.json` required shape

Preferred shape:

```json
{
  "schema": "agentic2d.asset-curation-workbench.diagnostics.v1",
  "diagnostics": []
}
```

Recommended diagnostic IDs:

| ID | Meaning |
|---|---|
| `CURATION0001` | Asset metadata not found. |
| `CURATION0002` | Review pack not found or malformed. |
| `CURATION0003` | Asset inspection evidence missing. |
| `CURATION0004` | Review-state evidence inconsistent. |
| `CURATION0005` | Workbench artifact generation failed. |

Exact IDs may vary if stable and tested.

## Deterministic comparison policy

Tests should compare semantic fields such as:

```text
schema
status
asset ID
tile IDs and coordinates
visual label values and review states
physical behavior values and review states
diagnostic IDs and severities
artifact reference kinds and relative paths
```

Tests should not compare volatile or environment-specific values.
