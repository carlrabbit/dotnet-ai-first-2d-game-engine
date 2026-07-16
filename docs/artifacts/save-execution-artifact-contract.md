# Save Execution Artifact Contract

## Authority

Authoritative for save creation, manifest, snapshot, contributor, validation, load-plan, equivalence, diagnostics, and resume evidence.

Required files:

```text
save-result.json
save-manifest.json
save-snapshot.json
save-contributors.json
save-validation.json
save-load-plan.json
save-equivalence.json
save-diagnostics.json
```

Evidence reconstructs source run/tick → contributor capture → canonical snapshot → compatibility validation → load plan → reconstructed runtime → equivalence comparison. Machine paths and wall-clock values do not affect semantic fingerprints.
