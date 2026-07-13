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
| `docs/artifacts/asset-inspection-artifact-contract.md` | `result.json`, `diagnostics.json`, `asset-summary.json`, and `tiles.json` artifacts produced by Milestone 007 asset inspection commands. |
| `docs/artifacts/review-pack-artifact-contract.md` | `review-summary.md`, `review-manifest.json`, and `diagnostics.json` artifacts produced by Milestone 010 review pack commands. |
| `docs/artifacts/asset-curation-workbench-artifact-contract.md` | `index.html`, `review-data.json`, and `diagnostics.json` artifacts produced by Milestone 010 asset curation workbench commands. |
| `docs/artifacts/asset-authoring-artifact-contract.md` | Asset review apply and asset perception artifacts introduced by Milestone 011. |
| `docs/artifacts/map-inspection-artifact-contract.md` | Map inspection artifacts introduced by Milestone 011. |
| `docs/artifacts/runtime-inspection-artifact-contract.md` | Runtime inspection artifacts introduced by Milestone 011. |

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

Current asset inspection commands write:

```text
<output>/result.json
<output>/diagnostics.json
<output>/asset-summary.json
<output>/tiles.json
```

Current asset inspection artifact contract:

```text
docs/artifacts/asset-inspection-artifact-contract.md
```

Current review pack commands write:

```text
<output>/review-summary.md
<output>/review-manifest.json
<output>/diagnostics.json
```

Current review pack artifact contract:

```text
docs/artifacts/review-pack-artifact-contract.md
```

Current asset curation workbench commands write:

```text
<output>/index.html
<output>/review-data.json
<output>/diagnostics.json
```

Current asset curation workbench artifact contract:

```text
docs/artifacts/asset-curation-workbench-artifact-contract.md
```

Current Milestone 011 artifact-producing commands additionally write:

```text
asset review apply -> result.json, diagnostics.json, mutation-plan.json, validation-result.json, proposed-metadata.json (dry-run only)
asset perceive -> result.json, diagnostics.json, tile-features.json, semantic-proposals.json
map inspect -> result.json, diagnostics.json, map-summary.json, layers.json, resolved-references.json
runtime inspect -> result.json, diagnostics.json, runtime-summary.json, entities.json, commands.jsonl, events.jsonl, final-state.json, assertions.json, content-references.json
```

## Generated/source rule

Generated artifacts are not source truth unless a specific document declares them committed baselines.
## Behavior and spatial execution artifacts

Milestone 012 runtime inspection additionally emits `behaviors.json`, `intents.jsonl`, and `spatial-resolutions.jsonl` for behavior scenarios.
