# Autonomous Detailed Region Artifact Contract

## Authority

Authoritative for M032 semantic, structural, graphical, and review evidence.

## Root and required set

```text
artifacts/simulation/M032/
  m032-manifest.json
  region-inspection.json
  designations.json
  work-opportunities.json
  worker-decisions.jsonl
  activities.json
  reservations.json
  navigation-requests.jsonl
  navigation-results.jsonl
  route-events.jsonl
  logistics-ledger.json
  needs.json
  command-results.jsonl
  domain-events.jsonl
  persistence-report.json
  fingerprints.json
  invariants.json
  diagnostics.json
  performance-baseline.json
  structural-frames/
  graphical-evidence/
  forest-logistics/direct/
  forest-logistics/roundtrip/
  forest-logistics/comparison.json
  review-pack/review-manifest.json
  review-pack/evidence-index.json
  review-pack/play-flow.md
  review-pack/decision-explanation-samples.md
  review-pack/graphical-evidence-index.md
  review-pack/limitations.md
```

## Schema families

`agentic2d.m032.manifest.v1`, designation/opportunity/decision/navigation/route/logistics/needs/persistence/comparison/invariant/review-pack v1 families.

## Required semantics

Designation evidence records stable ID, kind, region, canonical cells, priority, state, revision, commands/events, and affected opportunities—never screen coordinates as authority.

Opportunity evidence distinguishes current projection from historical selected evidence and records deterministic key, target/destination, quantity, source, priority, requirements, derivation revision, and blocking reason.

Worker decision JSONL records ordered candidates, factor values, rejection reasons, path estimate, reservations, resulting activity, interruption/replan context, and causality.

Navigation evidence records request/result, interaction candidates, occupancy/spatial revision, path/cost/nodes/fingerprint/status/diagnostics. Route events include created, advanced, contended, invalidated, replanned, blocked, completed, and reconstructed-after-load.

The logistics ledger independently accounts for source, loose, reserved, carried, stored, and consumed quantities. Reservations are never counted as physical quantity.

Needs evidence records current level, integration instant, thresholds, selected need work, interruption, satisfaction, and revision.

Structural frames show designation overlays, worker positions/facing, activity glyph, carried resource, interaction effect, need warning, blocked/replan status, and route overlay.

Graphical evidence is mandatory for review, linked to structural frames and environment classification. A skipped graphical run is not approval evidence.

Persistence evidence proves save while carrying, fresh-process load, semantic state/reservations preserved, route reconstructed, no duplicate effects, and matching final fingerprints.

## Invariants

One detailed region; no updates elsewhere; unique IDs; no duplicate exclusive assignment; valid path cells; forbidden occupancy absent; bounded replans; no silent blocked activity; no leaked reservations; logistics conservation; valid need integration; no semantic effect without successful command; correct reconstruction.

Partial invariant output is not aggregate success.

## Review pack

Bound review to designation flow, autonomous selection, explanation quality, movement/interaction readability, logistics, need interruption, blockage recovery, save/load continuation, and deferred M033/M034 limits. Source inspection must not be required.

## Forbidden authority

No native handles, process IDs, absolute paths, pathfinder internal heap objects, renderer pointers, raw key events, wall-clock timing, screenshot-only truth, or asset-home references.

## Performance baseline

Advisory same-machine counts for workers/targets/opportunities/evaluations/path searches/nodes/replans/movement/transitions/conflicts/render items/save-load/allocations. Semantic validation remains authoritative.
