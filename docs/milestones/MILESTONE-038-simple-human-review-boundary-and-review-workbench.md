# Milestone 038 — Simple Human Review Boundary and Review Workbench

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Repository role | capability-provider |
| Repository profiles | artifact-first-agentic-authoring; runtime-tool; game-simulation |
| Maturity | implementation-ready; artifact-first |
| Scope size | medium |
| Implementation autonomy | high within this contract |
| Documentation sync | separate/deferred except direct authority changes required here |
| Local validation | Tier 1 focused + Tier 2 resumable milestone suite |
| Integration validation | Tier 2 aggregate machine verifier |
| Human review | blocking, active-platform, milestone review run |

M038 remains the active milestone. Product/gameplay expansion remains postponed until M038 completes.

## Goal

Correct the repository's human-review boundary and provide a deliberately small graphical Review Workbench that lets the repository user review **all currently open simple human-review items for one milestone in one run**.

The workbench must make subjective review cheap:

```text
machine validation
-> review-run --milestone M038
-> question 1
-> Accept / Reject
-> automatic transition
-> question 2
-> ...
-> final persistence/status page
-> Close
```

Machine-verifiable claims remain machine-owned. The Review Workbench is not a manual-test framework, feedback tracker, issue system, or persisted review-session product.

## Target State

When M038 is complete:

1. required/blocking human review is limited to irreducibly perceptual or experiential questions;
2. machine-verifiable acceptance is complete before human review can launch;
3. `review-run --milestone <id>` is the normal human entry point and presents all currently open simple review items for that milestone in one graphical run;
4. each review item still contains one concise question and one bounded actual scenario/review-shard experience;
5. the workbench presents one item at a time with no question sidebar/list;
6. left/right arrows navigate previous/next items;
7. `Accept` and `Reject` enqueue durable review operations without blocking the graphical event loop and automatically move to the next undecided item;
8. the workbench visibly reports the last decision and persistence activity;
9. review persistence is serialized through one bounded FIFO background queue;
10. normal review operation never opens an auxiliary terminal/console window and never synchronously waits for an engineering process on the render/UI thread;
11. after all loaded items have a local decision, the workbench shows a final status page containing the choices and queue activity;
12. the final `Close` control is enabled only after all queued decision writes complete successfully;
13. `Restart` means **reset the whole milestone review set**, not restart the application/process;
14. Restart shows a resetting/progress screen, drains existing queued decisions, invokes canonical milestone review-reset authority, clears workbench/demo state, reloads the reset review set, and returns to question 1;
15. Restart does not rebuild, reload code, watch files, or relaunch the process; the reviewer may restart the application manually after a rebuild;
16. `.review/` remains the durable authority for each review item; no durable workbench session, comment stream, queue file, or navigation state is introduced;
17. historical completed milestones remain immutable; reset is legal only for the active milestone review set;
18. automated M038 verification remains independent of human approval;
19. M037 remains a regression proving that a genuine human question is not review-ready without an executable experience capable of answering it.

## Scope

### Human-versus-machine boundary

New required/blocking human questions are limited to:

```text
visual
UX
creative
gameplay
audio
accessibility-baseline
```

Machine-verifiable properties such as schema validity, determinism, persistence correctness, conservation, required-file presence, fingerprints, performance thresholds, architecture constraints, migration completeness, and report consistency are automated prerequisites.

Do not add an LLM or heuristic classifier. Planning declares the human question; deterministic validation enforces the allowed classes and review shape.

### Review item

A simple review item:

- has one concise human question;
- uses an actual scenario or purpose-built review shard exercising the real implementation;
- begins at or near the state under judgment;
- requires at most two deliberate interactions with reviewed content before the reviewer can decide;
- does not require artifact browsing, command tours, debugger knowledge, or implementation reconstruction.

The complexity bound applies **per item**, not to the number of items in a milestone review run.

### Milestone review run

The normal command is:

```text
./eng/review-run.sh --milestone <id>
pwsh ./eng/review-run.ps1 --milestone <id>
```

A targeted form may remain for engineering/debug use:

