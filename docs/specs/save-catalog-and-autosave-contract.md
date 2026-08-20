# Save Catalog and Autosave Contract

## Authority

Authoritative for M037 save metadata/catalog, naming, Continue resolution, browser operations, wall-clock autosave requests, and retention. M035 remains authoritative for world serialization, compatibility, validation, migration, atomic replacement, and recovery.

## Save record

Required metadata: SaveId, internal provenance WorldId, world title, save title, seed, world-config identity/schema/fingerprint, simulation instant/day, population, created-at, saved-at, type manual/autosave, game/build version, save schema, compatibility/recovery status.

A save is the player-visible unit and complete world snapshot. WorldId is provenance only.

## Naming

Manual default:

```text
<World Title> — Day <Simulation Day>
```

Editable before commit and afterward through rename.

Autosave title:

```text
Autosave — <World Title> — Day <Simulation Day>
```

Generated and non-editable through normal save naming. Titles never alter provenance.

## Catalog

Operations: list, resolve Continue, manual save, overwrite with confirmation, rename title, delete one record, inspect compatibility/recovery, load through canonical persistence. No visible world hierarchy and no delete-world operation.

## Continue ordering

Order by canonical successful saved-at descending, then stable SaveId tie-break. Invalid/incompatible candidates are skipped with player-visible diagnostics.

## Autosave schedule

Use injected monotonic application time. State includes enabled, interval, retention count, elapsed active time, pending request, save-active marker, last success, last failure.

Elapsed time advances while a world is loaded and application is active; simulation pause does not stop it. A due request waits for a valid save boundary.

Default retention is five autosaves per WorldId. Initial UI values: 1, 3, 5, 10. Rotation/cleanup is deterministic and never deletes manual saves.

Failed autosave preserves prior valid saves and exposes status/diagnostics.

## Cross-platform

Catalog/save semantics are platform-neutral. Durable metadata does not encode machine absolute paths or platform-specific separators.
