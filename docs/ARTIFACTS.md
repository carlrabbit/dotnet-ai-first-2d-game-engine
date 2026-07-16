# Artifacts

## Authority

This document indexes generated evidence and artifact contracts.

## Principle

Failures must produce enough structured evidence for diagnosis. Generated artifacts are not source truth unless explicitly declared as committed baselines.

## Current artifact contracts

| Document | Authority area |
|---|---|
| `docs/artifacts/runtime-result-contract.md` | Minimal runtime result. |
| `docs/artifacts/product-cli-result-contract.md` | Product CLI result. |
| `docs/artifacts/scenario-runner-artifact-contract.md` | Scenario result, events, diagnostics. |
| `docs/artifacts/content-validation-artifact-contract.md` | Content validation evidence. |
| `docs/artifacts/workspace-creation-artifact-contract.md` | Workspace acquisition and creation evidence. |
| `docs/artifacts/unified-run-artifact-contract.md` | Central unified run manifest and linked evidence. |
| `docs/artifacts/sound-execution-artifact-contract.md` | Deterministic sound command evidence. |
| `docs/artifacts/gameplay-state-artifact-contract.md` | Gameplay resource, lifecycle, and collection evidence. |
| `docs/artifacts/asset-inspection-artifact-contract.md` | Asset inspection. |
| `docs/artifacts/review-pack-artifact-contract.md` | Review-pack aggregation. |
| `docs/artifacts/asset-curation-workbench-artifact-contract.md` | Static curation workbench. |
| `docs/artifacts/asset-authoring-artifact-contract.md` | Review apply and perception. |
| `docs/artifacts/map-inspection-artifact-contract.md` | Map inspection. |
| `docs/artifacts/runtime-inspection-artifact-contract.md` | Runtime inspection. |
| `docs/artifacts/behavior-spatial-execution-artifact-contract.md` | Behavior intents and spatial resolutions. |
| `docs/artifacts/entity-component-continuous-spatial-artifact-contract.md` | Entity/component and continuous movement evidence. |
| `docs/artifacts/entity-instantiation-query-trigger-interaction-artifact-contract.md` | Instantiation, queries, triggers, and interactions. |
| `docs/artifacts/render-projection-artifact-contract.md` | Render projection and explicit capture evidence. |
| `docs/artifacts/input-execution-and-replay-artifact-contract.md` | Input map, frame, recording, and replay evidence. |
| `docs/artifacts/animation-execution-artifact-contract.md` | Animation compilation, samples, markers, and animated rendering. |
| `docs/artifacts/sound-execution-artifact-contract.md` | Sound projection evidence. |
| `docs/artifacts/gameplay-state-artifact-contract.md` | Gameplay state and transaction evidence. |

## Artifact roots

```text
artifacts/
game/artifacts/
game/assets/generated/
```

## Current rendering evidence

Headless render projection produces:

```text
render-result.json
render-snapshot.json
render-frame.json
render-items.jsonl
render-commands.jsonl
asset-bindings.json
render-diagnostics.json
```

Explicit screenshot capture additionally produces:

```text
frame.png
frame-metadata.json
```

Structural JSON artifacts are deterministic semantic evidence. PNGs are explicit human-review evidence and are not required to match pixel-for-pixel across platforms.

Review-pack manifests must make current artifact families discoverable.