```text
./eng/review-run.sh <review-id-or-alias>
pwsh ./eng/review-run.ps1 <review-id-or-alias>
```

For milestone mode:

1. resolve the active milestone;
2. perform the fast current machine-prerequisite/review-readiness check;
3. load the currently open required/blocking simple review requests owned by that milestone;
4. reject launch before graphics if any loaded item is not review-ready;
5. order the loaded items deterministically by canonical review identity;
6. launch one Review Workbench containing that in-memory review-run snapshot.

Approved records are not normally included in a new review run. `changes-requested` and undecided active requests are open items.

### Presentation and navigation

Normal question presentation contains:

```text
<            Question N / Total            >

one concise question

actual scenario/review-shard content

Current decision: <none|Accepted|Rejected|saving|failed>
Last decision: <question ordinal + Accepted/Rejected + persistence state>

Restart                         Reject   Accept
```

The exact layout is implementation-owned, but these semantics are required.

There is no question sidebar/list and no abbreviated-question navigation surface.

Left/right arrows:

- navigate through the loaded review-run snapshot;
- do not alter durable review state;
- wraparound behavior is implementation-owned but must be predictable and tested.

`Accept`/`Reject` automatically transition to the next undecided item. If manual navigation skipped earlier items, automatic transition selects the next undecided item in deterministic order; when all loaded items have a local decision, transition to the final status page.

In-place reversal of an already-decided item is not required by M038. Restart/reset is the canonical way to clear decisions and start the review set again.

### Asynchronous durable decision queue

Accept and Reject persist each item independently through canonical repository-local review authority.

Required behavior:

```text
button click
-> enqueue decision job
-> update local visual state immediately
-> advance to next undecided item
-> background worker serially persists queued jobs
```

The queue is:

- in-memory only;
- FIFO;
- single-consumer/serialized;
- bounded by the loaded review-run item count;
- not a new repository artifact or review-session format.

The graphical event loop must remain responsive while jobs execute.

Normal operation must not:

- call synchronous `WaitForExit` from the render/UI thread;
- expose a shell/terminal window;
- block drawing/input for engineering command startup or review persistence.

Implementation may call shared engineering authority directly or invoke existing review commands/processes asynchronously and invisibly. That is a local implementation choice provided the observable contract is satisfied.

While queue work exists, the UI shows visible activity such as:

```text
Saving decision…  ◌
2 pending
```

The precise animation is implementation-owned. It must visibly change while work is pending.

A decision failure:

- is not silently discarded;
- is visible in the workbench;
- does not enable final Close;
- remains retryable without fabricating success.

### Decision semantics

`Accept` persists the review item's canonical `approved` decision.

`Reject` persists the existing non-final `changes-requested` decision.

No reviewer comment is required.

Existing lower-level `.review` provenance/history may remain for compatibility but is not reviewer-facing workflow.

Implementation agents must never synthesize a human Accept action.

### Final status page

Once every loaded item has a local Accept/Reject choice, show a final status page.

The page shows:

- one compact line per loaded item using ordinal plus `Accepted` or `Rejected`; it does not reproduce abbreviated question text;
- queue activity/pending count while persistence remains;
- failed persistence operations, if any;
- a retry action when required;
- `Close` only after every queued decision operation has completed successfully.

Example:

```text
Review pass complete

1  Accepted
2  Rejected
3  Accepted

Saving decisions…  ◌
1 pending
```

Then:

```text
Review decisions saved

1  Accepted
2  Rejected
3  Accepted

[ Close ]
```

A pass containing rejections may close normally once all decisions are durable. The owning milestone remains incomplete because `review-check` will not pass until every blocking review is approved.

### Restart / review reset

`Restart` resets review state; it does not restart the process.

Canonical reset surface:

```text
./eng/review-reset.sh --milestone <id>
pwsh ./eng/review-reset.ps1 --milestone <id>
```

The underlying engineering authority may expose an equivalent `engineering review reset --milestone <id>` operation.

Restart behavior:

