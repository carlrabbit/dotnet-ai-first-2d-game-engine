# Canonical Save Snapshot Contract

## Authority

Authoritative for save schema, identity, canonical records, ordering, fingerprints, persisted metadata, and excluded state.

A save is resumable authoritative state, not a diagnostic dump.

Required identity includes save schema/version, save ID, project/scenario/content identities and fingerprints, runtime tick, deterministic continuation, contributor set, and save fingerprint.

Canonical ordering is ordinal by stable IDs. Machine paths, wall-clock values, artifacts, diagnostics, caches, native resources, sound/render commands, and sampled presentation output are excluded.

Persistent absence of authored-spawned entities must be explicit through tombstones or equivalent authoritative spawn-state records.
