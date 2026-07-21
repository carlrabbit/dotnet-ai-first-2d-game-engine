# Simulation Foundation Artifact Contract

## Authority

This document is authoritative for M031 structural evidence.

Artifacts describe semantic state and validation. They must not expose internal collection layout as project truth.

## Root

```text
artifacts/simulation/M031/
```

## Required artifact set

```text
foundation-manifest.json
world-before.json
world-after.json
regions.json
entities.json
activities.json
reservations.json
command-results.jsonl
domain-events.jsonl
persistence-report.json
fingerprints.json
invariants.json
diagnostics.json
performance-baseline.json
summary.md
wood-workflow/
  direct/
  roundtrip/
  comparison.json
review-pack/
  review-manifest.json
  architecture-summary.md
  evidence-index.json
```

## Schemas

Required schema families:

```text
agentic2d.simulation-foundation-manifest.v1
agentic2d.simulation-world-inspection.v1
agentic2d.simulation-region-inspection.v1
agentic2d.simulation-entity-inspection.v1
agentic2d.simulation-activity-inspection.v1
agentic2d.simulation-reservation-inspection.v1
agentic2d.simulation-command-result.v1
agentic2d.simulation-domain-event.v1
agentic2d.simulation-persistence-report.v1
agentic2d.simulation-fingerprint-comparison.v1
agentic2d.simulation-invariant-report.v1
agentic2d.simulation-diagnostics.v1
agentic2d.simulation-performance-baseline.v1
agentic2d.simulation-foundation-review-pack.v1
```

Final names may follow an already-established repository schema naming convention, but must be stable and documented.

## Foundation manifest

Contains:

- schema;
- milestone;
- scenario IDs;
- world ID;
- implementation capability/version;
- simulation-time resolution;
- registration fingerprint;
- command/event/activity/reservation schema versions;
- artifact references;
- aggregate status;
- diagnostics summary.

It references rather than duplicates detailed outputs.

## World inspection

Must show:

- world ID;
- simulation instant;
- deterministic sequence state;
- regions;
- world-scoped entity count;
- region-owned entity count;
- component registrations and persistence classifications;
- activity/reservation summaries;
- canonical fingerprint;
- invariant status.

## Entity inspection

For each bounded proof entity:

- stable entity ID;
- lifecycle;
- region or world scope;
- component keys;
- authoritative semantic component projection;
- persistence classification;
- activity/reservation references.

Do not include process memory addresses, CLR internal names as durable keys, native handles, or graphical adapter state.

## Activity inspection

For each activity:

- ID;
- actor;
- kind;
- stage;
- status;
- progress;
- revision;
- targets;
- start/transition instants;
- active reservations;
- correlation/causation;
- completion/interruption/failure reason.

## Reservation inspection

For each reservation:

- ID;
- activity;
- reserving entity;
- subject;
- kind;
- quantity/capacity;
- status;
- revision/guard;
- acquisition instant;
- release reason.

## Command results and domain events

Use JSON Lines with deterministic sequence.

Command result includes:

- command identity/type;
- issued and completed simulation instants;
- status;
- expected/current revision where relevant;
- diagnostic codes;
- emitted event IDs;
- causal references.

Domain event includes:

- event identity/type;
- simulation instant;
- deterministic sequence;
- affected stable IDs;
- typed fact payload;
- correlation/causation.

A failed command must not reference a factual success event.

## Persistence report

Contains:

- save schema/version;
- save path reference relative to artifact root where possible;
- atomic-write status;
- bytes;
- authoritative family counts;
- omitted state by persistence classification;
- validation status;
- load status;
- fresh-process proof;
- before/after fingerprints;
- diagnostics.

Do not record machine-local absolute paths as simulation authority. Environment diagnostics may contain sanitized operational paths only where repository conventions permit them.

## Fingerprint comparison

`fingerprints.json` and wood-workflow comparison evidence contain:

- direct branch fingerprints;
- round-trip branch fingerprints;
- registration fingerprint;
- authored input fingerprint;
- comparison status;
- semantic differences if unequal;
- excluded non-authoritative fields.

## Invariant report

At minimum:

- unique stable identities;
- exactly one region per region-owned entity;
- referential integrity;
- valid lifecycle;
- valid activity stage/revision;
- reservation quantity/capacity bounds;
- no completed activity with active leaked reservation;
- resource conservation;
- canonical ordering;
- persistence classification completeness.

Partial invariant output is not aggregate success.

## Diagnostics

Stable code, severity, message, related IDs, expected/actual values, and artifact pointer.

Diagnostics must be useful without raw logs.

## Performance baseline

Advisory same-machine evidence:

- entity/component/region counts;
- query count;
- command/event count;
- activity/reservation count;
- save/load bytes;
- elapsed durations;
- allocation values where existing infrastructure supports them;
- machine/runtime metadata required by current performance policy.

Timing does not override semantic validation.

## Review pack

The review pack must bound human attention to:

1. architecture/dependency summary;
2. public identity/time/activity/reservation concepts;
3. direct versus save/load equivalence;
4. conservation and invariant summary;
5. representative command/event trace;
6. failure diagnostics;
7. persistence classification;
8. deferred boundaries for detailed and abstract execution.

It must not require the reviewer to inspect raw source code to understand the evidence.

## Validation

Artifact validators fail on:

- missing required file;
- invalid schema;
- broken reference;
- non-deterministic ordering;
- forbidden absolute/native authority;
- mismatched fingerprint;
- incomplete invariant output;
- failed conservation;
- command/event causal inconsistency;
- leaked reservation;
- aggregate status inferred from partial evidence.
