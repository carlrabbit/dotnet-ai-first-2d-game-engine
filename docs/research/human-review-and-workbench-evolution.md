# Human Review and Workbench Evolution — Conversation-Led Reconstruction

## Status

Reconstructed planning context; non-authoritative.

## Question

How did the project's human-review model evolve, and which distinctions should future workbench/review planning preserve?

## Early problem

The project is designed for heavy automated implementation and validation, but some acceptance questions cannot be decided mechanically:

- visual clarity;
- audio quality;
- usability;
- gameplay feel;
- whether information is understandable;
- whether a result matches design intent.

Early review flows tended to mix these questions with machine-checkable facts or expose reports/JSON that were technically rich but poor human review experiences.

## Core distinction

The project converged on:

```text
machine validation
    proves facts

human review
    judges subjective quality
```

Examples of machine-owned facts:

- schema/reference validity;
- exact candidate identity;
- hashes/fingerprints;
- determinism;
- persistence/recovery;
- launch readiness;
- control wiring;
- whether animation frames actually advance;
- whether the correct preview subject is loaded.

Examples of human-owned judgments:

- does this image look correct;
- is the animation visually convincing/readable;
- does the audio comparison sound acceptable;
- is the UI understandable;
- does the gameplay interaction feel right.

The human should not be used as a manual hash checker.

## Milestone-owned review

Review evolved toward explicit milestone ownership.

A milestone should state:

- whether human review applies;
- exact subjective question;
- review class;
- reviewer;
- blocking/non-blocking effect;
- evidence/experience needed to make the judgment.

Not every milestone needs human review.

A purely mechanical infrastructure or migration milestone should not manufacture a subjective review gate.

## M038 simple Review Workbench

The generic Review Workbench intentionally became small and bounded:

```text
review question
optional content launch / evidence support
Reject
Accept
```

The value is consistency and low cognitive load, not building a universal editor.

The review framework owns review navigation and persistence.

The reviewed capability owns the actual content experience.

This distinction became important later for asset previews.

## Review launch is optional support

A key refinement from conversation:

A reviewer must be allowed to Accept or Reject without launching the evidence surface in that specific Review Workbench session.

Reason:

- the reviewer may already have inspected it elsewhere;
- the review record is the human judgment authority;
- launch history is not proof of understanding;
- over-guarding creates fake rigor and unnecessary coupling.

Therefore:

```text
preview launch
!=
milestone review acceptance prerequisite
```

This does not mean every domain approval workflow should behave the same way.

## Domain approval versus milestone review

Asset curation exposed the need to separate two concepts.

### Milestone human review

A repository-level completion mechanism.

The user can judge through any adequate inspection path and record Accept/Reject.

### Asset-curation approval

A domain decision that may require a current exact preview acknowledgement before durable approval, because the decision must prove that the reviewed candidate/variant/corrections match the committed subject.

That guard protects domain decision integrity.

The two systems should not be unified merely because both contain the word "review".

## Workbench content must be real

M048 exposed a recurring anti-pattern:

```text
review shell says candidate X
but
content surface renders generic/smoke/placeholder data
```

This is not a review experience; it is mislabeled evidence.

The resulting principle is:

The reviewed content must traverse the actual capability path that the subjective decision is supposed to judge.

For asset review:

```text
review item
→ exact registered fixture
→ exact materialization subject
→ actual preview process
→ actual engine rendering/animation/audio path
```

## Reviewability before subjectivity

Human review should begin only after machine checks prove the experience is structurally reviewable.

For example:

- exact fixture resolved;
- child process launchable;
- correct modality selected;
- irrelevant controls absent;
- relevant controls affect actual behavior;
- environment prerequisites truthfully diagnosed.

This avoids asking a human to judge a broken harness.

## Human-visible context matters

M050 showed that technically functional controls are insufficient if the reviewer does not know:

- what is being reviewed;
- which candidate/variant/correction;
- what raw versus processed means;
- what question they are answering;
- which controls are relevant.

Human-review UX is therefore a first-class contract when a tool exists specifically to support subjective judgment.

## RDP / remote interaction lesson

Workbench interaction had to account for remote desktop constraints.

Text/mouse/touch and review operations should resolve to the same semantic actions where practical.

Remote-access support is an interaction capability, not a new decision semantics layer.

## Durable planning implications

Future human-review/workbench planning should preserve:

1. subjective-only human review;
2. machine proof up to the subjective edge;
3. milestone-owned review questions;
4. simple generic review shell;
5. real capability-specific content surfaces;
6. optional launch for milestone acceptance;
7. separate domain approval guards when domain integrity requires them;
8. clear reviewer context;
9. no placeholder evidence accepted as real review content.

## Related current authority

- `docs/HUMAN-REVIEW.md`
- `docs/engineering/human-review-workflow.md`
- `docs/specs/simple-human-review-workbench-contract.md`
- M038 review infrastructure
- M048/M050 asset preview/review authority

## Freshness

Reconstructed through 2026-10-14.

Revalidate if the generic Review Workbench is replaced, review authority becomes multi-user/remote, or M053 materially restructures workbench interaction.
