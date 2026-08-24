# Validation Tiers

## Authority

This document is authoritative for validation tier names, machine-versus-human gate separation, and their interaction with platform epochs.

| Tier | Name | Intended use |
|---:|---|---|
| 0 | Edit sanity | Cheap checks for trivial/doc-only edits. |
| 1 | Focused implementation | Validate affected code/content only. |
| 2 | Standard local gate | Normal pre-completion local confidence. |
| 3 | PR integration | Clean repository validation in CI. |
| 4 | Release gate | Validate public/package/release artifacts. |
| 5 | Human judgment gate | Milestone-scoped subjective/perceptual acceptance that automation cannot decide. |

Milestones and implementation tasks name the expected validation tier.

## Execution modes

Tier names are independent of execution mode:

```text
direct
resumable-sharded
CI-only
human-review
```

A resumable machine suite has current passing receipts only when its fast verifier passes.

## Machine and human gate separation

Tier 5 is not a place to delegate machine-verifiable assertions to a human.

Schema validity, tests, determinism, persistence, fingerprints, artifact completeness, performance thresholds, migration correctness, and other mechanically decidable properties remain machine validation even when their outputs support a milestone that also has human review.

Automated suite verification and human approval are separate authorities:

```text
machine suite --verify
+
milestone review-check
=
completion gates when both apply
```

A machine verifier MUST NOT fail merely because the milestone's required human decision is still pending.

Required/blocking human review is established by the canonical review-check command for the owning milestone after machine prerequisites pass.

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
- absence of an inactive platform does not by itself block ordinary milestone completion.

Tier 5 subjective review follows the active-platform rule unless the owning milestone explicitly requires cross-platform subjective comparison.

A platform switch triggers cumulative catch-up validation against the current repository rather than replaying old milestones.

Release/public distribution gates may impose stricter multi-platform requirements independently of ordinary development milestones.
