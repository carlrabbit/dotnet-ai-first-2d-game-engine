# Asset Metadata Contract

## Authority

This document is authoritative for the first authored asset metadata contract introduced by Milestone 007.

This document is not authoritative for:

- graphical asset curation workbench behavior;
- image recognition or AI visual tagging;
- map, animation, shader, UI, or packaged-runtime semantics;
- renderer integration;
- asset packing or atlas generation;
- public documentation or release readiness;
- a full schema registry for all future asset domains.

## Purpose

Asset metadata makes raw assets addressable, inspectable, validatable, and reviewable by agents and humans.

The first supported asset domain is a tile atlas backed by a raw PNG file.

Required initial flow:

```text
raw PNG file
+ authored asset metadata JSON
→ content validation
→ product CLI asset inspection
→ structural tile atlas artifacts
```

Raw asset creation is out of scope. The repository may include a tiny deterministic sample PNG fixture only to validate structural asset metadata and inspection behavior.

## Asset identity

An asset has a stable asset ID.

Asset IDs must:

- be strings;
- be stable across file moves;
- use lowercase dotted segments by default;
- not depend on filename, display name, folder position, or visual coordinates.

Required initial asset ID:

```text
asset.tile-atlas-smoke
```

If implementation uses another ID, update all direct docs, test expectations, and validation commands consistently.

## Asset metadata source format

Milestone 007 uses authored JSON asset metadata files.

Required initial metadata path:

```text
game/assets/metadata/tile-atlas-smoke.asset.json
```

A valid Milestone 007 asset metadata file must include at least:

```json
{
  "schema": "agentic2d.asset-metadata.v1",
  "id": "asset.tile-atlas-smoke",
  "kind": "tile-atlas",
  "title": "Tile atlas smoke asset",
  "purpose": "Validate structural asset metadata and tile atlas inspection.",
  "source": {
    "path": "game/assets/raw/samples/tile-atlas-smoke.png",
    "mediaType": "image/png"
  },
  "tileAtlas": {
    "tileWidth": 8,
    "tileHeight": 8,
    "columns": 2,
    "rows": 2
  },
  "tiles": [
    {
      "id": "tile.smoke.0",
      "x": 0,
      "y": 0,
      "visualLabelsProposed": ["grass"],
      "physicalBehaviorsApproved": []
    }
  ],
  "provenance": {
    "sourceKind": "repository-fixture",
    "createdBy": "milestone-007",
    "notes": "Synthetic fixture for structural validation only."
  },
  "semantics": {
    "visualLabelsProposed": ["grass"],
    "physicalBehaviorsApproved": []
  },
  "humanReview": {
    "requiredForApprovedPhysicalBehaviors": true,
    "approvals": []
  }
}
```

The exact sample dimensions may differ if the raw PNG fixture and metadata remain internally consistent.

Property order is not semantically meaningful.

## Required top-level fields

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `schema` | string | Yes | Metadata contract. Must be `agentic2d.asset-metadata.v1` for Milestone 007. |
| `id` | string | Yes | Stable asset ID. |
| `kind` | string | Yes | Asset kind. Initial supported value: `tile-atlas`. |
| `title` | string | Yes | Human-readable display title. Not identity. |
| `purpose` | string | Yes | Short reason the asset metadata exists. |
| `source` | object | Yes | Raw asset reference. |
| `tileAtlas` | object | Yes for `tile-atlas` | Structural tile grid declaration. |
| `tiles` | array | Yes for `tile-atlas` | Tile entries addressed by stable tile IDs. |
| `provenance` | object | Yes | Source and authorship/provenance metadata. |
| `semantics` | object | Yes | Asset-level semantic proposal and approval summary. |
| `humanReview` | object | Yes | Review policy and approval evidence. |

## Source reference

Required source shape:

```json
{
  "source": {
    "path": "game/assets/raw/samples/tile-atlas-smoke.png",
    "mediaType": "image/png"
  }
}
```

Rules:

