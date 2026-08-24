# Human Review

## Authority

This document defines the current project-level human-review boundary.

Detailed repository mechanics are authoritative in `docs/engineering/human-review-workflow.md`.

The simple graphical experience is authoritative in `docs/specs/simple-human-review-workbench-contract.md`.

## Principle

Human review is a milestone-scoped completion gate for acceptance that automation cannot fully decide.

Humans review perception and experience, not machine evidence.

Machine-verifiable claims must be resolved by automated validation, engineering analysis, or planning before the human-review step.

## Valid human-review subjects

New required/blocking human reviews are limited to bounded questions in these classes:

```text
visual
UX
creative
gameplay
audio
accessibility-baseline
```

Typical valid questions:

- Is the visual state readable?
- Is the interaction understandable?
- Does the behavior feel coherent/autonomous?
- Is the presented creative/audio result acceptable?
- Is visible focus/scaling/critical-state presentation usable for the declared accessibility baseline?

## Not human-review subjects

Do not ask a human to approve because a report says that:

- schemas/tests passed;
- state is deterministic;
- persistence round-tripped;
- resources conserved;
- required files exist;
- hashes/fingerprints match;
- performance is within a numeric budget;
- dependencies/architecture follow a machine-checkable rule;
- migrations/artifacts are structurally complete.

If automation can decide the predicate, automate it.

Material architecture/semantic choices belong in planning before a milestone becomes `ready`.

## Simple review workbench

The normal graphical review path for a simple question is:

```text
machine prerequisites pass
-> review-run
-> one concise question + actual scenario/review shard
-> Restart / Reject / Accept
```

A simple review is intentionally bounded to at most two deliberate interactions with reviewed content before the decision.

The reviewer is not asked to enter comments, browse evidence files, navigate a queue, or maintain a review session.

`Restart` is always explicit. Reject does not automatically reload or close the review. The reviewer chooses Restart after an execution-agent change.

Complex exploratory/manual reviews are explicitly milestone-specific and do not expand the generic simple workbench.

## Evidence rule

Machine evidence is consumed by machine validation.

Human evidence is the actual thing the human must perceive or experience.

For interactive UX, a JSON report, Markdown description, screenshot of another screen, or pass flag is not a substitute for a launchable experience.

M037 remains historical evidence, but its main-menu-only graphical proof is the regression example for why future save/settings/rebinding questions require an appropriate live experience.

## Durable state

Required/blocking final milestone decisions remain repository-local under `.review/` as defined by ADR-0029.

The simple workbench itself is not a durable review database. It requires no reviewer comments, workbench session history, ratings, queues, or resume state.

Completed historical reviews remain immutable and are not reopened by later commits or by M038 policy changes.

## Completion gate

Machine validation and human approval are separate.

Automated milestone suite verification may pass while required human review remains pending.

The milestone completes only when its required machine validation and its milestone-scoped `review-check` both pass.
