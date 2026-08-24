# ADR-0050 — Human Review Is Subjective and the Simple Workbench Is Bounded

## Status

Accepted for M038.

## Context

Repository human review had drifted into manual inspection of mechanically decidable evidence. M037 also demonstrated the opposite problem: legitimate UX/visual questions without an executable experience capable of answering them.

The first M038 workbench iteration exposed additional UX problems that are important enough to make architectural constraints:

- one review command per question makes normal human review unnecessarily manual;
- a fresh-process interpretation of Restart is not the reviewer's intended workflow;
- synchronous `dotnet run`/engineering waits from the Raylib event loop freeze the UI and can expose auxiliary terminal windows;
- a question sidebar/list would add abbreviated labels, scrolling, and navigation complexity without value.

The reviewer expects one milestone review run containing all open simple items, immediate Accept/Reject progression, visible asynchronous persistence, and a whole-review reset.

## Decision

Human review remains limited to irreducibly perceptual or experiential acceptance. Machine-verifiable acceptance is automated first.

The Simple Review Workbench is a lightweight **runner over multiple simple repository-local review items**.

Each item still has one concise question and a bounded actual experience.

Normal milestone review behavior:

```text
review-run --milestone
-> one item at a time
-> left/right arrows
-> Accept/Reject enqueue durable decision
-> automatic progression
-> final persistence/status page
```

The workbench has no question list/sidebar.

Decision persistence uses one in-memory serialized background FIFO queue. The graphical event loop remains responsive and shows visible activity. Normal review interaction must not open an auxiliary terminal/console or synchronously wait for engineering command completion.

`Restart` means reset the entire active milestone review set:

```text
drain queue
-> canonical review-reset
-> clear local/demo state
-> question 1
```

Restart does not re-exec the process, rebuild, hot reload, or watch files.

Each item remains durable through existing `.review` authority. No persisted workbench session/queue/comment/history UI is introduced.

## Relationship to ADR-0029

ADR-0029 remains valid: required/blocking review state is repository-local.

This ADR changes the reviewer-facing orchestration, not the durable authority model.

Active-milestone review-reset may reopen/reset the current milestone's items while preserving compatible audit/provenance. Completed historical milestones remain immutable.

## Consequences

- A repository user performs one review-run per milestone review pass rather than one command per question.
- Multiple simple items do not make each item complex.
- Accept/Reject remain individually durable even though the UI immediately advances.
- Background persistence latency is visible but does not freeze review interaction.
- The final page acts as the persistence barrier before normal Close.
- Restart is a whole-review reset, not an application lifecycle feature.
- Arrow navigation avoids sidebar/list complexity.
- The existing `.review` implementation remains useful and no parallel review database is created.
- M029 remains asset-specific and is not generalized.
