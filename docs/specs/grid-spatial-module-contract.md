# Grid Spatial Module Contract

## Authority

Authoritative for `spatial.grid`, the first reference spatial implementation. It is not the universal engine movement model.

## State and movement

The module owns `GridPosition { X, Y }`. Coordinates are integer cells. Supported directions are north, east, south, and west; accepted movement changes exactly one cell.

## Resolution order

1. entity has valid grid position;
2. destination is in bounds;
3. explicit destination-cell override;
4. approved referenced-tile physical behavior;
5. otherwise blocked/unresolved.

```text
map-cell override → approved tile behavior → blocked/unresolved
```

Visual labels are never movement authority. Entity occupancy is deferred.

## Outcomes

`walkable`, `blocked`, `unresolved`, `out-of-bounds`.

## Events

Recommended: `spatial.movement-accepted`, `spatial.movement-rejected`, `entity.grid-position-changed`.

## Diagnostics

`GRID0001` invalid position; `0002` out of bounds; `0003` unresolved cell/tile; `0004` blocked by override; `0005` blocked by tile semantics; `0006` unresolved semantics; `0007` unsupported intent.

## Determinism

Equivalent snapshot, map, metadata, and intent produce equivalent resolution, facts, command, events, and diagnostics.
