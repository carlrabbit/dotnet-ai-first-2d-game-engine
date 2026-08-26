# M043 Application Instructions

This package is a planning/design overlay. It contains no implementation.

It was prepared against `main` at:

```text
97e9bb3745aec27a65fcccc4d4d97ee4f5dccc31
```

Apply the ZIP at repository root.

## Index merge

Do not replace current `docs/DECISIONS.md` or `docs/SPECS.md` with a stale snapshot.

Mechanically merge these rows into the live files if not already present.

### `docs/DECISIONS.md`

```text
| `docs/decisions/ADR-0055-one-canonical-game-save-wraps-simulation-world-v2.md` | Accepted for M043 | One current durable game-save envelope wraps the actual SimulationWorld v2 payload; historical M020/M035 persistence formats are retired. |
```

### `docs/SPECS.md`

```text
| `docs/specs/canonical-runtime-persistence-contract.md` | Current durable game-save envelope, real semantic-content compatibility, actual SimulationWorld payload, atomic write/recovery, and catalog boundary. |
```

Add the architecture document to an architecture index only if the live repository maintains one.

Do not rewrite historical M020/M035 milestone documents or completed review records.

During implementation update current documentation only where the live implementation would otherwise contradict the new canonical persistence architecture. Broad documentation synchronization remains separate.
