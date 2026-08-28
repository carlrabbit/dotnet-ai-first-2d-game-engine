# M047 Application Instructions

Apply this planning overlay after M046 is COMPLETE/current. Do not begin implementation until `pwsh ./eng/suite.ps1 m046-smoke --verify` passes.

Do not replace live index files wholesale. Preserve all existing rows and apply these merge-safe edits.

## `docs/DECISIONS.md`

Add:

```text
| `docs/decisions/ADR-0059-promotion-authority-is-exact-candidate-bound-and-generation-published.md` | Accepted for M047 | Promotion binds exact canonical candidate/variant decisions to deterministic recipes and publishes only independently validated immutable generations through atomic current authority. |
```

Do not change historical ADR-0040/ADR-0041 status solely for M047.

## `docs/SPECS.md`

Add:

```text
| `docs/specs/canonical-asset-candidate-contract.md` | Exact game-local candidate/source/selection/variant identity and fingerprint bridging discovery/campaign authority to v2 decisions and promotion. |
```

Update the existing rows to describe current authority:

```text
| `docs/specs/asset-workbench-decision-and-consequence-contract.md` | Exact v2 candidate/variant-bound asset decisions, typed corrections, history, groups, consequences, and candidate-scoped staleness. |
| `docs/specs/approved-asset-and-deterministic-promotion-contract.md` | V2 approved assets, deterministic recipes/provenance, immutable promoted generations, atomic publication, and independent validation; real affected rebuild deferred to M049. |
```

## `docs/TERMINOLOGY.md`

Preserve all terms and add:

```text
| Canonical asset candidate | Game-local structured promotion subject that binds a stable candidate ID to exact source-relative bytes/hash, typed media selection, promotion-relevant proposal data, variants, and a deterministic fingerprint. |
| Promoted asset generation | Immutable fully validated project-local set of approved definitions, derivatives, recipes, provenance, and hashes; one atomic current-generation record selects the current generation. |
```

## Historical authority

Do not edit the completed M029 milestone or its completed `.review/records/` entry. V1 decision/promoted data may remain readable historical evidence but is not current M047 promotion authority.

## Deferred synchronization

Do not require the M047 implementation agent to read `.guide-sync/`. The included pending hint is for a later documentation-synchronization pass.
