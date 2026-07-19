# Historical Review Request — M025 Signal Passage Playable Vertical Slice

This request is closed as historical evidence. Its approved immutable record is
`.review/records/review.m025.signal-passage-playable-vertical-slice.json`; later
repository commits do not reopen it.

## Review ID

```text
review.m025.signal-passage-playable-vertical-slice
```

## Applicability

```text
level: required
blocking: true
classes:
  - visual
  - UX
  - creative
  - artifact-quality
```

## Subject

Review the complete Signal Passage consumer vertical slice after automated semantic validation and before final M025 verification.

## Reviewer role

Human project owner or delegated game-design reviewer.

## Required evidence

- geometric visual-definition inventory;
- final structural render artifacts;
- screenshots covering all object classes;
- synthesized sound-definition and generated-WAV inventory;
- cue playback/review notes;
- complete-journey run artifact;
- save/resume evidence;
- Linux export manifest and isolated launch;
- M025 performance report;
- consumer extension report;
- current M025 validation-plan and completed non-review receipts.

## Approval criteria

Approve only when:

1. player, containers, hazards, fragments, switch, exit, walls, and objective zone are visually distinguishable without debug overlays;
2. colors and outlines have acceptable contrast;
3. HUD and prompts make the objective understandable;
4. synthesized cues are distinguishable, appropriately brief, and not uncomfortably loud or harsh;
5. the objective journey is coherent and completable;
6. the exported game feels like a small game rather than a raw technical fixture;
7. no severe visual, audio, or UX issue remains.

## Allowed decisions

```text
approved
changes-requested
rejected
waived
superseded
```

A waiver requires explicit rationale and does not permit bypassing failed automated semantic validation.

## Record requirements

The review record under `.review/records/` must include:

- this review ID;
- decision;
- reviewed repository revision/fingerprint;
- reviewed evidence fingerprints;
- reviewer;
- conditions or notes;
- re-review triggers.

## Re-review triggers

- shape-to-object mapping changes;
- material color/contrast changes;
- synthesis oscillator, envelope, gain, or cue mapping changes;
- objective flow changes;
- complete-journey scenario changes;
- graphical host/export presentation changes;
- evidence fingerprint changes.
