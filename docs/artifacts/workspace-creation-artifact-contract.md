# Workspace Creation Artifact Contract

## Authority

Authoritative for structured evidence emitted by workspace creation and acquisition.

## Required files

```text
workspace-create-result.json
engine-acquisition.json
workspace-validation.json
workspace-create-diagnostics.json
```

## Acquisition evidence

Include provider, placement, source descriptor, requested revision, resolved commit/fingerprint, resolved engine path reference, file count, copy-policy ID, exclusions, provenance, and diagnostics.

Absolute paths may appear as diagnostics but do not affect semantic identity.

## Creation result

Include target status, template ID, staging/finalization status, generated manifest fingerprints, generated file summary, validation result, cleanup status, and referenced evidence.

## Failure

Failure evidence must state whether target/staging cleanup succeeded and must not claim a valid workspace when final validation did not pass.
