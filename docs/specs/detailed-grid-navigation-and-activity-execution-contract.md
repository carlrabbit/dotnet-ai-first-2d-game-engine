# Detailed Grid Navigation and Activity Execution Contract

## Authority

Authoritative for M032 detailed active-region pathfinding, occupancy, movement, interaction positions, invalidation, and detailed activity execution. It extends `spatial.grid` without making grid navigation universal.

## Pipeline

```text
activity destination
→ interaction-position candidates
→ deterministic path
→ fixed-step movement
→ semantic completion command
```

## Ownership

Grid spatial code interprets positions/walkability/occupancy and returns paths. The detailed executor owns transient route/progress and movement intent. Runtime owns identities, positions, M031 activities/reservations, inventories/resources/storage/needs, command commits, and events.

Pathfinding/execution cannot directly mutate domain outcomes.

## Navigation request/result

Request: region, actor, start, goal kind/reference, interaction constraints, movement profile, occupancy policy, spatial revision, request ID.

Goal kinds: exact cell, any area cell, adjacent target, storage interaction, need-source interaction.

Result: status, selected goal cell, ordered path, integer cost, expanded nodes, spatial revision, route fingerprint, diagnostics.

Statuses: found, already-at-goal, unreachable, invalid-start, invalid-goal, stale-revision, bounded-limit.

## Determinism

Equivalent request/snapshot produces identical result. Tie-breaking must be explicit for frontier nodes, goal cells, interaction positions, and neighbors. Recommended order: estimated total cost, travel cost, Y, X, stable ID.

No unordered iteration or thread timing.

## Movement and occupancy

Four-directional movement only. Each accepted transition uses validated spatial/runtime mutation, fixed-step simulation time, occupancy/bounds, route progress, and current events. Visual interpolation is presentation-only.

Occupancy classifications: solid-static, solid-dynamic, non-solid, interaction-target. Deterministic movement ordering permits at most one worker into a forbidden shared cell. Bounded wait/replan replaces advanced crowd simulation.

## Interaction positions

Targets expose cardinal candidate cells constrained by bounds, walkability, occupancy, and work rules. Choose the reachable minimum-cost candidate with stable tie-break. No valid cell yields explicit diagnostic.

## Route state and invalidation

Route state: activity/actor, request, goal, interaction cell, path, index, fingerprint, spatial revision, replan count, status. It is transient or rebuildable.

Invalidate on walkability/solid occupancy change, target movement/destruction, destination invalidation, region transfer, activity revision, or disabled source. Stop before invalid movement, revalidate, bounded replan/wait, then interrupt/fail. Infinite same-state replanning is prohibited.

## Detailed executor

Map activity stages to travel, interaction, pickup, carry, deposit, and need satisfaction. Advance transient progress per fixed step. At semantic boundaries issue validated commands such as arrival, progress, harvest, pickup, deposit, satisfy need, interrupt, or fail.

Interaction progress uses simulation duration, is distinct from animation, and cannot be completed by graphical effects.

## Save/load

Persist semantic destination, stage/progress, actor position, carried inventory, reservations, and movement profile. Rebuild route/search/presentation after load; record reconstruction; prevent duplicate completion.

## Schemas

`agentic2d.detailed-navigation-request.v1`, `agentic2d.detailed-navigation-result.v1`, `agentic2d.detailed-route-state.v1`, `agentic2d.detailed-route-invalidation.v1`, `agentic2d.detailed-executor-state.v1`.

## Diagnostics

`NAV-REQUEST`, `NAV-START`, `NAV-GOAL`, `NAV-INTERACTION`, `NAV-UNREACHABLE`, `NAV-OCCUPANCY`, `NAV-REPLAN`, `EXECUTOR-STAGE`, `EXECUTOR-STALE`, `EXECUTOR-BLOCKED`.

## Non-goals

No abstract travel, diagonal/continuous navigation, navmesh, hierarchical search, multithreading, advanced crowd steering, pathfinder-owned reservations/domain mutation, or renderer authority.
