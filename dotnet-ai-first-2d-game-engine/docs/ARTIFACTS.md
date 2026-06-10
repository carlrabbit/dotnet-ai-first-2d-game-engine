# Artifacts

## Authority

This document is authoritative for generated evidence and report concepts until more specific artifact contracts exist.

## Artifact principle

A failed scenario, content validation, asset import, preview generation, or packaged-runtime validation must produce enough evidence for an agent or human to diagnose the failure without guessing.

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

## Generated/source rule

Generated artifacts are not source truth unless a specific document declares them committed baselines.
