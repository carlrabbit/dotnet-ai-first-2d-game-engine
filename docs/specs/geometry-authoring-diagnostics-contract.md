# Geometry Authoring Diagnostics Contract

## Authority

Authoritative for geometry inspection, preview artifacts, bounded contrast diagnostics, structural-versus-graphical projection evidence, and diagnostic severity.

## Required outputs

```text
geometry-inspection.json
geometry-preview.json
geometry-diagnostics.json
geometry-projection-comparison.json
```

Inspection records definition/part IDs, kind, normalized dimensions, anchor/offset, rotation, fill/outline/opacity, layer/order/sort, preview bounds, provenance, and fingerprints.

Headless preview is mandatory. Graphical capture is adapter-owned and graphics-environment conditional. Comparison relates normalized commands to capture metadata without making pixels simulation authority.

Contrast checks use a declared preview background, emit deterministic bounded diagnostics, and do not claim accessibility, aesthetic, or readability certification.

Invalid diagnostics identify definition, part, field, value, and accepted range/remediation. No automatic repair or layout mutation is permitted.
