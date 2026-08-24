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
| Documentation sync | separate/deferred pass except direct authority changes required below |
| Local validation | Tier 1 focused + Tier 2 resumable milestone suite |
| Integration validation | Tier 2 aggregate verifier; normal Tier 3 repository validation where available |
| Human review | blocking, active-platform, one simple workbench review |

M038 is the next active milestone. Previously contemplated post-M037 product/gameplay expansion is postponed until M038 completes.

## Goal

Correct the repository's human-review boundary and provide a deliberately small engine-powered Review Workbench for simple perceptual/experiential reviews.

Human review must stop acting as manual verification of JSON, reports, hashes, architecture, persistence, determinism, or other mechanically decidable claims. Automated validation must establish those claims before human review becomes available.

For the remaining genuinely human question, the reviewer should receive the actual current scenario or purpose-built review shard, one concise question, and exactly three primary controls:

```text
Restart    Reject    Accept
```

The workbench exists to make a small subjective checkpoint cheap. It is not a general manual-testing framework, review database, feedback tracker, or replacement for planning.

## Target State

When M038 is complete:

1. required/blocking human review is permitted only for irreducibly perceptual or experiential judgment;
2. machine-verifiable acceptance is owned by automated validation and is complete before a simple human review can launch;
3. the canonical human entry point can launch a simple Review Workbench for a milestone-owned scenario or review shard;
4. one workbench invocation presents one review subject/question and the actual current content/demo needed to judge it;
5. a simple review requires no more than two deliberate interactions with the reviewed content before the reviewer can decide; review-chrome controls do not count toward this limit;
6. the workbench exposes `Restart`, `Reject`, and `Accept` as its only primary decision controls;
7. `Restart` is always reviewer-triggered and starts a fresh review process against the current repository/build state; file watching, automatic reload, and in-process reset masquerading as code reload are prohibited;
8. `Reject` records a non-final changes-requested outcome but neither restarts nor automatically closes the review experience;
9. `Accept` records the canonical final approval and may close the workbench;
10. closing the window without approval never fabricates a completed review;
11. no reviewer comment, rating, defer action, multi-item queue, resumable workbench session, or reviewer-facing decision history is required;
12. the existing repository-local `.review` completion record remains the minimal durable milestone gate required by current guide/project authority;
13. historical review records remain unchanged and readable;
14. automated milestone-suite verification is independent of human approval; human review is a separate completion gate;
15. M037 is retained as a regression example proving that a screenshot/report or named evidence file cannot substitute for the missing live experience required to answer a genuine human question.

## Scope

### Human-versus-machine review boundary

Replace the current broad "human reviews evidence" interpretation with this rule:

> A human-review gate is valid only when the acceptance question requires human perception, usability judgment, experiential/gameplay judgment, creative/presentation judgment, audio judgment, or bounded accessibility observation that automation cannot decide reliably.

Allowed human-review classes for new required/blocking reviews are limited to:

```text
visual
UX
creative
gameplay
audio
accessibility-baseline
```

A mixed milestone may contain machine and human acceptance, but machine criteria are prerequisites rather than questions delegated to the reviewer.

The following are not sufficient reasons for human review and must be decided through planning, automated validation, engineering analysis, or explicit non-human acceptance mechanisms:

```text
architecture
semantic correctness
determinism
persistence correctness
schema validity
artifact completeness
migration correctness
performance budgets
security properties
public/API compatibility
release-readiness bookkeeping
platform compatibility facts
hash/fingerprint consistency
required-file presence
```

Do not add an LLM classifier that guesses whether arbitrary prose is subjective. Milestone planning declares the human question. Deterministic validation enforces the allowed boundary.

### Simple Review Workbench

Provide one canonical human entry point:

```text
./eng/review-run.sh <review-id-or-alias>
pwsh ./eng/review-run.ps1 <review-id-or-alias>
```

The launchers remain thin over tested .NET engineering semantics.

