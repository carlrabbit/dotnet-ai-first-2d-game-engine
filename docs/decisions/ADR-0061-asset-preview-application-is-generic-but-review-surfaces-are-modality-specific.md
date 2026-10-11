# ADR-0061 — Asset Preview Application Is Generic but Review Surfaces Are Modality-Specific

## Status

Accepted for M050.

## Context

M048 correctly introduced one actual candidate-preview application, but implementation exposed the union of image, animation, and audio controls on every review screen.

This made the review experience ambiguous: reviewers could see animation controls while judging audio, audio controls while judging images, and controls that had no effect on the current subject.

The defect came from conflating a generic process/application boundary with a generic visible interaction surface.

## Decision

Keep one generic asset-preview executable and shared infrastructure.

Select the visible review surface from the structured M048 `mediaKind`:

```text
image -> image review surface
animation -> animation review surface
audio -> audio review surface
```

Each surface shows only controls meaningful to its subject and a common human-readable review-context header.

Opaque IDs are never parsed to choose modality.

The generic Review Workbench remains the Accept/Reject owner. Preview launch remains optional for human decision recording.

## Consequences

- implementation can reuse one Raylib/native process and common infrastructure;
- reviewers no longer need to mentally filter irrelevant controls;
- every visible button has a reason to exist for the current subject;
- review context remains visible after the child preview opens;
- M048 identity/materialization/review authority remains unchanged;
- no duplicate M050 human-review items are required because the existing M048 reviews remain the subjective acceptance gate.
