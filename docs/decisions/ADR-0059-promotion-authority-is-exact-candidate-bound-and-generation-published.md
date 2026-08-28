# ADR-0059 — Promotion Authority Is Exact-Candidate-Bound and Generation-Published

## Status

Accepted for M047.

## Context

Historical M029 established useful workbench/session and preview-process structure, but current promotion authority can resolve an opaque candidate to unrelated sample media, infer semantics from candidate-ID text, ignore recorded alternatives/corrections, accept insufficiently bound v1 decisions, and validate a staged workspace mainly by producer-created files/claims.

The existing replace-directory approach also does not provide a clean process-failure authority boundary, and the affected-rebuild command can report success without performing a dependency rebuild.

## Decision

One canonical structured asset candidate binds game-local campaign intent to exact source-relative bytes, media kind, source selection, promotion-relevant proposal data, and typed variants. Candidate IDs are opaque identity and are never parsed for media or behavior semantics.

Current promotion decisions use `agentic2d.asset-review-decision.v2` and bind the exact canonical candidate fingerprint, selected-variant fingerprint, and typed corrections. Historical v1 decisions may remain readable but cannot authorize current promotion and are not automatically migrated.

Promotion materializes only explicit versioned deterministic recipes from exact input hashes. Unsupported corrections fail rather than being ignored.

Approved stable identity is derived from the logical campaign candidate, approved asset kind, and presentation role; mutable candidate/recipe/output fingerprints are revision provenance, not logical identity. Conflicting identity collisions fail.

A promoted workspace publishes immutable fully validated generations. One small current-generation authority record identifies the current generation and is atomically replaced only after independent staged validation. Promotion success requires readback of the newly current validated generation.

Legacy v1 promoted output is not current trusted M047 authority.

Until M049 implements real dependency-aware rebuild, affected rebuild must not report success.

## Consequences

M047 deliberately breaks promotion compatibility with unprovable v1 decision/output authority while preserving non-contradicted M029 session/input behavior. Existing reviewed candidates require explicit v2 re-review before trusted promotion.

Promotion and provenance become byte-addressable, path-independent and mechanically auditable. Process failure can leave only the previous or the new complete validated generation current rather than a partially replaced live set.

M048 can later bind preview evidence to the same canonical candidate fingerprint, and M049 can consume the same current promoted-generation authority and add real dependency rebuild without redefining promotion semantics.
