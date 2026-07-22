# Multi-Fidelity Simulation Artifact Contract

## Root

```text
artifacts/simulation/M033/
```

## Required files

```text
m033-manifest.json
queue-inspection.json
scheduled-triggers.jsonl
trigger-outcomes.jsonl
abstract-regions.json
abstract-routes.jsonl
duration-models.json
fidelity-state.json
transition-events.jsonl
materialization-mappings.jsonl
abstraction-mappings.jsonl
executor-ownership.json
persistence-report.json
conservation-ledger.json
equivalence-report.json
observer-neutrality-report.json
long-horizon-report.json
performance-baseline.json
invariants.json
diagnostics.json
graphical-evidence/
controls/all-abstract/
controls/periodically-switched/
controls/mostly-detailed/
controls/detailed-control/
review-pack/review-manifest.json
review-pack/evidence-index.json
review-pack/architecture-summary.md
review-pack/transition-samples.md
review-pack/equivalence-summary.md
review-pack/graphical-evidence-index.md
review-pack/limitations.md
```

## Schema families

```text
agentic2d.m033.manifest.v1
agentic2d.scheduled-trigger.v1
agentic2d.trigger-outcome.v1
agentic2d.abstract-region-inspection.v1
agentic2d.abstract-route.v1
agentic2d.duration-model.v1
agentic2d.region-fidelity.v1
agentic2d.fidelity-transition.v1
agentic2d.materialization-mapping.v1
agentic2d.abstraction-mapping.v1
agentic2d.executor-ownership.v1
agentic2d.multi-fidelity-persistence-report.v1
agentic2d.multi-fidelity-equivalence.v1
agentic2d.observer-neutrality.v1
agentic2d.m033.invariant-report.v1
agentic2d.m033.review-pack.v1
```

## Trigger evidence

Record due time, order, owner, expected revisions, delivery status, command result, and causal references. Stale/cancelled triggers remain visible.

## Transition evidence

For every transition record direction, prior/new fidelity, revision, instants, activities/reservations, trigger transfer/invalidation, state mappings, validation, and rollback.

## Position mapping

Materialization records abstract location/progress, candidate grid positions, selected cell, repair reason, and route reconstruction. Abstraction records exact position, mapped node/edge, remaining duration, and next trigger.

## Equivalence and neutrality

Report required runs, authored tolerances, zero-tolerance invariants, differences, pass/fail, and omissions. No partial run counts as success. Observer-neutrality must expose systematic effects rather than average them away.

## Conservation

Independent per-region/resource ledger. Reservations are not physical quantity.

## Long horizon

Include target/completed instant, events, safety status, failures, stale/cancelled triggers, queue peak, transitions, save/load count, and rerun fingerprint.

## Graphical evidence

Required for review and linked to structural transition evidence. Screenshots are not semantic authority.

## Forbidden authority

No native handles, wall-clock ordering, pathfinder internals, process IDs, absolute machine paths, screenshot-only claims, or hidden tolerance changes.

## Review pack

Explain architecture/ownership, trigger lifecycle, both transition directions, persistence/recovery, equivalence/neutrality, divergence, and limitations.
