# Autonomous Detailed-Region Execution Architecture

## Purpose

Define how M032 extends M031 without coupling shared rules to pathfinding or presentation.

## Shape

```text
input/policy → designation commands → simulation world
                                  ├→ opportunity projection → pure worker evaluation
                                  │                         → atomic start-work command
                                  └→ detailed executor → navigation queries
                                                        → movement/progress commands
                                                        → semantic completion commands
                                                             ↓
                                                   authoritative outcomes/events
                                                             ↓
                                               inspection + read-only rendering
```

## Placement

Autonomous work coordination, detailed grid navigation, detailed activity execution, logistics, and fixed needs are explicit optional provider capabilities. The forest scenario is bounded dogfood.

## Boundaries

- Opportunity derivation and evaluation read immutable state and never reserve/create/mutate.
- Assignment revalidates, reserves, and starts activity atomically.
- M031 activity state is semantic; detailed executor state is route/progress only.
- Future M033 abstract execution must reuse shared activity and completion commands.
- Navigation knows spatial goals/occupancy, not work priority, needs, reservations, inventory, storage semantics, or completion rules.
- Resource/inventory/storage are authoritative; carrying visuals are derived.
- Needs enter the same selector with fixed priority classes.
- Exactly one region receives detailed systems; other regions remain persistent and unadvanced.
- Persist semantic state; rebuild opportunities, routes, and render projections.
- Rendering is read-only; input translates pointer/action to designation commands.

## Failure states

Every failure settles into retry-after-revision, bounded wait, replan, interrupt, fail, or explicit idle. No infinite retry, silent stall, leaked reservation, teleportation, UI state repair, or success event after failure.

## Performance

Measure opportunity derivation, candidate evaluation, path searches/nodes, movement, replans, transitions, reservations, and rendering. No speculative multithreading/storage rewrite.

## Invariants

1. M031 is semantic authority.
2. Opportunities are derived; activities/reservations authoritative.
3. Evaluation is pure until assignment.
4. Pathfinding is deterministic/read-only.
5. Detailed execution owns transient progress only.
6. Integer logistics conserve quantity.
7. Needs use semantic time/common selection.
8. One detailed region.
9. Input/render adapters do not own simulation.
10. Blocked work is bounded/explainable.
11. Shared rules contain no abstract/detailed branch.
