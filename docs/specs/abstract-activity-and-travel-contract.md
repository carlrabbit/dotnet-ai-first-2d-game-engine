# Abstract Activity and Travel Contract

## Authority

Authoritative for M033 abstract activity execution, coarse travel, duration models, and threshold scheduling.

## Shared semantics

Abstract execution uses M031/M032 work selection, activities, reservations, resource/inventory/storage rules, needs, commands, and factual events. No abstract-only gameplay rules.

## Abstract location graph

Regions define stable nodes and edges. Nodes may represent housing, forest, storage, food, water, rest, and portals. Edges contain stable ID, endpoints, integer cost, access, revision, and declared modifiers.

## Abstract position

An entity is at a node or on an edge with origin, destination, departure, and planned arrival.

## Travel planning

Inputs: actor, origin, destination, movement profile, carrying state, graph revision. Outputs: stable route summary, integer cost, departure/arrival, fingerprint, and diagnostics.

## Duration models

Typed deterministic models cover travel, harvest, pickup, deposit, eat, drink, and rest. Inputs/constants are inspectable. No wall clock, hidden randomness, personality, or skill.

## One transition at a time

Schedule only the next meaningful activity transition. Delivery revalidates and issues the shared command before planning the next stage.

## Need integration

Lazily integrate from the last authoritative instant. Schedule next warning/mandatory threshold. Rate or source changes invalidate and rebuild triggers.

## Interruption

Mandatory needs may invalidate ordinary triggers and issue shared interruption commands. Carried inventory remains authoritative.

## Persistence

Persist abstract location/edge progress, ownership, trigger continuation, and duration inputs required for deterministic continuation.

## Diagnostics

```text
ABS-LOCATION
ABS-GRAPH
ABS-TRAVEL
ABS-DURATION
ABS-ACTIVITY
ABS-NEED
ABS-INTERRUPTION
```

## Exclusions

No detailed pathfinding, transient individual obstacles, animation, exact congestion, cross-region travel, or infrastructure networks.