For a simple review, the engineering review authority resolves the canonical review ID to exactly one current milestone-owned review experience. The concrete representation of that binding is implementation-owned; no public product API or generic plugin system is required.

A simple review experience is one of:

- a deterministic scenario presented through actual engine rendering/runtime capability; or
- a purpose-built engineering review shard that places the reviewed implementation directly into the state that needs judgment.

M038 does not create a generalized arbitrary-process/provider protocol.

The experience must use current implementation/content. A Markdown description, raw JSON, precomputed pass flag, or unrelated screenshot cannot stand in for an executable experience when the question concerns interactive behavior.

### Simplicity bound

The generic workbench is intentionally inapplicable to complex reviews.

A simple review must:

- have one concise review question/subject;
- begin at or very near the state being judged;
- require at most two deliberate interactions with reviewed content before a decision can be made;
- avoid setup tours, command lists, long workflows, multi-step test scripts, and reviewer reconstruction from artifacts;
- remain understandable without debugger or implementation knowledge.

If a human question would otherwise exceed this bound, planning or implementation may create a representative purpose-built scenario/shard that truthfully starts closer to the state under judgment. Do not split or stage the experience in a way that changes the property being reviewed.

If the property cannot honestly be reduced to a simple experience, the milestone must declare an explicit separate manual/exploratory human-review path. The generic M038 workbench must not grow to absorb it.

### Workbench interaction

The workbench's reviewer-facing chrome is intentionally small:

```text
review title / concise question
actual content or demo
Restart    Reject    Accept
```

Secondary developer diagnostics may exist outside normal reviewer presentation, but the normal human review must not require them.

No normal workbench controls for:

- comments or text notes;
- approve-with-conditions;
- ratings;
- defer/skip/waive;
- next/previous review queue;
- evidence-file browsing;
- aliases/candidate selection unrelated to the reviewed content;
- promotion or asset semantics.

### Restart semantics

`Restart` means a fresh execution, not an in-memory world reset when code or assets may have changed.

Required behavior:

```text
reviewer explicitly chooses Restart
-> current review execution ends/relinquishes native resources
-> canonical review launch path starts a fresh process from current repository/build state
-> same canonical review remains active
```

No source-file watcher or automatic restart is required or allowed by default.

The workbench must not automatically reload/restart because:

- a scenario reaches terminal state;
- a reviewed shard finishes its internal activity;
- the reviewer presses Reject;
- files change on disk.

The reviewer chooses when to restart after an execution-agent change.

### Reject and Accept semantics

`Reject` is non-final. It updates the active repository-local review to `changes-requested` (or the equivalent existing non-final state) without requiring a note and without closing/restarting the workbench.

The intended development loop is:

```text
planning/human intent
-> execution agent
-> automated validation
-> simple human review
   -> Accept: milestone may complete
   -> Reject: small correction returns to execution agent
              reviewer chooses Restart when ready
   -> material issue: return to planning
```

`Accept` records `approved` through the canonical repository-local review authority. No implementation agent may fabricate that action.

M038 does not require removal of historical/internal provenance fields from the existing review-file format. Those fields are compatibility details, not reviewer-facing workflow. No schema migration of historical v2 review records is required.

### Machine prerequisite and review readiness

Machine acceptance and human review must be separate.

A simple review may launch only after a fast current machine-prerequisite check for its owning milestone succeeds. The workbench must not make the reviewer verify those prerequisites manually.

The exact registration from a review ID to its fast prerequisite verifier is implementation-owned. It must be deterministic and testable.

The M038 automated suite verifier itself must not fail merely because the M038 blocking human review is still pending. This intentionally corrects the prior pattern where machine aggregate verification and human approval were entangled.

### Existing M029 asset workbench

M029 is not treated as a mature generic review framework and is not migrated in M038.

Its asset-specific concepts remain outside the simple-review contract:

- campaign candidate lists;
- editable command field;
- aliases;
- asset decision vocabulary;
- consequences;
- preview IPC/session recovery;
- promotion planning;
- durable asset decision history.

