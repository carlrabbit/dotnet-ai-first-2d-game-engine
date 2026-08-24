# Simple Human Review Workbench Contract

## Authority

This document is authoritative for the repository's simple human-review experience boundary, workbench presentation, decision controls, restart behavior, and relationship between automated validation and human judgment.

It does not replace asset-specific M029 workbench contracts.

## Principle

Human review exists only for acceptance that automation cannot fully decide because the answer depends on perception or experience.

Machine-verifiable claims are not reviewer tasks.

Examples of machine-owned claims include schema validity, determinism, persistence equivalence, conservation, required-file presence, fingerprint consistency, test status, performance thresholds, migration correctness, dependency direction, and artifact consistency.

Examples of legitimate human questions include visual readability, usability, gameplay feel, creative/presentation quality, audio quality, and bounded accessibility observation.

A milestone may mix machine and human acceptance. Machine acceptance is a prerequisite. The reviewer is shown only the remaining human question.

## Allowed required/blocking human classes

New required/blocking human reviews use one or more of:

```text
visual
UX
creative
gameplay
audio
accessibility-baseline
```

Other engineering/release/semantic labels may describe the milestone but do not by themselves justify human review.

Planning must resolve material architecture/semantic choices before a milestone becomes `ready`; do not defer those choices to human review.

## No automatic classifier

Do not add an LLM or heuristic natural-language classifier that decides whether a criterion belongs to a human.

The milestone declares the human question explicitly. Deterministic validation checks that the declared class and review shape comply with this contract.

## Simple review experience

A simple review experience is one milestone-owned executable view of the property being judged.

M038 supports two shapes:

```text
scenario
review shard
```

A scenario uses actual engine/runtime/rendering behavior.

A review shard is purpose-built engineering composition that starts directly at the state needed for judgment. It must exercise the real implementation under review rather than a second fake implementation of the behavior.

The workbench is not a general provider/plugin host.

## Complexity bound

A simple review:

- has one concise question/subject;
- starts at or near the state under judgment;
- requires at most two deliberate interactions with reviewed content before a decision can be made;
- may passively run/animate for observation;
- does not require a command list, artifact tour, debugger, or implementation knowledge.

`Restart`, `Reject`, `Accept`, and closing the window do not count as reviewed-content interactions.

If the bound can be met by truthfully pre-staging a representative scenario/shard, do that rather than adding navigation/setup steps.

If the property cannot honestly be judged within this bound, the generic simple workbench is inapplicable. The owning milestone must specify a separate explicit manual/exploratory review path instead of enlarging this workbench.

## Presentation

Normal reviewer presentation contains:

1. review title/context;
2. one concise question;
3. the actual content/demo;
4. exactly three primary controls:

```text
Restart    Reject    Accept
```

The content/demo receives the dominant presentation area.

The reviewer must not need to inspect raw JSON, hashes, report statuses, file paths, or test output to answer the question.

No normal reviewer UI for:

- comments/notes;
- ratings;
- approve-with-conditions;
- defer/skip/waive;
- next/previous review queue;
- evidence browser;
- review history;
- asset campaign/promotion actions.

## Decision semantics

### Restart

`Restart` has no review-decision effect.

It is explicit and reviewer-controlled.

It must start a fresh review execution from the current repository/build state. It must not claim that an in-process world reset reloads code or changed assets.

A valid implementation may use a parent launcher/re-exec or another small process lifecycle, but the externally observable result is a fresh process.

There is no default file watching or automatic restart.

### Reject

`Reject` is non-final.

It updates the active repository-local review to the existing non-final changes-requested state (or an equivalent compatible state), requires no reviewer comment, and leaves the review experience open.

It does not:

- restart automatically;
- close automatically;
- create a completed historical rejection record;
- require the reviewer to explain the rejection in the workbench.

Small corrections are communicated conversationally to the execution agent. Material contract issues return to planning.

### Accept

`Accept` is the final human approval action.

It records `approved` through the canonical repository-local review authority. It may then close the workbench.

No AI implementation agent may activate or synthesize this decision.

### Window close

Closing without Accept never completes the review.

If the active request was already marked changes-requested, it remains non-final until a later explicit Accept or other authorized review action.

## Completion-state persistence

The workbench itself owns no durable reviewer session.

No persisted workbench queue, comment history, navigation state, rating state, or resume checkpoint is required.

The repository retains the minimal milestone-level `.review` request/final-record authority required by current project/guide semantics.

Historical v2 records remain readable and immutable. M038 does not require a new review-record schema version merely to implement the simple workbench.

Existing internal decision-history fields may remain for compatibility; they are not reviewer-facing requirements and must not drive workbench complexity.

## Automated prerequisites

Human review follows machine validation.

A review-run invocation must perform a fast current prerequisite check before opening the simple workbench. It must not rerun long validation inside the UI, and it must not ask the reviewer to manually inspect machine evidence.

If current machine prerequisites fail or the required experience is missing/non-executable, `review-run` fails clearly and records no human decision.

Machine aggregate verification is independent of human approval. A pending human review is a completion-gate state, not a machine-validation failure.

## Canonical human entry point

```text
./eng/review-run.sh <review-id-or-alias>
pwsh ./eng/review-run.ps1 <review-id-or-alias>
```

The launchers are thin adapters over tested .NET engineering authority.

`review-show` remains metadata/read-only inspection. `review-run` is the normal human interaction path for simple reviews.

`review-record` may remain as a lower-level/admin compatibility command, but normal simple-review approval/rejection is performed through the workbench controls.

## Graphics and dependency boundary

The simple graphical workbench belongs to repository engineering/debug infrastructure.

Only the isolated Raylib client may depend on raylib-cs/native raylib. Engineering review state, review rules, persisted records, scenario definitions, and machine validation remain native-graphics independent.

The workbench does not become a product/game-consumer API.

## M029 boundary

The existing M029 asset workbench is asset-specific.

M038 may reuse small implementation techniques such as Raylib button drawing, click detection, or persistent event-loop behavior, but does not inherit or generalize:

- workbench sessions;
- aliases;
- text command submission;
- candidate lists;
- asset decision vocabulary;
- consequence confirmation;
- preview-host IPC;
- asset promotion.

M029 behavior is not migrated by this contract.

## Review-readiness failure

A human criterion without a usable experience is not "evidence pending" for the reviewer to reconstruct manually. It is not review-ready.

For interactive/UX questions, a screenshot or document describing a flow does not prove that the flow is available to review.

M037 is the historical regression example: its main-menu capture cannot cover save/load, display rollback, or input rebinding UX questions.

Historical M037 remains complete as historical evidence; future review definitions must satisfy this contract.
