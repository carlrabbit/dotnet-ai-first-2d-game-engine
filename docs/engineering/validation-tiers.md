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

## Execution modes

Tier names are independent of execution mode: `direct`, `resumable-sharded`, `CI-only`, or `human-review`. A resumable Tier 2 suite has current passing receipts only when its fast `--verify` passes. Tier 5 required/blocking review is established by `./eng/review-check.sh --milestone <id>` for the review's owning milestone; see `docs/engineering/human-review-workflow.md` for the full review command contract. M027 and M028 use direct focused checks plus resumable aggregates.

Linux/Bash is the tested platform baseline for engineering commands. Native Windows/PowerShell is unsupported until implemented and validated.
