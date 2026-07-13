# Entity Instantiation, Query, Trigger, and Interaction Artifact Contract

## Authority

This document is authoritative for Milestone 014 evidence produced by entity-definition resolution, runtime instantiation, spatial queries, trigger transitions, and interaction resolution.

## Required files

Runtime inspection for Milestone 014 scenarios must produce:

```text
entity-instantiations.jsonl
spatial-queries.jsonl
trigger-transitions.jsonl
interaction-resolutions.jsonl
```

Existing entity registry, components, component mutations, static world, events, assertions, diagnostics, and manifest artifacts remain required.

## `entity-instantiations.jsonl`

One record per attempted spawn.

Required fields:

- schema/version;
- sequence;
- tick/load phase;
- definition ID;
- spawn ID;
- entity ID;
- source kind and source ID;
- definition defaults;
- map-spawn overrides;
- scenario overrides;
- final merged bundle;
- validation status;
- generated commands in order;
- command results;
- behavior assignment;
- provenance;
- events;
- diagnostics;
- committed/rolled-back status.

Rejected instantiation must not claim committed state.

## `spatial-queries.jsonl`

One record per inspected spatial query.

Required fields:

- sequence;
- tick;
- query ID;
- query kind;
- world/spatial membership;
- input shape/center/radius;
- excluded entity ID;
- filters;
- unfiltered candidates;
- candidate distances when applicable;
- filter results;
- final ordered results;
- diagnostics.

## `trigger-transitions.jsonl`

One stable record representation per trigger evaluation or transition.

Required fields:

- sequence;
- tick;
- trigger ID;
- trigger owner entity ID;
- trigger bounds;
- filter;
- previous overlaps;
- current overlaps;
- entered IDs;
- exited IDs;
- event IDs;
- diagnostics.

No `stayed` field is required.

## `interaction-resolutions.jsonl`

One record per `InteractIntent`.

Required fields:

- sequence;
- tick;
- intent ID;
- behavior assignment ID;
- interactor entity ID;
- explicit target ID;
- requested kind;
- query ID;
- candidates;
- distances;
- eligibility and rejection reasons;
- selected target ID;
- selection reason;
- accepted/rejected status;
- command reference;
- interaction kind;
- events;
- rejection reason;
- diagnostics.

## Linkage

Stable IDs must link:

```text
definition
→ spawn
→ entity
→ query
→ trigger transition
→ interaction intent
→ resolution
→ command
→ event
```

## Determinism

Exclude wall-clock timestamps, durations, process IDs, object identities, and absolute machine paths.

Candidate and result arrays use semantic ordering from the applicable contract.

## Failure evidence

Failures must still produce safe partial evidence and structured diagnostics when the artifact boundary is reached.

## Review-pack integration

The runtime-inspection manifest must make all four files discoverable.

A reviewer must be able to explain definition/spawn origin, overrides, atomicity, query ordering, trigger transition, target selection, command, and event.
