# Standalone Game Export Artifact Contract

## Authority

Authoritative for export assembly, validation, isolated launch, development/export equivalence, diagnostics, and performance evidence.

## Export artifacts

```text
export-result.json
export-manifest.json
export-files.json
export-validation.json
export-diagnostics.json
```

## Isolated-launch artifacts

```text
isolated-launch-result.json
isolated-launch-process.json
isolated-launch-run-manifest.json
isolated-launch-diagnostics.json
```

## Equivalence artifacts

```text
development-export-equivalence.json
development-export-differences.json
development-export-diagnostics.json
```

## Required evidence

Evidence links:

```text
source project/content fingerprint
→ publish configuration
→ export file inventory
→ isolated direct launch
→ exported run manifest
→ development representation
→ strict semantic comparison
→ allowed representation differences
→ performance report
```

Machine-specific absolute paths and wall-clock timings are diagnostic and excluded from semantic export identity.
