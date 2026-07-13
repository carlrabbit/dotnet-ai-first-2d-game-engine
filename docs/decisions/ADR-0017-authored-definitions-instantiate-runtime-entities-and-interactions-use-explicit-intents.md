# ADR-0017 — Authored Definitions Instantiate Runtime Entities; Interactions Use Explicit Intents

## Status

Proposed for Milestone 014. Accept when implementation is accepted.

## Context

Milestone 013 established runtime-owned entities and typed components. Reusable game content needs a stable authored representation, and the engine needs spatial queries, trigger transitions, and interaction selection without an editor-centric prefab system or downstream gameplay domains.

## Decision

Use authored entity definitions as complete reusable component defaults.

Use separate stable identities for definition, spawn, and runtime entity.

Maps and scenarios share one spawn contract.

Overrides replace whole components with precedence:

```text
definition
→ map spawn
→ scenario
```

Instantiation is validated and transactional, generates normal runtime commands, and records immutable provenance.

Static map objects remain static content. Interactive objects are explicit entity spawns.

Spatial queries are read-only deterministic scans for entity lookup, AABB overlap, and radius/proximity.

Triggers are entity-owned non-solid AABBs. Runtime records prior overlap state and emits only entered/exited transitions.

Interactions require explicit behavior intents. Resolution uses radius eligibility, explicit target preference, nearest distance, and entity-ID tie-break. Accepted interaction emits `interaction.started` and stops there.

## Consequences

Positive:

- authored content avoids repeated low-level scenario bundles;
- origin and override provenance remain inspectable;
- definitions do not become mutable runtime objects;
- maps distinguish static geometry from interactive entities;
- queries, triggers, and target selection remain deterministic;
- downstream domains can later consume a stable event.

Costs:

- definition/spawn schemas and compatibility validation must be maintained;
- transactional evidence is required;
- trigger overlap state becomes runtime state;
- interaction selection requires candidate evidence;
- no inheritance means some duplication.

## Rejected alternatives

- definition inheritance;
- arbitrary JSON Patch overrides;
- automatic interaction on overlap;
- static interactive map objects;
- physical entity blocking;
- dialogue implementation in this milestone.
