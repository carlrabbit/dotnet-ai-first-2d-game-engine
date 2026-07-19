# Shared Asset Home and Source Registry Contract

## Authority

Authoritative for the machine-local asset home, source registration, source identity, local-path handling, fingerprinting, refresh, and cleanup.

## Role

The shared asset home is authoring infrastructure. It is not game truth, runtime authority, an export-time dependency, a source-controlled raw library, or a remote/multi-user database.

## Location

Linux default: `${XDG_DATA_HOME:-$HOME/.local/share}/agentic2d/assets`.

Override: `AGENTIC2D_ASSET_HOME`.

Absolute paths may appear in local operational records but are excluded from committed campaign semantics and semantic fingerprints.

## Initial storage model

Filesystem-only, one user and one machine:

```text
registry/
sources/
profiles/
annotations/
previews/
sessions/
cache/
```

SQLite, remote sync, authentication, and servers require later evidence.

## Source identity

A source has canonical ID, display name, kind, local path, availability, package fingerprint, ordered inventory fingerprint, current valid profile fingerprint, and diagnostics. Canonical identity does not derive solely from the current absolute path.

## Refresh

Refresh inventories source, calculates fingerprints, determines reusable/stale metadata, builds and validates replacement profile, then atomically updates the current-profile pointer. Failure preserves the prior valid pointer.

## Cleanup

Generated profiles, previews, sessions, and cache are disposable. Engine cleanup never removes raw source bytes or retained annotations. `--stale` removes only generated entries proven incompatible, unreachable, malformed, or obsolete.