- `source.path` must be repository-relative;
- `source.path` must not be absolute;
- `source.path` must not escape the repository using `..` path traversal;
- `source.mediaType` must be `image/png` for Milestone 007;
- the referenced file must exist for asset inspection;
- content validation may report a missing source as a validation failure.

## Tile atlas declaration

Required tile atlas shape:

```json
{
  "tileAtlas": {
    "tileWidth": 8,
    "tileHeight": 8,
    "columns": 2,
    "rows": 2
  }
}
```

Rules:

- `tileWidth` and `tileHeight` must be positive integers;
- `columns` and `rows` must be positive integers;
- the raw PNG width must equal `tileWidth * columns` or be explicitly accepted by a documented compatibility rule;
- the raw PNG height must equal `tileHeight * rows` or be explicitly accepted by a documented compatibility rule;
- Milestone 007 should prefer exact grid matching.

## Tile entries

A tile entry must include at least:

```json
{
  "id": "tile.smoke.0",
  "x": 0,
  "y": 0,
  "visualLabelsProposed": ["grass"],
  "physicalBehaviorsApproved": []
}
```

Required tile fields:

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `id` | string | Yes | Stable tile ID. |
| `x` | integer | Yes | Tile grid x coordinate, zero-based. |
| `y` | integer | Yes | Tile grid y coordinate, zero-based. |
| `visualLabelsProposed` | array | Yes | Proposed visual labels. These are not automatically approved design truth. |
| `physicalBehaviorsApproved` | array | Yes | Approved physical/gameplay behaviors. Empty unless review evidence exists. |

Rules:

- tile IDs must be unique within the asset;
- `x` must be `>= 0` and `< columns`;
- `y` must be `>= 0` and `< rows`;
- a tile must not occupy the same `(x, y)` coordinate as another tile unless a future contract explicitly supports layers;
- visual labels are proposals;
- approved physical/gameplay behaviors require human review evidence.

## Metadata layers

Milestone 007 recognizes these metadata layers:

| Layer | Initial behavior |
|---|---|
| Structural | Automatically validated. Includes dimensions, tile size, rows, columns, grid coordinates, tile IDs, source reference. |
| Visual | May be authored as proposals, for example `grass`, `flower`, `stone`, `water`. Proposals are not approval. |
| Physical | Review-gated. Includes `walkable`, `blocked`, `collision`, `navigation-cost`, `damage`. |
| Gameplay | Review-gated. Includes `harvestable`, `spawnable`, `interactable`, `progression-blocker`. |
| Narrative/design | Deferred. No durable taxonomy required in Milestone 007. |
| Technical | Minimal. Raw source media type and atlas structural metadata only. |

## Semantic approval rule

The validator must distinguish proposed visual labels from approved gameplay-relevant semantics.

Rules:

- `visualLabelsProposed` may contain values without human approval;
- `physicalBehaviorsApproved` must be empty unless human review approval evidence exists;
- gameplay-relevant approved metadata must not be silently accepted without review evidence;
- if approved physical/gameplay behavior is present without review evidence, validation must fail or emit an error diagnostic.

High-impact semantics include at least:

```text
walkable
blocked
collision
navigation-cost
damage
interactable
progression-blocker
spawnable
```

## Human review evidence

Required human review shape:

```json
{
  "humanReview": {
    "requiredForApprovedPhysicalBehaviors": true,
    "approvals": []
  }
}
```

If approvals are present, each approval should include at least:

```json
{
  "id": "review.asset-smoke.physical-behavior",
  "approvedBy": "human",
  "scope": "tile.smoke.0",
  "approvedAt": "manual-review-recorded-outside-milestone"
}
```

Milestone 007 does not need to implement a full review ledger. It only needs enough structure to prevent accidental auto-approval of review-gated semantics.

## Provenance

Required provenance shape:

```json
{
  "provenance": {
    "sourceKind": "repository-fixture",
    "createdBy": "milestone-007",
    "notes": "Synthetic fixture for structural validation only."
  }
}
```

