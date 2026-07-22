# Settlement Infrastructure and Operations Artifact Contract

## Root

```text
artifacts/simulation/M034/
```

Required:

```text
m034-manifest.json
world-dashboard.json
region-dashboards/
construction-plans.json
structures.json
resource-ledger.json
water-flow.json
farm-production.json
comfort-capacity.json
maintenance.json
roads.json
work-backlog.json
alerts.jsonl
event-journal.jsonl
operations-commands.jsonl
persistence-report.json
mixed-fidelity-report.json
shortage-recovery-report.json
sustained-run-report.json
performance-baseline.json
invariants.json
diagnostics.json
structural-frames/
graphical-evidence/
review-pack/
```

Core schema families:

```text
agentic2d.construction-plan.v1
agentic2d.infrastructure-state.v1
agentic2d.resource-flow.v1
agentic2d.crop-production.v1
agentic2d.comfort-capacity.v1
agentic2d.maintenance-state.v1
agentic2d.settlement-alert.v1
agentic2d.operations-dashboard.v1
agentic2d.shortage-recovery.v1
agentic2d.m034.review-pack.v1
```

The resource ledger independently accounts for source/extracted, loose, delivered-to-plan, construction-consumed, carried, stored, need-consumed, produced, and explicit loss.

Construction evidence records lifecycle, material, progress, completion/cancellation, structure identity, and causality.

Alert evidence records lifecycle and causes; UI state is not alert authority.

Sustained-run evidence covers fourteen post-stabilization days, reserves, failures, backlog, event counts, transitions, save/load, and deterministic rerun.

Graphical captures link to structural frame and simulation instant.

Invariants include conservation, valid footprints, single completion, material accounting, capacity, crop state, condition/maintenance, alert cause integrity, one detailed region, cross-fidelity semantics, and save/load continuation.
