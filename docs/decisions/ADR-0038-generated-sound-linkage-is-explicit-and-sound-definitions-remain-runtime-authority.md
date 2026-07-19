# ADR-0038 — Generated Sound Linkage Is Explicit and Sound Definitions Remain Runtime Authority

## Status

Proposed for M026. Accept when implementation is accepted.

## Decision

Add explicit stable linkage between synthesis definitions, generated WAV/provenance, and ordinary sound definitions. Synthesis definitions remain generation source truth; ordinary sound definitions remain runtime authority.

## Consequences

Validation can detect missing, stale, orphaned, drifted, duplicate, and incorrectly mapped outputs without runtime synthesis or a second sound-command model.