Rules:

- `sourceKind` must be present;
- `createdBy` must be present;
- provenance must not imply generated or externally licensed asset origin unless the repository has evidence for that claim.

## Content validation support

Milestone 007 extends content validation to support:

```text
assets
```

and direct validation of:

```text
*.asset.json
```

Asset metadata validation must check at least:

- required fields;
- schema value;
- stable asset ID;
- supported asset kind;
- safe source path;
- source file existence when validation runs in a repository context;
- supported media type;
- tile atlas shape;
- unique tile IDs;
- tile coordinate bounds;
- duplicate tile coordinates;
- semantic approval rule;
- provenance shape;
- human review shape.

## Asset inspection command

Milestone 007 introduces:

```text
agentic2d asset inspect <asset-id-or-path> --output <directory>
```

Supported target forms:

| Form | Required support | Meaning |
|---|---:|---|
| `asset.tile-atlas-smoke` | Yes | Inspect the known smoke asset metadata. |
| repository-relative `.asset.json` path | Yes | Inspect a single asset metadata file. |
| arbitrary folder path | No | Deferred. |
| raw image path without metadata | No | Deferred. |
| glob expression | No | Deferred. |

The command must produce artifacts defined by `docs/artifacts/asset-inspection-artifact-contract.md`.

## PNG structural inspection

Asset inspection must verify raw PNG structural data at least enough to compare image dimensions to the declared tile atlas grid.

Preferred implementation:

- parse the PNG header directly or use a narrow standard-library-friendly approach;
- avoid renderer or image transformation dependencies;
- do not decode pixels for semantic labels;
- do not perform image recognition.

Required observed fields:

```text
image width
image height
media type
```

## Diagnostics

Asset validation and inspection diagnostics must be structured and stable.

Minimum diagnostic shape:

```json
{
  "id": "ASSET0003",
  "severity": "error",
  "message": "Declared tile grid does not match image dimensions.",
  "target": "asset.tile-atlas-smoke",
  "field": "tileAtlas"
}
```

Recommended diagnostic IDs:

| ID | Meaning |
|---|---|
| `ASSET0001` | Missing required asset metadata field. |
| `ASSET0002` | Invalid asset source reference. |
| `ASSET0003` | Invalid tile grid or grid/image mismatch. |
| `ASSET0004` | Duplicate tile ID or coordinate. |
| `ASSET0005` | Semantic approval violation. |
| `ASSET0006` | Invalid provenance. |
| `ASSET0007` | Unsupported asset kind. |
| `ASSET0008` | Unsupported media type. |

The implementation may reuse `CONTENT####` IDs for generic validation failures if asset-specific IDs are not needed. Tests should assert stable IDs selected by the implementation.

## Determinism requirements

For the same metadata file, raw PNG fixture, CLI arguments, and source revision, repeated validation and inspection runs must produce equivalent semantic artifacts.

Semantic comparison includes:

```text
asset ID
source path
media type
image dimensions
tile atlas dimensions
tile IDs and coordinates
proposed visual labels
approved physical behaviors
status
exit code
diagnostic IDs and severities
artifact reference kinds and paths
```

Tests must not depend on:

```text
absolute paths
wall-clock timestamps
elapsed duration
local SDK path
machine name
process ID
filesystem enumeration order without deterministic sorting
```

## Relationship to content validation

Asset inspection is allowed to call the content validation logic first.

Content validation must still be able to report metadata errors without performing a full inspection command.

Asset inspection may perform additional source-file structural checks, such as PNG dimensions, that are not meaningful when validating syntax-only metadata outside a repository context.

## Human review policy

Human review is not required to decide whether structural asset validation passes.

Human review is required for milestone acceptance to judge whether the semantic boundary is visible and enforceable:

- proposed visual labels are proposals;
- approved physical/gameplay behaviors require review evidence;
- artifacts make review-gated semantics clear;
- the sample does not imply production art or taxonomy decisions.
