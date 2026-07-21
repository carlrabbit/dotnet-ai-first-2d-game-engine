# M032 Detailed Forest Logistics Vertical Slice

## Purpose

Prove reusable autonomous work coordination and detailed execution through one bounded playable region.

## Scenario

ID: `scenario.m032.detailed-region.forest-logistics`.

One fixed authored grid region contains two generic workers, at least six trees, finite wood storage, food/water/rest sources, static obstacles with route choices, one temporary dynamic blockage, and one unreachable control target.

Workers have identical policy/capability, carrying capacity of at least three wood, distinct starts, and no traits/skills.

## Sequence

1. Start paused and inspect.
2. Designate at least four reachable trees for extraction.
3. Create/enable wood storage.
4. Resume.
5. Observe distinct autonomous assignments.
6. Observe harvest/haul/deposit.
7. Trigger/wait for a mandatory need and legal interruption.
8. Add temporary blockage during travel and observe bounded replan.
9. Change priority or remove part of designation.
10. Save while carrying.
11. Terminate and fresh-process load.
12. Observe route reconstruction and continuation.
13. Reach exact authored stored-wood target.

## Assertions

- stored target reached;
- all wood accounted across source/loose/carried/stored;
- no leaked reservation on completed/cancelled work;
- no silent blocked worker;
- direct and round-trip deterministic replays produce same final authoritative fingerprint;
- identity remains stable;
- only active region receives detailed updates.

## Explanation checkpoints

Capture initial selections, same-target rejection, mandatory-need interruption, route invalidation/replan, designation invalidation, explicit idle/blocked state if any, and post-load route reconstruction.

## Graphical evidence

Capture initial map, extraction/storage overlays, two workers moving, carried wood, harvest/deposit overlays, mandatory need, route overlay before/after blockage, selected-worker explanation, and post-load continuation.

## Negative variants

Competing workers, no interaction cell, full storage during haul, depleted target, removed designation, mandatory need while reserved, permanent blockage, duplicate pickup/deposit, missing reservation in save, absent route after load, and accidental non-active-region update.

## Artifacts

Write under `artifacts/simulation/M032/` according to the M032 artifact contract.

## Non-goals

No background simulation, cross-region transport, farming/construction/infrastructure, polished assets, or large-scale balancing.
