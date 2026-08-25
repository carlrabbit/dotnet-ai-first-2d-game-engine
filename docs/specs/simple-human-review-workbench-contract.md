# Simple Human Review Workbench Contract

## Authority

This document is authoritative for simple human-review item eligibility, milestone review-run presentation, navigation, asynchronous decision persistence, final status behavior, reset semantics, and the boundary between automated validation and human judgment.

It does not replace asset-specific M029 workbench contracts.

## Principle

Humans review perception and experience. Machines decide mechanically decidable predicates.

A required/blocking human-review item is valid only when its question requires irreducible human judgment such as visual readability, UX/usability, gameplay feel, creative/presentation quality, audio quality, or bounded accessibility observation.

Machine-verifiable claims are prerequisites rather than reviewer questions.

## Allowed required/blocking classes

```text
visual
UX
creative
gameplay
audio
accessibility-baseline
```

Material architecture/semantic decisions belong in planning before `ready`.

Do not add an LLM or heuristic classifier. Planning declares the human question; deterministic tooling validates the shape.

## Simple review item

Each item:

- has one concise question;
- uses an actual scenario or purpose-built review shard;
- exercises the implementation under review rather than a duplicate fake implementation;
- begins at or near the state under judgment;
- requires at most two deliberate reviewed-content interactions before decision;
- may passively animate/run;
- requires no artifact tour, command list, debugger, or source inspection.

The interaction bound applies per item.

## Milestone review run

The normal graphical review operation presents all currently open simple required/blocking review items for one active milestone in one workbench run.

Canonical entry point:

```text
./eng/review-run.sh --milestone <id>
pwsh ./eng/review-run.ps1 --milestone <id>
```

A targeted review-id form may remain for engineering/debug use.

Before opening graphics, milestone mode must:

1. perform the fast current review-readiness/machine-prerequisite check;
2. resolve all open simple required/blocking requests owned by the active milestone;
3. fail before launch if any included item is not review-ready;
4. establish deterministic item order;
5. create an in-memory snapshot for the workbench run.

The workbench owns no durable session.

## Presentation

One item is shown at a time.

Required reviewer-facing concepts:

```text
<          Question N / Total          >

question

actual content/demo

Current decision
Last decision / persistence state

Restart                      Reject  Accept
```

There is no question sidebar/list, abbreviated-question navigation surface, list scrolling, comment field, rating, defer/skip/waive control, evidence browser, or review-history browser.

Left/right arrows navigate the loaded item snapshot.

Accept/Reject automatically move to the next undecided item. When all items have a local choice, the workbench transitions to the final status page.

## Asynchronous decision persistence

Accept and Reject persist each item independently through canonical repository-local review authority.

Button handling must not synchronously wait for engineering persistence.

Required flow:

```text
click
-> enqueue job
-> immediate visual acknowledgement
-> automatic progression
-> serialized background persistence
```

The queue is in-memory, FIFO, single-consumer, and bounded by the loaded item count.

The graphical event loop remains responsive while persistence runs.

If implementation uses a child process/engineering command, normal review operation must use non-shell/no-visible-console behavior and asynchronous completion. A visible auxiliary terminal or render-thread `WaitForExit` is invalid.

The workbench shows activity while jobs exist, including a changing animation/progress indication and pending count or equivalent.

Persistence failure is visible and retryable. Failure never becomes a successful decision merely because local UI advanced.

## Decision semantics

`Accept` -> canonical `approved`.

`Reject` -> canonical non-final `changes-requested`.

No reviewer comment is required.

The workbench may display internal persistence state such as queued/saving/saved/failed, but does not expose durable decision history.

In-place reversal of an already-decided item is not required. The canonical mechanism for clearing decisions is Restart/review-reset.

## Navigation

Left/right navigation is purely workbench-local.

It does not mutate `.review`.

When revisiting an item, show its current local/durable decision state clearly.

If an item already has a decision in the current run, the implementation may render the decision buttons disabled/read-only. Supporting decision replacement without reset is optional and must not complicate the required path.

## Final status page

After every loaded item has a local choice, show a status page.

It contains:

- one line per item using ordinal and `Accepted` or `Rejected`;
- current persistence activity;
- pending job count or equivalent;
- persistence failures and Retry where needed.

Do not reproduce a list of abbreviated question text.

`Close` is unavailable until all queued jobs have completed successfully.

A review pass with rejected items may close after persistence is complete; rejected items remain unresolved for milestone completion.

## Restart and review reset

Restart means reset the whole active milestone review set.

It does **not** restart/re-exec the graphical application.

Canonical reset:

```text
./eng/review-reset.sh --milestone <id>
pwsh ./eng/review-reset.ps1 --milestone <id>
```

Required workbench behavior:

```text
Restart
-> disable new decision input
-> show visible resetting/activity screen
-> drain already queued decision jobs
-> invoke canonical milestone review reset
-> return every participating active-milestone simple review item to undecided/open state
-> clear local navigation/decision/last-action/demo state
-> reload reset items
-> show question 1
```

Reset may reopen approvals belonging to the currently active milestone. It must preserve compatible internal audit/provenance and must never reopen or mutate completed historical milestones.

Reset failure remains visible and retryable.

Restart does not rebuild, hot reload, watch files, reload changed assemblies, or assume a rebuild occurred. The reviewer closes/relaunches the workbench manually when a new executable is desired.

## Durable-state boundary

Durable authority remains:

```text
.review/pending/
.review/records/
.review/closed/
```

The workbench does not add:

- persisted navigation;
- persisted queue;
- comments;
- ratings;
- resume checkpoints;
- a new review-session schema.

Existing v2 review request/record compatibility remains valid.

## Machine prerequisites

Human review follows machine validation.

`review-run --milestone` performs only a fast current prerequisite/review-readiness check before opening graphics. Long validation is never rerun inside the workbench.

If an open item lacks a current executable experience or current prerequisite evidence, the milestone run does not open partially; it fails clearly and identifies the blocking item.

Machine aggregate verification remains independent of human approval.

## Canonical review command roles

```text
review-list                     metadata/admin
review-show                     metadata/admin
review-run --milestone <id>     normal human workflow
review-run <review-id>          targeted/debug workflow
review-reset --milestone <id>   clear active milestone review set
review-record                   lower-level/admin compatibility
review-reopen                   lower-level/admin compatibility
review-check --milestone <id>   milestone human completion gate
```

Thin Bash and PowerShell launchers delegate to tested .NET engineering authority.

## Graphics/dependency boundary

The graphical workbench belongs to repository engineering/debug infrastructure.

Only the isolated Raylib client depends on raylib/native graphics. Durable review semantics and machine validation remain native-graphics independent.

No product/game-consumer review API is introduced.

## M029 boundary

M029 remains asset-specific.

M038 may reuse low-level Raylib techniques but does not generalize M029 sessions, aliases, command entry, candidate lists, consequence rules, preview IPC, or promotion.

## Review-readiness regression

For interactive/UX questions, a screenshot or document describing a flow is not a substitute for a launchable experience.

M037 remains the historical regression example: its main-menu proof cannot answer save/load, display rollback, or input-rebinding UX questions.

Historical M037 remains unchanged.
