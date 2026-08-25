# Human Review

## Authority

This document defines the project-level human-review boundary.

Detailed repository mechanics are authoritative in `docs/engineering/human-review-workflow.md`.

The graphical review contract is `docs/specs/simple-human-review-workbench-contract.md`.

## Principle

Human review is milestone-scoped acceptance for questions automation cannot fully decide.

Humans review perception and experience, not machine evidence.

Machine-verifiable claims are resolved before human review.

## Valid subjects

New required/blocking human questions are limited to:

```text
visual
UX
creative
gameplay
audio
accessibility-baseline
```

Typical questions concern readability, usability, gameplay feel/comprehensibility, creative/audio quality, or bounded accessibility observation.

Architecture, determinism, persistence, schema validity, artifact completeness, migration correctness, numeric performance, fingerprints, required files, and other mechanically decidable facts are not human-review subjects.

## Simple items, milestone review run

Each human-review item stays simple: one concise question and one bounded actual experience.

The normal repository-user workflow presents **all currently open simple review items for the active milestone in one workbench run**:

```text
machine readiness
-> review-run --milestone M038
-> question 1
-> Accept/Reject
-> question 2
-> ...
-> final save/status page
```

There is no question sidebar/list. Left/right arrows navigate.

Accept/Reject automatically progress while durable decisions are written asynchronously in the background.

The reviewer sees last-decision and persistence activity rather than a frozen UI.

## Restart

Restart means reset the active milestone review set.

It:

- shows a visible resetting state;
- waits for already queued writes;
- invokes canonical review-reset;
- clears workbench/demo state;
- returns to question 1.

It does not restart/re-exec the application, rebuild, hot reload, or watch files.

The reviewer restarts the application manually when a new build should be loaded.

## Durable state

Each item is persisted through existing repository-local `.review` authority.

The workbench itself has no persisted session, queue, comments, ratings, navigation state, or resume checkpoint.

Completed historical milestones remain immutable.

## Evidence rule

Machine evidence is consumed by machine validation.

Human evidence is the actual scenario/review shard the person perceives or uses.

For interactive UX, JSON, Markdown, hashes, or an unrelated screenshot are not substitutes for a launchable experience.

M037 remains the historical regression example for this rule.

## Completion

Machine validation and human approval are separate gates.

The machine milestone suite may pass while human reviews remain open.

The milestone completes only when machine validation passes and milestone-scoped `review-check` passes.
