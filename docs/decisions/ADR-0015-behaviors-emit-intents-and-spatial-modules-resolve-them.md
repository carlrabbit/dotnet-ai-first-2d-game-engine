# ADR-0015 — Behaviors Emit Intents and Spatial Modules Resolve Them

## Status

Proposed for Milestone 012. Accept when implementation is accepted.

## Context

Different genres require incompatible spatial models. A universal hard-coded movement model would either be grid-specific or become a bloated core abstraction.

## Decision

Compiled C# behaviors read immutable snapshots and emit intents. Explicitly registered spatial modules own spatial state and resolve intents into accepted commands or rejected domain outcomes. The first module is `spatial.grid` with `GridPosition`. Scenario content owns activation, lifecycle, and module selection. One phase uses one immutable snapshot and deterministic intent ordering.

## Consequences

Benefits: behavior is decoupled from spatial representation, direct mutation is constrained, rejections remain observable, and future modules reuse the same phases.

Costs: intent and command types are separate, phases are explicit, registration must be maintained, and scenario validation expands.

## Rejected alternatives

Direct position mutation, one universal `ISpatialModel`, reflection/runtime discovery, and grid coordinates in engine core are rejected.
