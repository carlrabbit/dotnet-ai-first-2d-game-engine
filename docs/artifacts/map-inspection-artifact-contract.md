# Map Inspection Artifact Contract

## Authority

Authoritative for `agentic2d map inspect` artifacts.

## Required files

```text
<output>/result.json
<output>/diagnostics.json
<output>/map-summary.json
<output>/layers.json
<output>/resolved-references.json
```

Schemas use `agentic2d.map-inspection.*.v1`.

`result.json` includes command, target, map ID, status, exit code, counts, diagnostics, and artifact references. Summary includes identity/path/dimensions/tile size/counts/assets. Layers include ordered cells. Resolved references identify asset/tile status and source location such as layer/cell. Failures emit every artifact that can be produced safely.

## Determinism

Layer order follows declared meaningful source order or stable ID. Cells sort by layer ID, y, x, asset ID, and tile ID.