```text
Restart
-> stop accepting new review input
-> show RESETTING REVIEW activity screen
-> drain already queued decision jobs
-> execute canonical review-reset for the active milestone
-> reset all participating active-milestone simple review items to undecided/open state
-> preserve internal audit/provenance history required by existing review compatibility
-> clear local decisions/last-decision/navigation/demo state
-> reload the reset review set
-> show question 1
```

Review reset may reopen approval records belonging to the **currently active milestone**. It must not reopen or alter completed historical milestones.

Reset failure leaves the workbench in a visible retryable error state. Local state must not pretend reset succeeded.

Restart explicitly does **not**:

- restart/re-exec the graphical process;
- rebuild code;
- reload changed assemblies;
- watch source/assets;
- assume a rebuild happened.

If the implementation was rebuilt and the reviewer wants a new executable, the reviewer closes and reruns `review-run`.

### Window close

The normal completed path uses the final status page's `Close`.

No workbench session recovery is required after forced process termination. Already completed durable review operations remain authoritative; in-memory navigation/activity state is disposable.

### M029 boundary

M029 remains asset-specific and is not generalized.

M038 may reuse low-level Raylib/event-loop/button techniques only. Do not import or generalize M029 campaign, alias, text-command, consequence, IPC, or promotion semantics.

### M037 regression

Machine regression must establish that:

- M037 had genuine UX/visual questions;
- the existing one-frame main-menu graphical proof could not provide live save/load, display rollback, or input-rebinding experience;
- Markdown/JSON references did not close that gap;
- under M038 such questions are not review-ready until an appropriate executable experience exists;
- M038 does not repair or reopen M037.

## Non-goals

Do not implement:

- a generic review SDK for consumers;
- plugin/provider marketplace or arbitrary-process review protocol;
- persisted workbench sessions or queue files;
- reviewer comments, ratings, issue tracking, or feedback history UI;
- a question sidebar/list, abbreviated question titles, or list scrolling;
- a Next button; Accept/Reject provide automatic progression and arrows provide explicit navigation;
- complex multi-step manual-test orchestration;
- automatic file watching, hot reload, rebuild, or process restart;
- M029 migration/generalization;
- M037 product fixes;
- historical completed-milestone rewriting;
- LLM review classification;
- release-readiness or guide migration work;
- unrelated product/gameplay expansion.

## Decisions and Constraints

1. Human review is subjective/experiential last-mile acceptance only.
2. A milestone review run may contain multiple simple review items.
3. One review item still owns one concise question.
4. The normal workbench runs all currently open milestone review items in one graphical invocation.
5. No question list/sidebar is introduced; arrows navigate.
6. Accept/Reject enqueue persistence and auto-advance.
7. Decision persistence is asynchronous from the graphical event loop and visibly active.
8. A single serialized in-memory FIFO queue is sufficient; no persisted queue/session is allowed.
9. Final Close waits for successful queue drain.
10. Restart invokes milestone review-reset in-process/workbench lifecycle; it does not restart the executable.
11. Reset clears the active milestone review set and reviewer-facing state while preserving compatible internal provenance/history.
12. Visible terminal windows and synchronous engineering waits during normal button interaction are prohibited.
13. `.review` remains durable authority for each item.
14. Machine suite verification and milestone human review-check remain separate gates.
15. Raylib remains isolated to graphical presentation; durable review semantics stay in repository engineering authority.
16. M029 reuse is optional low-level implementation reuse only.

## Baseline Executor Readiness

Ready for GPT-5.6 Luna.

Planning has settled:

- review applicability;
- per-item complexity;
- multi-item milestone-run semantics;
- navigation;
- async persistence semantics;
- final status/drain behavior;
- Restart/reset semantics;
- durable-state boundary;
- process/UI responsiveness requirements;
- historical compatibility;
- M029 and M037 boundaries;
- validation and human-review policy.

Implementation owns concrete files/types/functions, queue implementation mechanics, exact async invocation/direct-call technique, Raylib layout, tests, process abstraction, retry implementation, and refactoring within this contract.

## Required Authority

Read:

