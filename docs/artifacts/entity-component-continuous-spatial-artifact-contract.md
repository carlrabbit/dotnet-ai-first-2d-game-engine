# Entity Component and Continuous Spatial Artifact Contract

## Authority

This document is authoritative for Milestone 013 entity/component and continuous-spatial execution evidence.

## Required runtime-inspection additions

For Milestone 013 scenarios, runtime inspection must produce:

```text
<output>/entity-registry.json
<output>/components.json
<output>/component-mutations.jsonl
<output>/static-spatial-world.json
<output>/continuous-resolutions.jsonl
```

Existing runtime-inspection artifacts remain required.

## `entity-registry.json`

Required semantic content:

- world/run identity;
- current entity IDs;
- created entity IDs;
- destroyed entity IDs;
- lifecycle event references;
- deterministic entity ordering.

## `components.json`

Required semantic content:

- registered component type IDs;
- component values grouped by entity or component type;
- source/ownership classification:
  - runtime/core;
  - spatial.grid;
  - spatial.continuous-kinematic-2d;
- deterministic ordering;
- snapshot fingerprint references.

## `component-mutations.jsonl`

Each record includes:

- tick;
- command ID;
- entity ID;
- component type ID;
- mutation kind;
- accepted/rejected status;
- previous value when review-relevant;
- resulting value when accepted;
- event and diagnostic references.

## `static-spatial-world.json`

Required semantic content:

- source map ID and path;
- map bounds;
- blocked cell geometry;
- static object geometry;
- `object.tree.large.smoke`;
- source references;
- deterministic geometry fingerprints.

## `continuous-resolutions.jsonl`

Each record includes:

- tick;
- intent ID;
- target entity ID;
- module ID;
- requested direction/displacement;
- initial transform and AABB;
- collision candidate references;
- X-axis requested/applied displacement;
- Y-axis requested/applied displacement;
- outcome:
  - accepted;
  - clipped;
  - slid;
  - blocked;
- resulting transform;
- mutation command reference;
- events and diagnostics.

## Numeric normalization

- finite invariant numbers only;
- serialize negative zero as zero;
- no locale-dependent decimal formatting;
- document any semantic comparison tolerance;
- exclude durations, timestamps, process IDs, and absolute paths from semantic comparison.

## Failure evidence

Validation or runtime failure must still emit:

- result;
- diagnostics;
- entity registry when available;
- safe partial component/static-world/resolution evidence.

## Review-pack integration

The review pack must make these questions answerable:

- Which entities existed?
- Which components did each entity have?
- Which state was static map geometry?
- Which state was mutable runtime entity state?
- What movement was requested?
- Which obstacle constrained it?
- What displacement was applied?
- What final transform resulted?
