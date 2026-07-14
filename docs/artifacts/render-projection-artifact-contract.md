# Render Projection Artifact Contract

## Authority

Authoritative for headless render-projection and explicit screenshot evidence.

## Required files

```text
render-result.json
render-snapshot.json
render-frame.json
render-items.jsonl
render-commands.jsonl
asset-bindings.json
render-diagnostics.json
```

Explicit capture additionally produces `frame.png` and `frame-metadata.json`.

`render-result.json` contains status, source mode, scenario/snapshot ID, map ID, tick, references, fingerprints, diagnostics, and exit semantics.

`render-snapshot.json` contains stable render-relevant authored/runtime state sufficient to reconstruct projection.

`render-frame.json` contains source mode, scenario/snapshot ID, map/version, tick, viewport, camera, clear color, fingerprints, counts, and diagnostics.

`render-items.jsonl` contains one semantic item per record with complete source/visual/asset linkage and ordering fields.

`render-commands.jsonl` contains one backend-neutral command per record with item linkage and stable draw data.

`asset-bindings.json` contains stable asset-to-source and region bindings, source fingerprint, filtering expectation, and fallback status.

Screenshot metadata contains capture sequence, explicit capture source, scenario/snapshot ID, tick, projection fingerprint, viewport, path, and diagnostics.

Structural artifacts exclude volatile process, duration, absolute path, GPU, and window-system data. PNG pixels are review evidence and may vary.

Review packs distinguish semantic structural evidence from visual review evidence.