1. `AGENTS.md`;
2. `README.md`;
3. this milestone;
4. `docs/HUMAN-REVIEW.md`;
5. `docs/specs/simple-human-review-workbench-contract.md`;
6. `docs/engineering/human-review-workflow.md`;
7. `docs/engineering/command-contract.md`;
8. `docs/engineering/validation-tiers.md`;
9. `docs/specs/raylib-debug-client-contract.md`;
10. `docs/decisions/ADR-0029-human-review-state-is-repository-local.md`;
11. `docs/decisions/ADR-0050-human-review-is-subjective-and-simple-workbench-is-bounded.md`;
12. active platform-verification authority.

For M038 review-command semantics, this milestone plus the updated human-review workflow and simple-workbench contract are newer authority than any stale M038 command examples remaining elsewhere. Implementation must update directly contradicted command indexes/contracts before completion.

Inspect live source/tests as needed. Do not read the external guide repository during implementation.

## Acceptance Criteria

### Multi-item workflow

- `review-run --milestone M038` launches one workbench containing every currently open M038 simple human-review item.
- M038 dogfood contains at least three simultaneously open review items so sequential progression, navigation, background persistence, and reset can be reviewed.
- One item is presented at a time.
- No sidebar/question list exists.
- Left/right arrows navigate.
- Accept/Reject immediately produce visible feedback and automatically transition without waiting for durable persistence to finish.

### Async persistence

- Decision persistence executes through one serialized background queue.
- A deliberately delayed persistence test proves the graphical loop continues updating/responding while a job is pending.
- The UI visibly indicates pending/saving state.
- Normal Accept/Reject interaction opens no auxiliary terminal/console.
- No render/UI-thread synchronous process wait is used for durable review operations.
- Persistence failure is visible and retryable.
- Decisions that report success are actually durable through canonical `.review` authority.

### Final status

- After every loaded item has a choice, the status page shows each ordinal and Accepted/Rejected choice.
- The status page visibly animates/reports activity until the queue is drained.
- `Close` is unavailable while queued jobs remain or any decision failed.
- `Close` becomes available after all decision writes complete successfully.
- A pass with Reject choices may still close; milestone review-check remains unsatisfied.

### Restart/reset

- Restart does not launch a new workbench process.
- Restart immediately transitions to a responsive resetting/activity screen.
- Pending decision jobs are drained before reset authority runs.
- Canonical review-reset returns all participating M038 review items to undecided/open state.
- Local current/last decision state and demo/navigation state are cleared.
- After successful reset the workbench returns automatically to question 1.
- Reset does not rebuild/reload code.
- Reset cannot alter completed historical milestones.

### Compatibility/policy

- Existing historical v2 review records remain readable.
- M029 remains operational and un-generalized.
- M037 regression remains machine-verifiable.
- Machine `m038-smoke --verify` succeeds independently of pending human decisions.
- Existing invalid M038 single-review approval from the first implementation iteration is removed; it is not historical completion authority.

### Documentation/closure

- Review command documentation reflects milestone mode and reset.
- No stale project authority still requires fresh-process Restart or one-review-per-workbench as M038 behavior.
- Completion audit distinguishes machine complete, human pending, and complete.

## Validation

Execution mode: `resumable-sharded`.

Active Windows:

```text
pwsh ./eng/suite.ps1 m038-smoke --plan-json
pwsh ./eng/suite.ps1 m038-smoke --shard policy-and-state
pwsh ./eng/suite.ps1 m038-smoke --shard simple-workbench
pwsh ./eng/suite.ps1 m038-smoke --shard historical-regression
pwsh ./eng/suite.ps1 m038-smoke --shard active-platform-graphics
pwsh ./eng/suite.ps1 m038-smoke --shard review-readiness
pwsh ./eng/suite.ps1 m038-smoke --verify
```

Required M038 proof now includes:

