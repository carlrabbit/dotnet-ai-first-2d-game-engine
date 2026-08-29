# Asset Candidate Preview and Curation Evidence Contract

## Authority

Authoritative for M048 machine evidence and human-review readiness artifacts.

Generated evidence is not candidate/decision/promotion authority.

## Root

```text
artifacts/assets/M048/
```

Validation receipts remain:

```text
artifacts/validation/m048-smoke/
```

## Required Structural Evidence

The implementation may choose concrete file names, but the M048 evidence set must expose canonical records equivalent to:

```text
preview-subject
preview-bundle inspection
image preview observation
animation preview observation
audio preview observation
draft/decision binding comparison
staleness/restart comparison
workbench/group guard result
review readiness
evidence-integrity comparison
```

## Preview Observation

For each modality, record observed:

```text
candidate ID/fingerprint
selected variant ID/fingerprint
canonical typed corrections
recipe fingerprint
materializationSubjectFingerprint
raw media fingerprint
processed media fingerprint
preview bundle fingerprint
preview-host acknowledged fingerprint
backend-neutral projection/command fingerprint
adapter execution status where applicable
diagnostics
```

The independent comparer derives equality/mismatch predicates.

## Decision Binding Evidence

Record enough raw inputs to independently derive:

```text
draft materialization subject
acknowledged preview materialization subject
committed v2 decision materialization subject
promotion-plan materialization subject
```

Acceptance requires exact equality in the positive case and rejection in deliberately mismatched/stale cases.

## Review Readiness

Machine readiness artifact identifies each canonical M048 review ID and proves:

- request exists and is current;
- experience provider is registered;
- deterministic fixture exists;
- fixture subject machine validation passed;
- graphical requirements are current;
- audio item declares actual audio-output requirement.

It must not claim the human liked, understood, saw correctly, or heard correctly.

## Human Evidence

Human evidence is the launchable:

```text
review-run --milestone M048
```

experience.

Screenshots may support diagnosis/capture provenance but are not substitutes for interactive visual/UX review and cannot establish audio quality.

## Integrity Rule

Producer-authored booleans are insufficient.

The verifier/comparer recomputes identity and hash relationships from observed files/content.

A stale prior artifact cannot satisfy a current fingerprinted validation receipt.