Implementation may reuse small Raylib window/button/event-loop techniques from existing M029 code where that is locally useful. It must not generalize or import the M029 provider/session model merely for reuse.

### M037 regression

M038 must encode a bounded regression based on the M037 failure that motivated this work.

The regression must establish at minimum:

- M037 contained genuine UX/visual questions;
- its one-frame main-menu graphical proof could not provide a live save/load, display rollback, or input-rebinding experience;
- named Markdown/JSON review-pack artifacts do not close that gap;
- under the M038 policy such a review definition is not review-ready until an appropriate executable experience exists;
- M038 does not repair or reopen historical M037.

Use fixed representative regression fixtures/assertions. Do not build a natural-language classifier over historical review records.

### Historical compatibility

- Existing `.review/records/` remain immutable historical evidence.
- Existing v2 review request/record files remain readable.
- M038 does not reopen M025–M037.
- M038 may change validation rules for newly created required/blocking reviews without retroactively invalidating historical records.
- ADR-0029's repository-local completion authority is retained; M038 narrows what qualifies for human review and how simple interactive review is performed.

### Platform boundary

The active development epoch is Windows.

- Portable review-policy/state semantics must remain platform-neutral.
- The Raylib workbench must respect the existing isolated-native-adapter boundary.
- Active-Windows graphical smoke is mandatory for M038.
- Inactive Linux native/graphical verification may be recorded as deferred platform debt under existing platform-epoch policy; it is not fabricated.
- M038 human review occurs on the active Windows platform.

## Non-goals

Do not implement:

- a generic review SDK/package for game consumers;
- a plugin/provider marketplace or dynamic review loader;
- a general manual-testing or exploratory-testing application;
- complex multi-step review orchestration;
- reviewer comments, issue tracking, ratings, or feedback history UI;
- review queues, next/previous navigation, resumable workbench sessions, or workbench aliases;
- AI/LLM classification of review criteria;
- video recording or screenshot management as a replacement for live experience;
- automatic source watching, hot reload, or automatic restart after Reject;
- M029 asset-workbench migration/generalization;
- M037 product-shell/save/settings/rebinding completion work;
- historical review-record rewriting;
- guide-system migration;
- release-readiness work;
- unrelated gameplay/product expansion;
- new TBPs, issue templates, workflows, or copied guide material.

## Decisions and Constraints

1. Human review is subjective/perceptual last-mile acceptance, never manual duplication of mechanically decidable validation.
2. Planning owns material architecture/semantic choices. A `ready` milestone must not defer such choices to a human-review gate.
3. The simple workbench is deliberately bounded to one question and at most two reviewed-content interactions.
4. One invocation represents one review. Do not create a generic multi-item review session.
5. The workbench uses actual current engine/application content through a scenario or purpose-built shard.
6. The only primary reviewer controls are `Restart`, `Reject`, and `Accept`.
7. Reject is non-final and does not restart or close. Restart is explicit. Accept is the final approval action.
8. Restart uses a fresh process/current build state; no hidden in-process code reload claim.
9. The reviewer is not asked for comments. Conversational feedback to the execution/planning agent remains outside durable review state.
10. Existing `.review` final completion authority is retained for guide compatibility; M038 does not introduce a new durable review database or v3 session schema.
11. Machine suite verification and human review-check are distinct gates.
12. Raylib remains isolated to the graphical adapter. Engineering review authority must not expose raylib types.
13. Reuse from M029 is opportunistic low-level implementation reuse only, not a requirement to preserve its architecture.

## Baseline Executor Readiness

Readiness is confirmed for GPT-5.6 Luna.

Planning has settled the material decisions affecting:

- human-versus-machine acceptance boundary;
- review applicability classes;
- simple-review complexity limit;
- workbench interaction and restart semantics;
- durable-state boundary;
- relationship to M029;
- historical compatibility;
- M037 regression expectation;
- machine/human gate separation;
- cross-platform policy;
- scope and non-goals.

