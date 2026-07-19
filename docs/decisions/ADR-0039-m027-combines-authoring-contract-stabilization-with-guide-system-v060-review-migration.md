# ADR-0039 — M027 Combines Authoring Contract Stabilization with Guide-System v0.6.0 Review Migration

## Status

Proposed for M027. Accept when M027 completes.

## Context

M026 proved geometry diagnostics and generated-sound linkage across two consumers. It also exposed evidence and review-state weaknesses: graphical captures were not preserved, an approved review record was not committed in the accepted revision, and repository-wide review staleness affected historical approvals.

Guide-system v0.6.0 changes human review to milestone-scoped completion gates and removes perpetual repository-wide approval semantics.

The authoring stabilization and review migration affect the same artifact, evidence, command, and validation boundaries.

## Decision

M027 combines stable geometry contracts, stable generated-sound contracts, durable consumer-authoring review packs, milestone-scoped review commands and state, and guide metadata migration from 0.5.1 to 0.6.0.

M027 uses broad AI execution and one blocking milestone-owned review.

## Consequences

Completed M022, M025, and M026 reviews remain historical; later commits do not stale them; M027 checks only M027; stable authoring artifacts feed review packs; future milestones declare new reviews; no broad plugin framework is introduced.
