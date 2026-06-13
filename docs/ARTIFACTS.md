# Artifacts

## Authority

This document indexes generated evidence and report contracts.

## Artifact principle

A failed scenario, content validation, asset import, preview generation, product CLI command, or packaged-runtime validation must produce enough evidence for an agent or human to diagnose the failure without guessing.

## Current artifact documents

| Document | Authority area |
|---|---|
| `docs/artifacts/report-contract.md` | General minimum report concept. |
| `docs/artifacts/generated-artifacts.md` | Generated file handling rules. |
| `docs/artifacts/runtime-result-contract.md` | `result.json` artifacts produced by Milestone 002 runtime smoke execution. |
| `docs/artifacts/product-cli-result-contract.md` | `result.json` artifacts produced by Milestone 003 product CLI commands. |

## Artifact roots

```text
artifacts/
game/artifacts/
game/assets/generated/
```

## Typical artifacts

```text
result.json
diagnostics.json
events.jsonl
scene-dump.json
map-dump.json
ui-dump.json
screenshot.png
preview.png
collision-overlay.png
navigation-overlay.png
semantic-overlay.png
metrics.json
review-summary.md
```

## Current product CLI artifacts

Current artifact-producing product CLI commands write:

```text
<output>/result.json
```

Current contracts:

```text
docs/artifacts/runtime-result-contract.md
docs/artifacts/product-cli-result-contract.md
```

## Generated/source rule

Generated artifacts are not source truth unless a specific document declares them committed baselines.
