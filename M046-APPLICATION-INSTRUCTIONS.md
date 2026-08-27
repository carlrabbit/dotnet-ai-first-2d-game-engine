# M046 Application Instructions

M046 depends on M045. Apply this planning overlay after the M045 overlay; do not begin implementation until M045 is COMPLETE.

Do not replace live index files wholesale.

## `docs/DECISIONS.md`

Preserve all rows and add:

```text
| `docs/decisions/ADR-0058-continuous-kinematic-resolution-classifies-safe-displacement-truthfully.md` | Accepted for M046 | Continuous kinematic resolution validates finite input, tracks actual limiting geometry, and classifies every nonzero safe displacement as accepted/slid/clipped rather than blocked. |
```

## `docs/SPECS.md`

Preserve all rows and add:

```text
| `docs/specs/continuous-kinematic-spatial-correctness-contract.md` | Deterministic continuous AABB movement, finite input, truthful outcome classification, constraint-source evidence, and M045 mutation linkage. |
```

Index the M046 scenario contract only if the live repository maintains a scenario-document index. Historical M013 milestone/review records remain immutable.
