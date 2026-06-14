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
| `docs/artifacts/scenario-runner-artifact-contract.md` | `result.json`, `events.jsonl`, and `diagnostics.json` artifacts produced by Milestone 005 scenario runner commands. |
| `docs/artifacts/content-validation-artifact-contract.md` | `result.json`, `diagnostics.json`, and `validated-items.json` artifacts produced by Milestone 006 content validation commands. |

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

Current scenario runner commands write:

```text
<output>/result.json
<output>/events.jsonl
<output>/diagnostics.json
```

Current scenario artifact contract:

```text
docs/artifacts/scenario-runner-artifact-contract.md
```

Current content validation commands write:

```text
<output>/result.json
<output>/diagnostics.json
<output>/validated-items.json
```

Current content validation artifact contract:

```text
docs/artifacts/content-validation-artifact-contract.md
```

## Generated/source rule

Generated artifacts are not source truth unless a specific document declares them committed baselines.
