# Asset Review Decision Contract

## Authority

Authoritative for authored asset review decisions and safe application.

## Purpose

```text
generated evidence → human-authored decision source → dry-run → fingerprint check → deterministic apply → content validation → mutation evidence
```

Generated workbench output is never decision source.

## Source format

Schema: `agentic2d.asset-review-decisions.v1`.

Required smoke path: `game/assets/reviews/tile-atlas-smoke.review.json`.

Minimum shape:

```json
{
  "schema": "agentic2d.asset-review-decisions.v1",
  "id": "review.asset.tile-atlas-smoke",
  "assetId": "asset.tile-atlas-smoke",
  "metadataPath": "game/assets/metadata/tile-atlas-smoke.asset.json",
  "expectedSourceFingerprint": "sha256:<lowercase-hex>",
  "provenance": {
    "sourceKind": "repository-fixture",
    "createdBy": "milestone-011",
    "reviewedBy": "human.smoke-fixture",
    "notes": "Synthetic capability-validation review evidence."
  },
  "decisions": [{
    "id": "decision.tile.smoke.0.walkable",
    "target": {"tileId": "tile.smoke.0", "field": "physicalBehaviorsApproved", "value": "walkable"},
    "state": "approved",
    "reason": "Synthetic smoke fixture decision."
  }]
}
```

## Rules

- IDs are stable and unique.
- Paths are repository-relative and safe.
- Asset ID matches target metadata.
- Expected fingerprint is calculated from current source bytes.
- Supported states: `approved`, `rejected`, `needs-revision`, `clear`.
- Approved physical/gameplay behavior requires review provenance.
- Rejected/needs-revision values are not written as approved behavior.
- Duplicate or contradictory decisions are invalid.
- Unknown targets, fields, tiles, or values fail validation.
- Fixture identity is synthetic test evidence, not production approval.

## Command

```text
agentic2d asset review apply --decisions <review-file> [--dry-run] --output <directory>
```

Dry-run validates and generates proposed output without source mutation.

Non-dry-run order:

1. validate decision source;
2. validate target metadata;
3. compare fingerprint;
4. calculate full update in memory;
5. validate update;
6. atomically replace source where practical;
7. emit evidence.

## Safety

Fingerprint mismatch is validation failure and cannot mutate source. Serialization/validation/write failures cannot leave partial source. Reapplication may be idempotent or a documented no-op. Unrelated metadata must be preserved.

## Diagnostics

| ID | Meaning |
|---|---|
| `REVIEW0001` | Invalid decision contract. |
| `REVIEW0002` | Target not found. |
| `REVIEW0003` | Source fingerprint mismatch. |
| `REVIEW0004` | Unknown tile, field, or value. |
| `REVIEW0005` | Contradictory or duplicate decision. |
| `REVIEW0006` | Missing review provenance. |
| `REVIEW0007` | Updated metadata failed validation. |
| `REVIEW0008` | Mutation write failed. |

## Determinism

The same decision and target source produce semantically equivalent evidence. Timestamps, if present, are volatile.
