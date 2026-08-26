# M045 Application Instructions

Prepared against the audited `main` baseline:

```text
f7450e04fb288a55de1dcf5e2097d1778286f85d
```

Apply at repository root. Do not replace live index files wholesale.

## `docs/DECISIONS.md`

Preserve all rows and add:

```text
| `docs/decisions/ADR-0057-evaluation-reads-immutable-runtime-snapshots-and-mutation-commits-transactionally.md` | Accepted for M045 | Runtime evaluation reads immutable typed snapshots; lifecycle/component/provenance mutation commits through a staged runtime transaction. |
```

## `docs/SPECS.md`

Preserve all rows and add:

```text
| `docs/specs/runtime-snapshot-and-mutation-authority-contract.md` | Immutable typed runtime snapshots, generic component-binding rules, canonical fingerprints, evaluator command boundary, and atomic runtime mutation transactions. |
```

Add the architecture file to a live architecture index only if one exists. Historical M013 milestone/review records remain immutable.
