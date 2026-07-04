# Map Content Contract

## Authority

Authoritative for the first map domain and map inspection. It does not define rendering, physics, collision resolution, pathfinding, or a scene graph.

## Purpose

Prove that stable asset and tile IDs can be consumed by another authored content domain.

## Source format

Schema: `agentic2d.map.v1`.

Required path and ID:

```text
game/maps/smoke/map-smoke.map.json
map.smoke
```

Minimum shape:

```json
{
  "schema": "agentic2d.map.v1",
  "id": "map.smoke",
  "title": "Smoke map",
  "width": 2,
  "height": 2,
  "tileSize": {"width": 8, "height": 8},
  "assetRefs": [{"assetId": "asset.tile-atlas-smoke"}],
  "layers": [{
    "id": "layer.ground",
    "kind": "tile",
    "cells": [{"x": 0, "y": 0, "assetId": "asset.tile-atlas-smoke", "tileId": "tile.smoke.0"}]
  }],
  "markers": [{"id": "marker.player-spawn", "kind": "spawn", "x": 0, "y": 0}]
}
```

## Validation

Extend `content validate` for `maps` and `.map.json`.

Validate JSON/schema, stable IDs, positive dimensions/tile size, unique layers/markers, supported layer kind `tile`, bounds, asset/tile references, deterministic ordering, and review-gated semantic dependencies. Visual proposals cannot be treated as physical/gameplay truth.

## Inspection

```text
agentic2d map inspect <map-id-or-path> --output <directory>
```

Required targets: `map.smoke` and repository-relative `.map.json` paths. Inspection validates first, then emits summaries and resolved references.

## Diagnostics

| ID | Meaning |
|---|---|
| `MAP0001` | Missing/invalid field. |
| `MAP0002` | Invalid stable ID. |
| `MAP0003` | Invalid dimensions or bounds. |
| `MAP0004` | Duplicate identity. |
| `MAP0005` | Asset not found. |
| `MAP0006` | Tile not found. |
| `MAP0007` | Unsupported layer kind. |
| `MAP0008` | Review-gated requirement unsatisfied. |

## Determinism

Equivalent map and referenced metadata produce semantically equivalent outputs. Discovery and lists are deterministically sorted.
