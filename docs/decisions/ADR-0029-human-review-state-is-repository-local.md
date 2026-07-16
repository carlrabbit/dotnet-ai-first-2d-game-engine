# ADR-0029 — Human Review State Is Repository-Local

## Status

Accepted with Milestone 022.

## Context

The repository has Tier 5 review guidance and generated review packs, but durable requests, decisions, evidence linkage, and staleness are not represented by one executable repository-local workflow.

## Decision

Required and blocking review state lives under `.review/` and is managed through canonical engineering commands. Generated or large evidence remains under `artifacts/review/`.

Review state is project truth. External guide documents are not operational authority.

## Consequences

Human decisions become inspectable, fingerprinted, and enforceable. Recommended informal review may remain outside `.review/`, but required/blocking completion depends on `review-check`.
