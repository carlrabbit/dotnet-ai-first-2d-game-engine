# Human Review Workflow

## Authority

This document is authoritative for repository-local review requests/records, canonical review commands, milestone review-run behavior, asynchronous decision persistence semantics, active-milestone review reset, milestone completion, and historical compatibility.

The project-level applicability rule is in `docs/HUMAN-REVIEW.md`.

The graphical contract is in `docs/specs/simple-human-review-workbench-contract.md`.

## Applicability

Review levels remain:

```text
none
recommended
required
blocking
```

New required/blocking human questions use subjective classes only:

```text
visual
UX
creative
gameplay
audio
accessibility-baseline
```

Machine-verifiable criteria are automated prerequisites.

## Repository state

Durable authority remains:

```text
.review/
  pending/
  records/
  closed/
```

Generated machine evidence remains under `artifacts/`.

The graphical workbench has no durable session, queue, comment history, rating state, or navigation checkpoint.

## Review states

Existing compatible states remain:

```text
pending
approved
changes-requested
rejected
waived
superseded
```

Normal simple-review decisions are:

- Accept -> `approved`;
- Reject -> `changes-requested`;
- Restart -> review-reset, not a decision;
- window close -> no fabricated decision.

No reviewer comment is required.

## Machine acceptance before human review

Human review is downstream of fast current machine readiness.

Milestone review-run must fail before graphics if any open included review is not current/review-ready.

Do not run long validation in the UI and do not ask the reviewer to inspect machine evidence.

Machine aggregate verification and human review-check are separate gates.

## Canonical commands

```text
./eng/review-list.sh [--milestone <id>] [--state <active|historical>] [--status <status>]
./eng/review-show.sh <review-id-or-alias>
./eng/review-run.sh --milestone <id>
./eng/review-run.sh <review-id-or-alias>
./eng/review-reset.sh --milestone <id>
./eng/review-request.sh --milestone <id> ...
./eng/review-record.sh <review-id-or-alias> <decision> ...
./eng/review-reopen.sh <review-id-or-alias> --reason <reason> [--correct-record]
./eng/review-check.sh --milestone <id>
```

PowerShell 7 exposes equivalent thin adapters.

### review-run --milestone

This is the normal human workflow.

It:

1. verifies the milestone is active;
2. checks fast machine/review readiness;
3. resolves every currently open simple required/blocking review request for that milestone;
4. fails before launch if any included item is not review-ready;
5. orders items deterministically;
6. opens one graphical workbench over an in-memory snapshot;
7. lets Accept/Reject enqueue durable decisions and auto-advance;
8. keeps the graphical loop responsive while persistence occurs;
9. presents the final persistence/status page after all loaded items are decided.

Approved records are not normally included in a new run. Pending/changes-requested items are open.

### targeted review-run

The positional review-id/alias form may launch one item for engineering/debug use. It is not the normal repository-user workflow when a milestone has multiple open items.

### review-reset

`review-reset --milestone <id>` is legal only for an active milestone.

It resets the participating simple required/blocking review set to undecided/open state.

The operation may reopen approvals belonging to the active milestone and clear pending changes-requested state while preserving compatible audit/provenance history.

It must not reopen or mutate completed historical milestones.

Reset is idempotent from the reviewer perspective: after success, the current milestone's simple review set is open/undecided.

### review-record / review-reopen

These remain lower-level/admin compatibility surfaces.

The Review Workbench may use their underlying semantics directly or through equivalent tested engineering APIs. Reviewer interaction must not require shell commands.

### review-check

This is the milestone-scoped human completion gate.

It passes only when every required/blocking review item for the milestone has an acceptable final decision and required machine provenance/evidence exists.

## Workbench decision queue

The workbench persists each item as it is decided.

It uses one in-memory serialized FIFO queue.

Accept/Reject UI handling:

```text
click
-> local acknowledgement
-> enqueue persistence
-> auto-advance
```

Persistence occurs off the graphical/render event loop.

A pending operation must produce visible activity. Normal operation must not display an auxiliary terminal window.

Failed persistence remains visible and retryable.

## Final status

After every loaded item has a local decision, show the status page.

`Close` becomes available only when the persistence queue is empty and all jobs succeeded.

A rejected review may still be durably recorded and the workbench may close; milestone review-check remains failed until it is later approved.

## Restart lifecycle

Restart is implemented through milestone review-reset, not process re-execution.

```text
Restart
-> resetting screen
-> drain decision queue
-> review-reset --milestone
-> clear local state
-> reset/reload review demo state
-> question 1
```

Restart does not rebuild or reload changed code. The reviewer restarts the application manually when desired.

## Navigation

The workbench uses left/right arrows and question ordinal only.

There is no question list/sidebar.

Navigation does not change durable review state.

Accept/Reject automatically advance to the next undecided item.

## Platform epochs

Human review runs on the active development platform unless a milestone explicitly requires cross-platform subjective comparison.

Portable review state and policy remain platform-neutral. Native graphical proof follows active platform authority.

## Historical compatibility

Completed historical milestones remain immutable.

Existing v2 requests/records remain readable.

Active-milestone reset/reopen does not imply perpetual staleness or re-review after milestone completion.
