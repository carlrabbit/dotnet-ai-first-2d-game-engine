# Visual Definition Contract

## Authority

Authoritative for authored visual definitions and visual references from entity definitions and static map objects.

## Principle

Visual definitions describe presentation only. They do not determine entity identity, static-object identity, lifecycle, collision, behavior, interaction, or simulation state.

## Identity and source root

IDs use `visual-definition.<domain>.<name>`. Canonical root: `game/visuals/`.

## Shape

A definition contains schema, stable ID, and one or more parts. Each part contains stable part ID, asset ID, region ID, anchor, finite offset, finite positive world size, layer, integer order, sort mode, and tint.

Supported anchors: `top-left`, `center`, `bottom-center`.

Supported sort modes: `fixed`, `y`.

Supported layers in order: `background`, `ground`, `entities`, `foreground`, `debug`, `ui`.

## References

Entity definitions and static map objects may reference `visualDefinitionId`. Projection preserves source kind, source ID, visual definition, part, asset, and region. A visual reference never changes source ownership.

## Multi-part visuals

The initial tree uses base on `entities/y` and canopy on `foreground/fixed`.

## Validation

Reject unsupported schema, invalid IDs, empty parts, duplicate part IDs, missing assets/regions, non-finite values, non-positive sizes, unknown anchor/layer/sort mode, invalid tint, and nondeterministic invalid content.

## Determinism

Equivalent visual definitions and asset metadata produce equivalent canonical parts, ordering, fingerprints, and diagnostics.
