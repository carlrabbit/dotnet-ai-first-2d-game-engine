# Asset Perception Contract

## Authority

Authoritative for deterministic local asset perception. Not a general image-recognition or hosted-AI contract.

## Purpose

```text
asset metadata + raw PNG → deterministic observations → optional proposals → review evidence
```

Output is generated evidence, never approved truth.

## Command

```text
agentic2d asset perceive <asset-id-or-path> --output <directory>
```

Initial target: `asset.tile-atlas-smoke`.

## Required observations

For each tile:

- stable tile ID and coordinate;
- width/height;
- alpha coverage or transparency ratio;
- occupied pixel bounds or empty status;
- deterministic representative color data;
- pixel-content SHA-256 fingerprint;
- exact duplicate-group ID when applicable.

## Optional proposals

Every proposal must include stable ID, tile ID, kind, value, state `proposed`, origin, finite score from 0 through 1, and evidence references. A bounded vocabulary is required. No proposal is required when evidence is insufficient. Proposals cannot populate approved gameplay/physical fields automatically.

## Diagnostics

| ID | Meaning |
|---|---|
| `PERCEPTION0001` | Asset/source not found. |
| `PERCEPTION0002` | Unsupported media type or kind. |
| `PERCEPTION0003` | Decode/extraction failure. |
| `PERCEPTION0004` | Tile outside source bounds. |
| `PERCEPTION0005` | Invalid feature value. |
| `PERCEPTION0006` | Invalid proposal. |

## Determinism

Equivalent metadata and pixels produce equivalent observations, fingerprints, duplicate groups, proposals, order, and diagnostics. No randomness, network, wall-clock decisions, machine identity, or nondeterministic parallel ordering.
