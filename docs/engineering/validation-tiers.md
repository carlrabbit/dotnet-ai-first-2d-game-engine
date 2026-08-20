# Validation Tiers

## Authority

This document is authoritative for validation tier names and their interaction with platform epochs.

| Tier | Name | Intended use |
|---:|---|---|
| 0 | Edit sanity | Cheap checks for trivial/doc-only edits. |
| 1 | Focused implementation | Validate affected code/content only. |
| 2 | Standard local gate | Normal pre-completion local confidence. |
| 3 | PR integration | Clean repository validation in CI. |
| 4 | Release gate | Validate public/package/release artifacts. |
| 5 | Artifact/human review | Validate generated evidence and review-gated outputs. |

Milestones and implementation tasks should name the expected validation tier.

## Execution modes

Tier names are independent of execution mode:

```text
direct
resumable-sharded
CI-only
human-review
```

A resumable Tier 2 suite has current passing receipts only when its fast verifier passes.

Tier 5 required/blocking review is established by the canonical review-check command for the owning milestone.

## Platform epochs

Linux/Bash and native Windows/PowerShell 7 are supported development targets.

Current per-milestone platform authority is defined by:

```text
eng/platform-verification.json
docs/engineering/platform-verification.md
```

For normal milestone execution:

- portable Tier 0–3 validation runs on the active development platform;
- active-platform native/integration validation runs on that platform;
- inactive-platform-specific validation may be recorded as deferred verification debt;
- deferred inactive-platform evidence is neither pass nor failure;
- absence of an inactive platform does not by itself block milestone completion.

A platform switch triggers cumulative catch-up validation against the current repository rather than replaying old milestones.

Graphics-capable native proof follows the same active-platform rule unless a milestone or release gate explicitly requires multi-platform evidence.

Release/public distribution gates may impose stricter multi-platform requirements independently of ordinary development milestones.
