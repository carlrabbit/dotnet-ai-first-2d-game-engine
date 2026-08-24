# Human Review Workflow

## Authority

This document is authoritative for repository-local review requests/records, canonical review commands, simple-review launch behavior, milestone-scoped review completion, and historical review compatibility.

The project-level applicability rule is in `docs/HUMAN-REVIEW.md`.

The graphical simple-review contract is in `docs/specs/simple-human-review-workbench-contract.md`.

## Applicability

Review levels remain:

```text
none
recommended
required
blocking
```

For new required/blocking human reviews, the human question must belong to at least one allowed subjective class:

```text
visual
UX
creative
gameplay
audio
accessibility-baseline
```

Engineering descriptors such as semantic, architecture, migration, artifact-quality, release-readiness, persistence, performance, security, public-api, or platform-compatibility do not by themselves justify a human gate.

Machine-verifiable criteria are automated prerequisites, not reviewer questions.

Do not add an LLM classifier. Planning declares the human question and deterministic tooling enforces the allowed boundary.

## Repository state

The existing repository-local completion authority remains:

```text
.review/
  pending/
  records/
  closed/
```

Generated/large machine evidence remains under `artifacts/`.

The simple Review Workbench owns no durable reviewer session, queue, comment history, or resume state.

## Review states

Existing review states remain compatible:

```text
pending
approved
changes-requested
rejected
waived
superseded
```

For the normal simple-workbench path:

- `Reject` uses `changes-requested` and remains non-final;
- `Accept` uses `approved` and is the normal final completion decision;
- window close does not create a decision;
- Restart does not change review state.

No reviewer comment is required for Reject or Accept.

Historical/internal decision history may remain in the v2 representation for compatibility, but normal reviewer UX must not require or expose it as a workflow.

## Machine acceptance before human review

Human review is downstream of current machine validation.

The owning milestone must identify a fast current prerequisite check for its simple review. `review-run` invokes/checks that fast authority before opening graphics.

If prerequisites fail, are stale, or the experience is unavailable, `review-run` fails clearly and records no human decision.

Do not run long validation inside the workbench.

Do not ask the human to inspect machine evidence to compensate for a missing assertion.

Automated suite verification and human review are separate gates. A pending required human review does not make the machine suite verifier fail.

## Canonical commands

The canonical family is:

```text
./eng/review-list.sh [--milestone <id>] [--state <active|historical>] [--status <status>]
./eng/review-show.sh <review-id-or-alias>
./eng/review-run.sh <review-id-or-alias>
./eng/review-request.sh --milestone <id> ...
./eng/review-record.sh <review-id-or-alias> <decision> ...
./eng/review-reopen.sh <review-id-or-alias> --reason <reason> [--correct-record]
./eng/review-check.sh --milestone <id>
```

Windows uses PowerShell 7 thin adapters with the same engineering-host semantics.

### `review-list`

Lists active/historical review metadata and may maintain ephemeral aliases under ignored session artifacts.

### `review-show`

Read-only metadata inspection. It is not the normal human acceptance UI.

### `review-run`

The normal human entry point for a simple review.

It:

1. resolves the canonical review;
2. confirms it belongs to an active milestone and is eligible for simple review;
3. checks the owning milestone's fast current machine prerequisite;
4. resolves exactly one current scenario/review-shard experience;
5. launches the simple Raylib Review Workbench;
6. applies reviewer `Reject`/`Accept` through canonical repository-local review semantics.

It must not fabricate a decision when graphics/experience launch fails.

### `review-request`

Creates the durable milestone-owned request. Existing v2 compatibility remains valid for M038; a new persisted session schema is not required.

### `review-record`

Remains a lower-level/admin compatibility surface. Normal simple human approval/rejection uses `review-run` controls.

### `review-reopen`

Retains existing active-versus-historical restrictions. M038 does not reopen completed historical reviews.

### `review-check`

The milestone-scoped human completion gate.

It verifies final review state and required machine provenance/evidence paths. It does not require the human to read those paths.

It ignores historical milestones as current gates.

## Simple workbench behavior

One invocation represents one review question/experience.

Normal chrome:

```text
review title / concise question
actual content/demo
Restart    Reject    Accept
```

The workbench has no normal comment field, rating, defer/skip/waive button, evidence browser, or multi-review navigation.

A simple review requires at most two deliberate interactions with reviewed content before decision.

If a review exceeds that bound, do not enlarge the workbench. The owning milestone must provide a truthful simpler scenario/shard or an explicit separate manual/exploratory path.

## Restart lifecycle

Restart is reviewer-controlled and fresh-process.

```text
Restart click
-> release current review execution/native resources
-> start the same canonical review again from current repository/build state in a fresh process
```

Do not automatically reload because content completes, files change, or Reject is clicked.

An in-process scenario reset may exist inside reviewed content for its own semantics, but it is not the Review Workbench `Restart` action when changed code/assets must be reloaded.

## Reject lifecycle

Reject:

- records/updates the active request to `changes-requested`;
- requires no reviewer note;
- does not close the workbench;
- does not restart;
- does not produce final completion.

The reviewer may leave the workbench open while a small correction is made, then explicitly choose Restart.

Material issues that change the ready contract return to planning rather than being encoded as review comments.

## Accept lifecycle

Accept:

- records `approved` through canonical review authority;
- is the final human completion decision;
- may close the workbench after successful recording.

Implementation agents must not invoke/simulate Accept.

## Platform epochs

Unless a milestone explicitly requires multi-platform subjective comparison, human review runs on the active development platform.

Portable review state/policy remains platform-neutral.

Graphics launch must use the isolated native adapter and follow current platform-verification authority.

Inactive-platform subjective/native verification may be deferred to a future platform catch-up task.

## Historical compatibility

Completed records are immutable historical evidence and do not stale because later commits or review-policy changes occur.

Existing v2 records remain readable.

M038 narrows future applicability and interaction behavior; it does not rewrite M022–M037 history.
