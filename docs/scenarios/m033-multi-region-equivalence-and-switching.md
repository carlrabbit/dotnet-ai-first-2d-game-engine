# M033 Multi-Region Equivalence and Switching Scenario

## Identity

```text
scenario.m033.multi-region-equivalence-and-switching
```

## World

Three bounded M032-style regions: `region.alpha`, `region.beta`, and `region.gamma`.

Each has two generic workers, extraction/storage designations, trees, finite storage, food/water/rest sources, detailed grid, abstract graph, and fixed needs. No cross-region transfer.

## Runs

- all abstract for thirty days;
- periodically switched with exactly one detailed region;
- mostly detailed for one selected region;
- bounded continuously detailed control.

## Transition checkpoints

Switch during travel, harvest progress, carrying, travel to storage, need activity, temporary blockage, and idle/no-work state. Exercise both directions repeatedly.

## Save/load

Save with equal-time triggers, abstract carrying, immediately after materialization, detailed carrying, immediately after abstraction, and mixed fidelity after repeated switches. Load each in a fresh process.

## Assertions

Zero tolerance: conservation, reservation integrity, single completion, one owner, no stale-trigger mutation, stable identity/lifecycle.

Bounded tolerance: completion/travel/idle/blocked/need timing and declared resource checkpoints.

## Graphical evidence

Capture activation request, materialized positions, route reconstruction, continued carrying/work, deactivation, abstract continuation, and later reactivation.

## Long horizon

At least thirty days with event/safety limits. Report acceleration, events, stale/cancelled triggers, transitions, resources, needs, failures, and divergence.

## Negative variants

Trigger due at activation, rollback, invalid materialization cell, disconnected graph, full destination, depleted target, unknown trigger kind, duplicate delivery, detailed update of abstract region, and repeated rapid switching.

## Artifacts

```text
artifacts/simulation/M033/
```
