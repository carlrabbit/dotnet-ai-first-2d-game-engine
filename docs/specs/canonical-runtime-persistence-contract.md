# Canonical Runtime Persistence Contract

## Authority

Authoritative for the current durable gameplay save envelope and its relationship to the actual simulation runtime.

## Current durable shape

```text
agentic2d.game-save.v1
    └── agentic2d.simulation-world-save.v2
```

The outer game-save envelope owns durable file identity, compatibility, integrity and recovery. `SimulationWorld` owns authoritative world semantics and its canonical payload. There is no second persistence-only gameplay world.

## Required envelope identity

A canonical game save records stable SaveId, ProjectId, WorldId, applicable world-configuration identity/fingerprint, semantic content fingerprint, embedded world schema/version, component registration fingerprint, payload fingerprint/checksum, and canonical save fingerprint.

Wall-clock/catalog metadata and machine paths are non-semantic.

## Semantic content fingerprint

The fingerprint is computed from an ordinal canonical set of resolved semantic identity entries:

```text
kind
stableId
schema/version
semanticFingerprint
```

Entries cover actual authored gameplay inputs that can affect reconstruction or future authoritative behavior. Presentation-only inputs are excluded.

A hard-coded milestone marker, repository revision label or unchecked caller constant is invalid evidence of content compatibility.

## World capture

Canonical world payload is produced from the actual `SimulationWorld`. Typed component authority remains in `EntityComponentWorld`.

Persistence classifications are executable:

- `AuthoritativePersistent`: persist and restore;
- `DerivedRebuildable`: omit and rebuild;
- `ActiveModeTransient`: omit from ordinary stable gameplay save;
- `PresentationOnly`: never gameplay-save authority;
- `ExternalHandle`: never persisted.

## Load publication boundary

```text
parse
→ envelope validation
→ integrity validation
→ project/world/config/content validation
→ world schema + registration/reference validation
→ fresh SimulationWorld load
→ derived reconstruction
→ reconstructed invariant validation
→ publish
```

Failure before publication leaves the caller's existing world/session unchanged.

## Sequence continuity

All authoritative sequence/epoch state needed to allocate future deterministic command/event identities is durable. A resumed world must not reuse an identifier already allocated before the save.

## Durable file write

Canonical save writes use validated temporary output and atomic replacement. A previous-good candidate is preserved according to recovery policy before replacement.

Recovery validates the previous-good candidate before restoring it and validates the restored temporary output before atomic replacement.

## Compatibility

Current supported durable format:

```text
agentic2d.game-save.v1
agentic2d.simulation-world-save.v2
```

Historical M020 `agentic2d.canonical-save.v1`, M035 `agentic2d.m035.save-envelope.v1`, and SimulationWorld v1 are unsupported. No migration is implied.

## Catalog boundary

M037 `SaveCatalog` owns player-facing metadata, ordering, titles, manual/autosave type and retention. It references canonical saves; it does not serialize world authority.

## Evidence

Validation must derive compatibility, round-trip equality, classification behavior, atomicity and recovery from observed runtime/files. Presence of a JSON artifact or producer-authored success boolean is not proof.