The implementation agent owns concrete source layout, types, functions, code reuse/refactoring, test organization, exact review-registration representation, process-restart mechanics, and supporting edits that stay inside this contract.

No stronger implementation model is required to resolve project policy.

## Required Authority

Read these project-truth documents before implementation:

1. `AGENTS.md`;
2. `README.md`;
3. `docs/TERMINOLOGY.md`;
4. `docs/SPECS.md`;
5. `docs/specs/agentic-workflow.md`;
6. `docs/specs/simple-human-review-workbench-contract.md`;
7. `docs/ENGINEERING.md`;
8. `docs/HUMAN-REVIEW.md`;
9. `docs/engineering/command-contract.md`;
10. `docs/engineering/validation-tiers.md`;
11. `docs/engineering/human-review-workflow.md`;
12. `docs/engineering/platform-verification.md`;
13. `eng/platform-verification.json`;
14. `docs/specs/raylib-debug-client-contract.md`;
15. `docs/decisions/ADR-0029-human-review-state-is-repository-local.md`;
16. `docs/decisions/ADR-0041-asset-preview-host-is-a-separate-restartable-client-around-engine-systems.md`;
17. `docs/decisions/ADR-0050-human-review-is-subjective-and-simple-workbench-is-bounded.md`;
18. `docs/milestones/MILESTONE-029-choice-driven-asset-workbench-persistent-audiovisual-preview-and-deterministic-promotion.md` only for existing M029 behavior/boundary, not as generic workbench authority;
19. `docs/milestones/MILESTONE-037-product-shell-ui-saves-settings-and-input.md` only as the historical regression source.

Then inspect only the live source/tests needed to implement the change, especially the current engineering review host and Raylib workbench/preview/session code. Do not read the external guide repository or `.guide-profile.json` during ordinary implementation.

## Acceptance Criteria

### Review-policy outcome

- New required/blocking reviews cannot use machine-only criteria as human acceptance.
- A mixed review separates objective prerequisites from the human question.
- Human-review readiness fails clearly when a required simple experience is absent, stale, non-executable, or outside the simple-review bound.
- No LLM classifier is introduced.

### Workbench outcome

- The canonical `review-run` command exists on Bash and PowerShell 7 and resolves the same engineering semantics.
- A scenario-backed simple review launches the current engine-rendered experience with the review question and exactly the three primary controls.
- A shard-backed simple review can launch directly at a review-relevant state without creating a generic provider/plugin framework.
- Normal reviewer presentation contains no comment field, rating, defer/skip/waive action, candidate alias UI, next/previous queue, evidence browser, or asset-promotion controls.
- The reviewed content remains visible when it naturally completes; completion does not auto-close or auto-restart the review.
- Reject records a non-final changes-requested state without requiring a comment, without restarting, and without auto-closing.
- Restart is triggered only by the reviewer and starts a fresh review execution against current build/repository state.
- Accept records the canonical final approval. Closing without Accept cannot complete the review.

### Gate separation

- `m038-smoke --verify` can report automated validation success while the M038 human review is still pending.
- The M038 milestone remains `AWAITING HUMAN REVIEW` until the separate milestone-scoped review-check passes.
- Automated verification never asks the reviewer to inspect raw JSON/reports as a substitute for an automated assertion.

### Compatibility and regression

- Existing historical v2 review records remain readable and unchanged.
- Existing M029 asset-workbench behavior remains operational; M038 does not require M029 migration.
- Representative future-style versions of M028/M031 machine-only review criteria are rejected as invalid human-review definitions.
- Representative M032/M034 subjective criteria remain valid candidates for simple human review when supplied with an appropriate bounded experience.
- The M037 regression proves that its current main-menu-only graphical proof cannot satisfy save/settings/rebinding human questions and would not be review-ready under M038.

### Documentation and closure

