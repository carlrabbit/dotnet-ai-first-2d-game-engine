# Mixed World Projection Contract

## Authority

This document is authoritative for projecting authored map content into static spatial world data while keeping runtime entities and mutable component state separate.

## Principle

```text
authored map data != runtime entity registry
```

Maps may contain terrain, tile semantics, static objects, regions, and spawn markers.

Runtime entities contain stable mutable identity and components.

Only authored objects that require mutable runtime behavior need to become entities.

## World layers

### Authored map content

May contain:

- dimensions;
- tile and semantic layers;
- static object declarations;
- markers and spawns;
- asset and tile references.

### Static spatial world

Derived from map content:

- map bounds;
- blocked tile rectangles;
- static object AABBs;
- stable source references;
- deterministic geometry ordering.

### Runtime entity world

Contains:

- player;
- NPCs;
- mutable or independently inspectable objects;
- typed components;
- lifecycle state.

## World units

One map tile represents:

```text
1.0 × 1.0 world units
```

Map cell `(x, y)` covers:

```text
[x, x + 1.0) × [y, y + 1.0)
```

Rendering pixels and source texture resolution do not affect runtime world coordinates.

## Static object source

Initial static object form:

```json
{
  "id": "object.tree.large.smoke",
  "kind": "static-obstacle",
  "assetId": "asset.tile-atlas-smoke",
  "position": { "x": 1.5, "y": 1.5 },
  "bounds": {
    "kind": "aabb",
    "halfWidth": 0.45,
    "halfHeight": 0.45
  }
}
```

Initial geometry is axis-aligned rectangle only.

## Static object validation

Required:

- stable unique object ID;
- supported kind;
- finite coordinates;
- finite positive half-extents;
- supported bounds kind;
- deterministic source ordering;
- valid asset reference when provided.

## Projection evidence

Every static geometry item must retain:

- source map ID;
- source object/layer/cell identity;
- geometry kind;
- world-space bounds;
- semantic source;
- asset/tile reference where applicable;
- deterministic geometry fingerprint.

## Entity instantiation policy

Map markers may direct scenario/runtime entity creation.

Static decorations and obstacles remain static geometry unless mutable behavior, interaction, lifecycle, or independent runtime identity is required.

The initial tree obstacle remains static authored geometry.

## Determinism

Equivalent map content and approved referenced metadata must produce equivalent static geometry, ordering, fingerprints, and diagnostics.
