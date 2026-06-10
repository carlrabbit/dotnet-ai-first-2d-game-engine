# Validation Tiers

## Authority

This document is authoritative for validation tier names.

| Tier | Name | Intended use |
|---:|---|---|
| 0 | Edit sanity | Cheap checks for trivial/doc-only edits. |
| 1 | Focused implementation | Validate affected code/content only. |
| 2 | Standard local gate | Normal pre-completion local confidence. |
| 3 | PR integration | Clean repository validation in CI. |
| 4 | Release gate | Validate public/package/release artifacts. |
| 5 | Artifact/human review | Validate generated evidence and review-gated outputs. |

Milestones and implementation tasks should name the expected validation tier.