- Directly affected review/validation/project-workflow authority is consistent with the implemented behavior.
- `docs/SPECS.md`, `docs/DECISIONS.md`, `docs/TERMINOLOGY.md`, `docs/ENGINEERING.md`, and `docs/engineering/command-contract.md` are updated where implementation would otherwise leave them contradictory or fail to index the new permanent authority.
- Any inactive-Linux M038 native verification debt is recorded through existing platform-verification authority rather than invented success.
- A completion audit distinguishes machine validation success, pending/approved human review, and final milestone completion.

## Validation

### Validation execution mode

`resumable-sharded`

M038 uses the generic suite interface.

On the active Windows development platform:

```text
pwsh ./eng/suite.ps1 m038-smoke --plan-json
pwsh ./eng/suite.ps1 m038-smoke --shard policy-and-state
pwsh ./eng/suite.ps1 m038-smoke --shard simple-workbench
pwsh ./eng/suite.ps1 m038-smoke --shard historical-regression
pwsh ./eng/suite.ps1 m038-smoke --shard active-platform-graphics
pwsh ./eng/suite.ps1 m038-smoke --shard review-readiness
pwsh ./eng/suite.ps1 m038-smoke --verify
```

Bash exposes the same suite/shard semantics:

```text
./eng/suite.sh m038-smoke --plan-json
./eng/suite.sh m038-smoke --shard <id>
./eng/suite.sh m038-smoke --verify
```

Required shard contract:

| Shard | Required proof |
|---|---|
| `policy-and-state` | review applicability rules, existing review-state compatibility, Reject/Accept state transitions, machine/human gate separation |
| `simple-workbench` | scenario/shard simple-review contract, three-control UI structure, explicit restart lifecycle, no reviewer-comment/session machinery |
| `historical-regression` | fixed M028/M031 negative cases, M032/M034 subjective positive cases, M037 insufficient-experience regression |
| `active-platform-graphics` | real Windows Raylib context, current review experience draw, bounded smoke input, cleanup; no graphical skip counts as pass |
| `review-readiness` | fast machine prerequisite check allows launch only from current validated state and produces machine provenance for the M038 review request |

Receipts:

```text
artifacts/validation/m038-smoke/<shard>.json
```

Fingerprint scope follows existing EngineeringHost suite authority: current repository, suite definition, command, declared inputs, and validated result/evidence. Previous receipts are invalidated before shard execution; only atomic passing receipts count.

Aggregate automated success authority:

```text
pwsh ./eng/suite.ps1 m038-smoke --verify
```

or Bash equivalent.

The aggregate verifier checks machine receipts only. It MUST NOT require human approval.

Also run the directly affected standard gates:

