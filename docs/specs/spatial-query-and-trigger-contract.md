# Spatial Query and Trigger Contract

## Authority

This document is authoritative for read-only entity spatial queries, query filtering and ordering, entity-owned trigger volumes, trigger overlap state, and entered/exited transitions.

It is not authoritative for physical entity collision, navigation, or pathfinding.

## Spatial query operations

Required:

```text
lookup entity spatial state
query AABB overlap
query radius/proximity
```

Queries read immutable runtime snapshots and do not mutate state.

The initial implementation may scan all compatible entities.

## Spatial representation

Milestone 014 queries continuous entities using:

- `component.continuous-transform-2d`;
- optional `component.collision-aabb-2d`;
- `component.spatial-membership`.

Grid entities are not required to participate unless existing generic contracts support them without ambiguity.

## AABB overlap query

Inputs:

- world/spatial membership;
- query AABB;
- filters;
- optional excluded entity ID.

Result ordering:

```text
entity ID ordinal
```

## Radius query

Inputs:

- world/spatial membership;
- center;
- finite non-negative radius;
- filters;
- optional excluded entity ID.

Distance is measured between transform positions.

Result ordering:

```text
distance ascending
→ entity ID ordinal
```

## Filters

Supported predicates:

- explicit entity ID;
- required semantic tags;
- required component type IDs.

Predicates combine with logical AND.

No arbitrary expressions, regex filters, scripts, or callbacks are supported.

## Trigger volume

Stable component ID:

```text
component.trigger-volume-2d
```

The trigger is an entity-owned AABB centered on the entity continuous transform.

Required values:

- finite positive half-width;
- finite positive half-height;
- filter;
- optional stable trigger ID.

Trigger volumes are non-solid and do not affect movement resolution.

Static map-region triggers are excluded.

## Trigger state

The runtime owns previous overlap state keyed by:

```text
(triggerId, entityId)
```

Per evaluation:

```text
entered = current - previous
exited = previous - current
```

Required transitions:

```text
trigger.entered
trigger.exited
```

`trigger.stayed` is unsupported.

## Evaluation phase

Trigger evaluation occurs after accepted movement/component mutations for the tick.

Initial qualifying overlap emits `trigger.entered` on first evaluation.

## Ordering

Required transition ordering:

```text
trigger ID ordinal
→ entity ID ordinal
→ entered before exited
```

## Events

Required payload:

- trigger ID;
- trigger owner entity ID;
- affected entity ID;
- tick;
- source query/filter information sufficient for inspection.

## Determinism

Equivalent snapshots and prior overlap state must produce equivalent candidate ordering, filtered results, overlap state, transitions, events, and diagnostics.
