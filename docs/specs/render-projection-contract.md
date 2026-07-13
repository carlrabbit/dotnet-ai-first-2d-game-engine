# Render Projection Contract

## Authority

Authoritative for backend-neutral semantic projection, command compilation, ordering, static caching, dynamic projection, frame composition, and renderable snapshot reconstruction.

## Read-only boundary

Projection reads immutable runtime snapshots and authored content. It must not mutate runtime state, advance simulation, perform domain resolution, load native resources, or depend on raylib-cs types.

## Coordinates

Origin top-left; X right; Y down; one tile equals `1.0 × 1.0` world units. Projection remains in world units.

## Semantic items

Each item has stable ID, source kind/ID, visual definition/part, asset/region, world destination, anchor, layer, order, sort mode, Y-sort coordinate, tint, and provenance.

## Ordering

Layer index, explicit order, Y coordinate when Y-sorted, then item ID ordinal.

## Static projection

Derived from map content, tile layers, static objects, visuals, and asset metadata. Cache key includes map structural identity, visual/asset revisions, and settings. Cache has no native resources. M015 uses full rebuild on invalidation.

## Dynamic projection

Derived per immutable snapshot from runtime entities, provenance, transforms, and requested overlays. No interpolation.

## Frame composition

Static projection + dynamic projection + client-local overlays produce `RenderFrame` and a backend-neutral command list.

Initial commands: `clear`, `begin-world-camera`, `draw-texture-region`, `draw-solid-rectangle`, `draw-line`, `draw-text`, `end-world-camera`, `begin-screen-space`, `end-screen-space`.

Logical viewport: `320 × 180`.

## Snapshot reconstruction

A renderable snapshot contains enough stable content/reference data to reproduce equivalent semantic projection without simulation. Client camera, selection, and overlay toggles are excluded.

## Determinism

Equivalent authored content, snapshot, settings, and local projection inputs produce equivalent frames and command lists. PNG pixels are not semantic equality authority.
