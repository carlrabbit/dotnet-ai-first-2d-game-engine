# Canonical Persistence Architecture

## Layering

```text
M037 player save catalog / UI metadata
            |
            | SaveId + file reference
            v
Agentic2D.Persistence
  canonical game-save envelope
  compatibility / integrity
  atomic write / recovery
  inspect / compare
            |
            v
Agentic2D.Simulation
  SimulationWorld v2
  semantic clock / sequence
  regions / entities
  typed components
  activities / reservations
  tombstones
            |
            v
Agentic2D.Engine
  EntityComponentWorld typed stores
```

## Ownership

`Agentic2D.Engine` owns typed entity/component storage and validated mutations.

`Agentic2D.Simulation` owns authoritative simulation semantics and canonical world payload, including persistence classifications.

`Agentic2D.Persistence` owns the durable file boundary around the world: outer schema/version, SaveId and compatibility identity, semantic content fingerprint, checksum/fingerprints, validated load orchestration, atomic replacement, previous-good recovery, inspection and comparison. It does not mirror gameplay state into another world model.

`Agentic2D.UI` / product composition owns player-facing save metadata/catalog operations and requests persistence work through the canonical persistence service.

## Dependency rule

Prefer:

```text
Persistence -> Simulation -> Engine
```

Avoid `Simulation -> Persistence`. Join catalog and persistence at an application/product composition boundary rather than introducing a reverse runtime dependency.

## Historical code

`PersistentWorldRuntime` and the M035 envelope may remain referenced by immutable historical documents, but not by current production persistence or product save flows after M043.
