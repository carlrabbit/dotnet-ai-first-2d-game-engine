# ADR-0055 — One Canonical Game Save Wraps SimulationWorld v2

## Status

Accepted for M043.

## Context

The repository accumulated overlapping persistence implementations: M020's `PersistentWorldRuntime` and `agentic2d.canonical-save.v1`; M035's test-oriented save envelope/recovery helper; and the current `SimulationWorld v2` persistence hardened by M039–M042. M037 also introduced player-facing save catalog metadata that expects a canonical persistence backend.

## Decision

The current durable gameplay save is:

```text
agentic2d.game-save.v1
    containing
agentic2d.simulation-world-save.v2
```

The actual `SimulationWorld` is the only authoritative gameplay world payload.

The outer persistence service owns durable envelope identity, actual semantic-content compatibility, integrity, file atomicity, inspection/comparison and recovery.

`EntityComponentWorld` remains the typed component authority through `SimulationWorld`; persistence classification remains defined at current simulation component registration.

M037 `SaveCatalog` remains a metadata/catalog layer referencing canonical saves.

M020's persistence-only runtime and M035's test envelope are not retained as current architecture.

## Compatibility

No compatibility is promised for these unreleased historical formats:

```text
agentic2d.canonical-save.v1
agentic2d.m035.save-envelope.v1
agentic2d.simulation-world-save.v1
```

They are rejected explicitly; no migration shim is required.

## Consequences

- one current save path exists;
- persistence tests exercise the real world;
- compatibility derives from real semantic content rather than fixture constants;
- previous-good recovery remains available;
- old M020 fixtures/direct consumers must be migrated, demoted or removed;
- full separate-process resume/recovery equivalence is proved by M044 rather than M043.
