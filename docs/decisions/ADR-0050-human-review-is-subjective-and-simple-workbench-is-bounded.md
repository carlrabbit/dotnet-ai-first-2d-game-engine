# ADR-0050 — Human Review Is Subjective and the Simple Workbench Is Bounded

## Status

Accepted for M038.

## Context

Repository human review evolved into a broad Tier 5 gate where humans were often asked to inspect JSON, reports, hashes, architecture summaries, persistence results, and other mechanically decidable evidence.

M037 exposed the opposite failure at the same time: its human questions were largely appropriate UX/visual questions, but the graphical review path only provided a bounded main-menu proof rather than live save/settings/rebinding experiences capable of answering those questions.

The repository also contains an M029 asset workbench, but its session, alias, command, consequence, preview-IPC, and promotion semantics are asset-specific and were not established as a general review platform.

The intended project loop is planning/human intent -> execution agent -> automated validation -> human subjective checkpoint -> small fixes return to execution, while material contract issues return to planning.

## Decision

Human review is limited to irreducibly perceptual or experiential acceptance. Mechanically decidable acceptance is automated before human review.

Introduce a deliberately small repository-engineering Review Workbench for simple human questions only.

A simple workbench review:

- has one concise question;
- uses actual current content through a scenario or purpose-built review shard;
- requires at most two deliberate interactions with reviewed content;
- exposes only `Restart`, `Reject`, and `Accept` as primary controls;
- never requires reviewer comments/history;
- never automatically restarts/reloads after changes or Reject;
- uses a fresh process when the reviewer explicitly chooses Restart;
- retains the existing minimal repository-local final review record as milestone completion authority.

Machine suite verification and human review-check remain distinct completion gates.

M029 is not generalized or migrated. Reuse from it is limited to optional low-level implementation techniques.

Complex human reviews are outside the simple workbench. They require an explicit milestone-specific manual/exploratory path or a truthful purpose-built experience that reduces the question to the simple bound.

## Relationship to ADR-0029

ADR-0029 remains valid: required/blocking milestone review completion state is repository-local.

This ADR narrows what may qualify as human review and removes reviewer-facing session/history/comment complexity from the normal simple-review workflow. It does not require rewriting historical records or migrating the v2 review format.

## Consequences

- Humans no longer duplicate machine validation by reading reports.
- A missing live experience blocks review readiness instead of producing a command/evidence-reading checklist.
- Review UX is intentionally too small to become a manual-test framework.
- The reviewer controls when a changed implementation is restarted.
- Conversational feedback remains in the planning/execution loop rather than becoming a new issue/comment subsystem.
- Future milestones must plan complex subjective validation explicitly instead of expanding the generic workbench.