| Shard | Required proof |
|---|---|
| `policy-and-state` | subjective-only policy, multiple active review items, per-item durable state, reset active-milestone compatibility, machine/human gate separation |
| `simple-workbench` | milestone-run snapshot, arrows/no-list presentation contract, Accept/Reject auto-advance, FIFO decision queue, visible async status, final drain/status semantics, reset lifecycle |
| `historical-regression` | M028/M031 machine-only negatives, M032/M034 subjective positives, M037 insufficient-live-experience regression |
| `active-platform-graphics` | real Windows Raylib context, multi-item workbench presentation, navigation/decision input, visible activity state, cleanup; no graphics skip is pass |
| `review-readiness` | all open M038 dogfood items have current executable experience and current machine prerequisite provenance |

Add focused tests/evidence proving a delayed decision write does not freeze the graphical update loop and normal review actions do not request a visible terminal window.

Aggregate machine success authority:

```text
pwsh ./eng/suite.ps1 m038-smoke --verify
```

The verifier MUST NOT require human approval.

Also run:

```text
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

Use Bash equivalents where applicable.

## Human Review

Applicability: `blocking`

M038 dogfoods the multi-item flow with these canonical review items:

```text
review.m038.01-presentation-and-navigation
review.m038.02-decision-feedback-and-persistence
review.m038.03-reset-flow
```

All three are owned by M038 and must be simultaneously open before the final M038 human review begins.

Normal review command:

```text
pwsh ./eng/review-run.ps1 --milestone M038
```

Human questions:

1. **Presentation and navigation** — Is the current question/content immediately understandable, and are left/right navigation plus automatic progression simple without a question list?
2. **Decision feedback and persistence** — Do Accept/Reject respond immediately, show clear last-decision/saving activity, remain responsive while persistence runs, and avoid auxiliary terminal windows?
3. **Reset flow** — Does Restart clearly reset the whole review set through a visible resetting state and return to question 1 without restarting the application?

Reviewer role: `repository user`.

Acceptable final decision for each blocking item: `approved`.

No implicit waiver.

Milestone human gate:

```text
pwsh ./eng/review-check.ps1 --milestone M038
```

After machine validation, if any blocking item remains unresolved, execution terminates `AWAITING HUMAN REVIEW`.

## Completion Audit

### Continue implementation

Any source, direct documentation, machine shard, queue/reset behavior, active-platform graphical proof, or review-readiness requirement remains unsatisfied.

### AWAITING HUMAN REVIEW

Implementation and machine validation are complete, all three M038 review items are open/review-ready, but one or more lack final approval.

### COMPLETE

All implementation/documentation obligations are satisfied, `m038-smoke --verify` passes, all M038 blocking review items are approved, and milestone-scoped review-check passes.

## Direct Documentation Impact

Implementation must keep directly affected authority consistent, especially:

- `docs/HUMAN-REVIEW.md`;
- `docs/specs/simple-human-review-workbench-contract.md`;
- `docs/engineering/human-review-workflow.md`;
- `docs/engineering/command-contract.md`;
- `docs/ENGINEERING.md` if it indexes review commands;
- `docs/decisions/ADR-0050-human-review-is-subjective-and-simple-workbench-is-bounded.md`.

Do not perform broad unrelated documentation synchronization.

## Deferred Documentation Synchronization

The existing focused `.guide-sync/pending/` hint is updated by this planning package. M038 implementation does not depend on external guide synchronization.

## Escalation Boundary

Return to planning if implementation requires:

- a question list/sidebar;
- a persisted review-session/queue model;
- a generic provider/plugin protocol;
- complex exploratory/manual-test orchestration;
- product/public APIs for review;
- reviewer comments/history as normal UX;
- automatic rebuild/hot reload/process restart;
- changing the allowed human-review classes;
- rewriting completed historical milestones;
- M037 product repair or M029 generalization.

Local implementation choices inside this contract remain executor-owned.

## Baseline-Executability Audit

Confirmed:

- architecture: settled;
- semantics: settled;
- durable compatibility: settled;
- multi-item review flow: settled;
- async persistence and responsiveness: settled;
- reset semantics: settled;
- scope/non-goals: settled;
- acceptance: observable;
- validation: resumable and machine/human separated;
- human-review policy: explicit;
- active-platform graphical requirement: explicit;
- baseline model: GPT-5.6 Luna can implement without inventing project policy.

No unresolved material planning issue prevents `ready` status.
