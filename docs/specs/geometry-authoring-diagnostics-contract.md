# Geometry Authoring Diagnostics Contract

## Authority

Authoritative for stable M027 geometry inspection, preview, diagnostics, projection comparison, capture metadata, and review packs.

## Stable schemas

```text
agentic2d.geometry-inspection.v1
agentic2d.geometry-preview.v1
agentic2d.geometry-diagnostics.v1
agentic2d.geometry-projection-comparison.v1
agentic2d.geometry-review-pack.v1
```

Writers emit these versions. Readers may support the prior M026 shape only through a bounded migration adapter.

## Determinism

Order by project, definition, part, diagnostic code, and source field path. Semantic fingerprints exclude timestamps, absolute temporary paths, graphics device identity, and non-semantic image encoding metadata.

## Diagnostic identity

```text
GEO001 unknown geometry kind
GEO002 invalid or non-finite dimension
GEO003 invalid opacity
GEO004 invalid outline or ring width
GEO005 invalid polygon or point set
GEO006 unresolved visual reference
GEO007 low foreground/background distinguishability
GEO008 structural/capture metadata mismatch
GEO009 unsupported capture environment
```

Each diagnostic includes code, severity, source path, field path, safe value, message, and remediation.

## Graphical boundary

Headless structural preview is mandatory. Capture is optional only when the owning milestone permits explicit absence. Pixels are human-review evidence, not simulation authority.

## Review pack

Include manifest, index, inspection, preview, diagnostics, comparison, capture status, representative images when available, and omissions.
