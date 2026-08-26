# M044 Application Instructions

This package is a planning/design overlay. It contains no implementation.

M044 is sequentially dependent on M043. Apply this package only after the M043 planning overlay has been applied. Implementation MUST NOT begin until M043 is `COMPLETE`.

Apply the ZIP at repository root.

## Index merge

Do not replace current `docs/DECISIONS.md` or `docs/SPECS.md`.

Mechanically merge these rows into the live files if not already present.

### `docs/DECISIONS.md`

```text
| `docs/decisions/ADR-0056-resume-equivalence-requires-process-separated-continuation.md` | Accepted for M044 | Canonical resume/recovery proof uses distinct producer/consumer processes, exact continuation identity, and independently derived equivalence. |
```

### `docs/SPECS.md`

```text
| `docs/specs/canonical-save-resume-equivalence-contract.md` | Process-separated canonical save/resume, event/sequence identity continuity, product Continue, and recovery continuation equivalence. |
```

Index the M044 scenario document only if the live repository's scenario documentation index includes narrative scenario contracts.

Do not rewrite M020, M035, M037, M040, M041 or M042 milestone/review history.

Update current documentation only where M044's implemented process-separated continuation/recovery proof changes current truth. Broad documentation synchronization remains separate.