```text
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

Use Bash equivalents on Linux.

### Graphics/constrained-runtime rule

The active-platform graphical shard is mandatory. A `skipped-not-graphics-capable` result is not M038 validation success.

If a disconnected execution harness cannot access the active Windows graphical session, it must not fabricate a passing receipt. The shard may be run in a separate foreground invocation by the repository user/authorized environment, then consumed by the fast verifier. Do not use backgrounding, detached processes, or timeout inflation.

## Human Review

Applicability: `blocking`

Review class:

```text
UX
visual
```

Canonical review ID:

```text
review.m038.simple-human-review-boundary-and-workbench
```

Owning milestone:

```text
M038
```

Review subject:

> Is the simple Review Workbench itself clear enough that a reviewer immediately understands what is being judged, sees the actual content/demo, and retains explicit control over restart and the Accept/Reject decision?

Reviewer role: `repository user`

Required human evidence:

- the live M038 simple Review Workbench launched through the canonical `review-run` command on the active Windows platform;
- the M038 review fixture must be a simple bounded experience, not a JSON/report inspection.

Machine provenance referenced by the review request is not reviewer reading material.

The human review should require at most one explicit `Restart` demonstration plus the final decision. Automated validation owns Reject state-transition correctness and all structural assertions.

Canonical review command:

```text
pwsh ./eng/review-run.ps1 review.m038.simple-human-review-boundary-and-workbench
```

Bash equivalent:

```text
./eng/review-run.sh review.m038.simple-human-review-boundary-and-workbench
```

Acceptable completion decisions:

```text
approved
```

Waiver policy:

No implicit waiver. M038 introduces the human-review UI itself, so the blocking UX/visual review cannot be replaced by machine evidence or waived as normal completion.

Milestone review gate:

```text
pwsh ./eng/review-check.ps1 --milestone M038
```

or:

```text
./eng/review-check.sh --milestone M038
```

The implementation agent must never activate `Accept` or fabricate the approval. When machine work is complete and this decision remains, terminate as `AWAITING HUMAN REVIEW` with the exact command above.

After the repository user approves, rerun review-check and the completion audit. Historical completed reviews remain untouched.

## Completion Audit

The executor must distinguish these states:

### Automated work incomplete

Any required implementation, direct documentation, shard, receipt, graphics proof, or machine verifier is missing/failed.

Result: continue implementation; do not ask for human review.

### Machine-complete, human pending

All implementation and machine validation pass, the simple review is launchable, but M038 approval is not recorded.

Result:

```text
AWAITING HUMAN REVIEW
```

### Complete

All implementation/documentation obligations are satisfied, `m038-smoke --verify` passes, the blocking M038 review is approved, and milestone-scoped `review-check` passes.

Result:

```text
COMPLETE
```

If implementation reveals a material conflict with this contract, return to planning rather than broadening the workbench.

## Direct Documentation Impact

Planning authority supplied with this milestone already establishes the target review policy and workbench contract.

Implementation must keep directly affected live authority consistent with the realized command surface and exact implementation behavior, including indexes where needed. At minimum inspect/update as applicable:

- `docs/SPECS.md`;
- `docs/DECISIONS.md`;
- `docs/TERMINOLOGY.md`;
- `docs/HUMAN-REVIEW.md`;
- `docs/specs/agentic-workflow.md`;
- `docs/specs/simple-human-review-workbench-contract.md`;
- `docs/engineering/human-review-workflow.md`;
- `docs/engineering/validation-tiers.md`;
- `docs/engineering/command-contract.md`;
- `docs/ENGINEERING.md`;
- active platform-verification authority if M038 creates inactive-platform debt.

Do not perform broad unrelated documentation cleanup.

## Deferred Documentation Synchronization

A focused `.guide-sync/pending/` hint is supplied. It records that the external guide system may later want to adopt the machine-versus-human review distinction and the no-manual-JSON principle.

M038 implementation does not depend on that external synchronization. Ordinary implementation agents must not read `.guide-sync/`.

## Escalation Boundary

Implementation owns concrete code/test mechanics, source reuse, process-launch details, exact engineering registration shape, and local refactoring within the resolved boundary.

Return M038 to planning if implementation discovers that completion requires any of these new material decisions:

- expanding the workbench beyond the one-question/two-interaction bound;
- adding a generic provider/plugin protocol;
- changing public/product APIs to support review;
- replacing repository-local final review authority;
- requiring reviewer comments/history as product behavior;
- altering historical review records;
- repairing M037 product functionality as part of M038;
- changing the active-platform/graphics policy;
- changing what qualifies as a valid human-review class.

A local implementation difficulty is not a reason to broaden the milestone.

## Baseline-Executability Audit

Confirmed before marking `ready`:

- architecture: settled;
- review semantics/behavior: settled;
- persisted compatibility: historical v2 records retained; no required schema migration;
- scope/non-goals: settled and intentionally narrow;
- subsystem boundary: EngineeringHost + isolated Raylib debug client; no product review SDK;
- acceptance criteria: observable and include regression/compatibility/UX outcome;
- validation policy: resumable machine suite separated from human gate;
- human-review policy: subjective-only, simple bounded workbench, blocking M038 dogfood review;
- constrained execution: explicit plan/shards/receipts/verifier and graphics rule;
- external dependencies: current .NET/Raylib/project engineering substrate only;
- baseline model: GPT-5.6 Luna can choose local mechanics without inventing project policy.

No unresolved material planning issue prevents `ready` status.
